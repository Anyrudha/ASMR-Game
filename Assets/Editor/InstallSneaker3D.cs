#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class InstallSneaker3D
{
    private const string ModelPath = "Assets/Art/Sneakers/Sneakers.obj";
    private const string TexturePath = "Assets/Art/Sneakers/sneaker_diffuse.png";
    private const string ShaderName = "Restore/DirtSurface";
    private const string PrefabPath = "Assets/Resources/Sneakers/Sneakers.prefab";
    private const string MaterialPath = "Assets/Resources/Sneakers/SneakerRestoreMaterial.mat";
    private const string SingleMeshPath = "Assets/Resources/Sneakers/SneakerSingleMesh.asset";
    private const int BuildVersion = 5;
    private const string BuildVersionKey = "Restore.SingleSneakerPrefabVersion";

    [InitializeOnLoadMethod]
    private static void AutoBuildOnEditorLoad() => EditorApplication.delayCall += TryAutomaticBuild;

    [MenuItem("Restore/3D Sneaker/Build Sneaker Prefab")]
    public static void BuildPrefab() => BuildPrefabInternal(true);

    private static void TryAutomaticBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutomaticBuild;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null || AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) == null)
            return;
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
            if (showDialog) EditorUtility.DisplayDialog("Restore", "Sneaker source, texture, or Restore/DirtSurface shader is not ready.", "OK");
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
        material.shader = shader;
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_DirtStrength", 1f);
        material.SetColor("_DirtColor", new Color(0.20f, 0.13f, 0.08f, 1f));
        EditorUtility.SetDirty(material);

        GameObject oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (oldPrefab != null) AssetDatabase.DeleteAsset(PrefabPath);
        Mesh oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(SingleMeshPath);
        if (oldMesh != null) AssetDatabase.DeleteAsset(SingleMeshPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Sneakers";
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        if (!ExtractSingleShoe(instance))
        {
            if (showDialog) EditorUtility.DisplayDialog("Restore", "Could not separate the paired sneaker mesh.", "OK");
            Object.DestroyImmediate(instance);
            return;
        }

        instance.transform.rotation = Quaternion.Euler(8f, -26f, 0f);
        Bounds bounds = CalculateBounds(instance);
        float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (longest > 0.0001f) instance.transform.localScale *= 4.9f / longest;
        bounds = CalculateBounds(instance);
        instance.transform.position -= bounds.center;

        foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        if (instance.GetComponent<MeshDirtSurface>() == null) instance.AddComponent<MeshDirtSurface>();
        bool success = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out _);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog) EditorUtility.DisplayDialog("Restore", success ? "3D single-sneaker prefab created successfully." : "Unity could not create the sneaker prefab.", "OK");
    }

    private static bool ExtractSingleShoe(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0) return false;

        float minX = float.MaxValue, maxX = float.MinValue;
        bool found = false;
        foreach (MeshFilter filter in filters)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            foreach (Vector3 v in mesh.vertices)
            {
                float x = root.transform.InverseTransformPoint(filter.transform.TransformPoint(v)).x;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); found = true;
            }
        }
        if (!found || maxX - minX < 0.000001f) return true;

        float splitX = (minX + maxX) * 0.5f;
        int left = 0, right = 0;
        foreach (MeshFilter filter in filters)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            Vector3[] v = mesh.vertices;
            int[] t = mesh.triangles;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                float x = TriangleRootLocalX(filter, v, t[i], t[i + 1], t[i + 2], root);
                if (x < splitX) left++; else right++;
            }
        }
        if (left == 0 || right == 0) return true;

        bool keepRight = right >= left;
        int assetIndex = 0;
        bool changed = false;
        foreach (MeshFilter filter in filters)
        {
            Mesh source = filter.sharedMesh;
            if (source == null) continue;
            Vector3[] vertices = source.vertices;
            int[] triangles = source.triangles;
            List<int> kept = new List<int>(triangles.Length);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                float x = TriangleRootLocalX(filter, vertices, triangles[i], triangles[i + 1], triangles[i + 2], root);
                if ((x >= splitX) == keepRight)
                {
                    kept.Add(triangles[i]); kept.Add(triangles[i + 1]); kept.Add(triangles[i + 2]);
                }
            }
            if (kept.Count == 0) { filter.gameObject.SetActive(false); changed = true; continue; }
            if (kept.Count == triangles.Length) continue;

            Mesh single = CreateFilteredMesh(source, kept);
            if (single == null) return false;
            string meshPath = assetIndex == 0 ? SingleMeshPath : $"Assets/Resources/Sneakers/SneakerSingleMesh_{assetIndex}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null) AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(single, meshPath);
            filter.sharedMesh = single;
            MeshCollider collider = filter.GetComponent<MeshCollider>();
            if (collider != null) collider.sharedMesh = single;
            assetIndex++;
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
        Vector3[] sv = source.vertices;
        Vector2[] su = source.uv;
        Vector3[] sn = source.normals;
        Vector4[] st = source.tangents;
        Color[] sc = source.colors;
        Dictionary<int, int> map = new Dictionary<int, int>();
        List<Vector3> v = new List<Vector3>(); List<Vector2> u = new List<Vector2>();
        List<Vector3> n = new List<Vector3>(); List<Vector4> tan = new List<Vector4>(); List<Color> col = new List<Color>();
        List<int> tri = new List<int>(triangles.Count);
        foreach (int old in triangles)
        {
            if (!map.TryGetValue(old, out int ni))
            {
                ni = v.Count; map.Add(old, ni); v.Add(sv[old]);
                if (su.Length == sv.Length) u.Add(su[old]);
                if (sn.Length == sv.Length) n.Add(sn[old]);
                if (st.Length == sv.Length) tan.Add(st[old]);
                if (sc.Length == sv.Length) col.Add(sc[old]);
            }
            tri.Add(ni);
        }
        Mesh mesh = new Mesh { name = source.name + "_SingleShoe", indexFormat = v.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(v); mesh.SetTriangles(tri, 0);
        if (u.Count == v.Count) mesh.SetUVs(0, u);
        if (n.Count == v.Count) mesh.SetNormals(n); else mesh.RecalculateNormals();
        if (tan.Count == v.Count) mesh.SetTangents(tan);
        if (col.Count == v.Count) mesh.SetColors(col);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path) && !string.IsNullOrEmpty(parent)) AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif