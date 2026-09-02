#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Builds the runtime sneaker prefab from the imported CC0 sneaker asset.
/// The source asset is a pair packed into the same mesh, so this processor
/// extracts one complete shoe from the mesh instead of relying on renderer count.
/// </summary>
public static class InstallSneaker3D
{
    private const string ModelPath = "Assets/Art/Sneakers/Sneakers.obj";
    private const string TexturePath = "Assets/Art/Sneakers/sneaker_diffuse.png";
    private const string ShaderName = "Restore/DirtSurface";
    private const string PrefabPath = "Assets/Resources/Sneakers/Sneakers.prefab";
    private const string MaterialPath = "Assets/Resources/Sneakers/SneakerRestoreMaterial.mat";
    private const int BuildVersion = 4;
    private const string BuildVersionKey = "Restore.SingleSneakerPrefabVersion";

    [InitializeOnLoadMethod]
    private static void AutoBuildOnEditorLoad()
    {
        EditorApplication.delayCall += TryAutomaticBuild;
    }

    [MenuItem("Restore/3D Sneaker/Build Sneaker Prefab")]
    public static void BuildPrefab()
    {
        BuildPrefabInternal(true);
    }

    private static void TryAutomaticBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutomaticBuild;
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null ||
            AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) == null)
            return;

        // Version 4 is a forced migration from the previous pair-prefab build.
        if (EditorPrefs.GetInt(BuildVersionKey, 0) >= BuildVersion)
            return;

        BuildPrefabInternal(false);
        EditorPrefs.SetInt(BuildVersionKey, BuildVersion);
    }

    private static void BuildPrefabInternal(bool showDialog)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        Shader shader = Shader.Find(ShaderName);

        if (model == null || texture == null || shader == null)
        {
            if (showDialog)
            {
                string missing = model == null ? "Sneakers.obj" : texture == null ? "sneaker_diffuse.png" : "Restore/DirtSurface shader";
                EditorUtility.DisplayDialog("Restore", missing + " is not ready yet. Wait for Unity to finish importing/compiling, then try again.", "OK");
            }
            return;
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Sneakers");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Sneaker Restore Material" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_MainTex", texture);
        material.SetFloat("_DirtStrength", 1f);
        material.SetColor("_DirtColor", new Color(0.20f, 0.13f, 0.08f, 1f));
        EditorUtility.SetDirty(material);

        GameObject oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (oldPrefab != null)
            AssetDatabase.DeleteAsset(PrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Sneakers";
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        // The source contains two shoes inside the same mesh. Extract one side.
        if (!ExtractSingleShoe(instance))
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Restore", "Could not separate the paired sneaker mesh. The source model may have changed.", "OK");
            Object.DestroyImmediate(instance);
            return;
        }

        // Give the shoe a deliberate three-quarter product angle.
        instance.transform.rotation = Quaternion.Euler(8f, -26f, 0f);

        Bounds bounds = CalculateBounds(instance);
        float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (longest > 0.0001f)
            instance.transform.localScale *= 4.9f / longest;

        bounds = CalculateBounds(instance);
        instance.transform.position -= bounds.center;

        foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        MeshDirtSurface surface = instance.GetComponent<MeshDirtSurface>();
        if (surface == null)
            instance.AddComponent<MeshDirtSurface>();

        bool success = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out _);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
            EditorUtility.DisplayDialog("Restore", success
                ? "3D single-sneaker prefab created successfully."
                : "Unity could not create the sneaker prefab.", "OK");
    }

    private static bool ExtractSingleShoe(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0)
            return false;

        // Find the pair's left/right split in root-local space. This works even
        // when the OBJ stores both shoes in one MeshFilter/MeshRenderer.
        bool foundVertex = false;
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        foreach (MeshFilter filter in filters)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || mesh.vertexCount == 0)
                continue;

            Vector3[] vertices = mesh.vertices;
            foreach (Vector3 vertex in vertices)
            {
                float x = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex)).x;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                foundVertex = true;
            }
        }

        if (!foundVertex || maxX - minX < 0.000001f)
            return true;

        float splitX = (minX + maxX) * 0.5f;
        int leftTriangles = 0;
        int rightTriangles = 0;

        foreach (MeshFilter filter in filters)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;

            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                float x = TriangleRootLocalX(filter, vertices, triangles[i], triangles[i + 1], triangles[i + 2], root);
                if (x < splitX) leftTriangles++;
                else rightTriangles++;
            }
        }

        if (leftTriangles == 0 || rightTriangles == 0)
            return true; // Already a single shoe.

        // The two source shoes are mirrored. Keep the side with more geometry;
        // ties intentionally keep the right side for deterministic builds.
        bool keepRight = rightTriangles >= leftTriangles;
        bool changed = false;

        foreach (MeshFilter filter in filters)
        {
            Mesh source = filter.sharedMesh;
            if (source == null) continue;

            Vector3[] sourceVertices = source.vertices;
            int[] sourceTriangles = source.triangles;
            if (sourceTriangles.Length == 0)
            {
                filter.gameObject.SetActive(false);
                changed = true;
                continue;
            }

            List<int> keptTriangles = new List<int>(sourceTriangles.Length);
            for (int i = 0; i + 2 < sourceTriangles.Length; i += 3)
            {
                float x = TriangleRootLocalX(filter, sourceVertices, sourceTriangles[i], sourceTriangles[i + 1], sourceTriangles[i + 2], root);
                bool isRight = x >= splitX;
                if (isRight == keepRight)
                {
                    keptTriangles.Add(sourceTriangles[i]);
                    keptTriangles.Add(sourceTriangles[i + 1]);
                    keptTriangles.Add(sourceTriangles[i + 2]);
                }
            }

            if (keptTriangles.Count == 0)
            {
                filter.gameObject.SetActive(false);
                changed = true;
                continue;
            }

            if (keptTriangles.Count == sourceTriangles.Length)
                continue;

            Mesh single = CreateFilteredMesh(source, keptTriangles);
            if (single == null)
                return false;

            filter.sharedMesh = single;
            MeshCollider collider = filter.GetComponent<MeshCollider>();
            if (collider != null)
                collider.sharedMesh = single;

            changed = true;
        }

        return changed;
    }

    private static float TriangleRootLocalX(MeshFilter filter, Vector3[] vertices, int a, int b, int c, GameObject root)
    {
        Vector3 pa = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[a]));
        Vector3 pb = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[b]));
        Vector3 pc = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[c]));
        return (pa.x + pb.x + pc.x) / 3f;
    }

    private static Mesh CreateFilteredMesh(Mesh source, List<int> triangles)
    {
        if (source == null || triangles.Count < 3)
            return null;

        Vector3[] sourceVertices = source.vertices;
        Vector2[] sourceUv = source.uv;
        Vector3[] sourceNormals = source.normals;
        Vector4[] sourceTangents = source.tangents;
        Color[] sourceColors = source.colors;

        Dictionary<int, int> remap = new Dictionary<int, int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector4> tangents = new List<Vector4>();
        List<Color> colors = new List<Color>();
        List<int> newTriangles = new List<int>(triangles.Count);

        for (int i = 0; i < triangles.Count; i++)
        {
            int oldIndex = triangles[i];
            if (!remap.TryGetValue(oldIndex, out int newIndex))
            {
                newIndex = vertices.Count;
                remap.Add(oldIndex, newIndex);
                vertices.Add(sourceVertices[oldIndex]);

                if (sourceUv != null && sourceUv.Length == sourceVertices.Length)
                    uvs.Add(sourceUv[oldIndex]);
                if (sourceNormals != null && sourceNormals.Length == sourceVertices.Length)
                    normals.Add(sourceNormals[oldIndex]);
                if (sourceTangents != null && sourceTangents.Length == sourceVertices.Length)
                    tangents.Add(sourceTangents[oldIndex]);
                if (sourceColors != null && sourceColors.Length == sourceVertices.Length)
                    colors.Add(sourceColors[oldIndex]);
            }

            newTriangles.Add(newIndex);
        }

        Mesh mesh = new Mesh
        {
            name = source.name + "_SingleShoe",
            indexFormat = vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(newTriangles, 0);

        if (uvs.Count == vertices.Count) mesh.SetUVs(0, uvs);
        if (normals.Count == vertices.Count) mesh.SetNormals(normals);
        else mesh.RecalculateNormals();
        if (tangents.Count == vertices.Count) mesh.SetTangents(tangents);
        if (colors.Count == vertices.Count) mesh.SetColors(colors);

        mesh.RecalculateBounds();
        return mesh;
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path) && !string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
