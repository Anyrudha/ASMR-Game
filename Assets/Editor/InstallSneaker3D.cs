#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Builds the runtime sneaker prefab from the imported CC0 sneaker asset.
/// The source asset is a pair, so this processor keeps one complete shoe,
/// centers it, and prepares it for the restoration system.
/// </summary>
public static class InstallSneaker3D
{
    private const string ModelPath = "Assets/Art/Sneakers/Sneakers.obj";
    private const string TexturePath = "Assets/Art/Sneakers/sneaker_diffuse.png";
    private const string ShaderName = "Restore/DirtSurface";
    private const string PrefabPath = "Assets/Resources/Sneakers/Sneakers.prefab";
    private const string MaterialPath = "Assets/Resources/Sneakers/SneakerRestoreMaterial.mat";

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

        // Rebuild once when the imported source is present. The prefab itself is
        // marked by the presence of MeshDirtSurface after successful processing.
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null && prefab.GetComponent<MeshDirtSurface>() != null)
            return;

        BuildPrefabInternal(false);
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

        // The free CC0 source is a pair. Keep the right-hand shoe and discard
        // the other shoe before creating the runtime prefab.
        KeepSingleShoe(instance);

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
            surface = instance.AddComponent<MeshDirtSurface>();

        bool success = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out _);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
            EditorUtility.DisplayDialog("Restore", success
                ? "3D single-sneaker prefab created successfully."
                : "Unity could not create the sneaker prefab.", "OK");
    }

    private static void KeepSingleShoe(GameObject root)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length < 2) return;

        Bounds all = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            all.Encapsulate(renderers[i].bounds);

        float split = all.center.x;
        float left = 0f;
        float right = 0f;
        int leftCount = 0;
        int rightCount = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            float x = root.transform.InverseTransformPoint(renderer.bounds.center).x;
            if (x < split)
            {
                left += x;
                leftCount++;
            }
            else
            {
                right += x;
                rightCount++;
            }
        }

        if (leftCount == 0 || rightCount == 0) return;

        // Keep the side with the larger renderer cluster. For this asset both
        // shoes are equivalent; this also remains safe if the source hierarchy
        // contains several renderers per shoe.
        bool keepRight = rightCount >= leftCount;

        foreach (MeshRenderer renderer in renderers)
        {
            float x = root.transform.InverseTransformPoint(renderer.bounds.center).x;
            bool isRight = x >= split;
            if (isRight != keepRight)
                renderer.gameObject.SetActive(false);
        }
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
