#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Converts the locally imported CC0 sneaker OBJ into the runtime prefab used by the
/// restore scene. The processor also auto-builds when the OBJ/texture are imported,
/// so the scene never silently falls back to the old procedural sneaker.
/// </summary>
public static class InstallSneaker3D
{
    private const string ModelPath = "Assets/Art/Sneakers/Sneakers.obj";
    private const string TexturePath = "Assets/Art/Sneakers/sneaker_diffuse.png";
    private const string ShaderName = "Restore/DirtSurface";
    private const string PrefabPath = "Assets/Resources/Sneakers/Sneakers.prefab";
    private const string MaterialPath = "Assets/Resources/Sneakers/SneakerRestoreMaterial.mat";

    [MenuItem("Restore/3D Sneaker/Build Sneaker Prefab")]
    public static void BuildPrefab()
    {
        BuildPrefabInternal(true);
    }

    internal static void BuildPrefabAutomatic()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null) return;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) == null) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
        BuildPrefabInternal(false);
    }

    private static void BuildPrefabInternal(bool showDialog)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        Shader shader = Shader.Find(ShaderName);

        if (model == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Restore", "Sneakers.obj was not found at Assets/Art/Sneakers/Sneakers.obj.", "OK");
            return;
        }
        if (texture == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Restore", "sneaker_diffuse.png was not found at Assets/Art/Sneakers/sneaker_diffuse.png.", "OK");
            return;
        }
        if (shader == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Restore", "Restore/DirtSurface shader has not compiled yet. Wait for Unity to finish importing, then try again.", "OK");
            return;
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Sneakers");

        GameObject oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (oldPrefab != null) AssetDatabase.DeleteAsset(PrefabPath);

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

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Sneakers";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        bool success = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out _);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Restore",
                success ? "3D sneaker prefab created. The Restore scene will now use it." : "Unity could not create the sneaker prefab.",
                "OK");
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folder))
            EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path) && !string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, folder);
    }
}

/// <summary>Automatically creates the runtime sneaker prefab after the local asset is imported.</summary>
public sealed class SneakerAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool relevant = false;
        foreach (string path in importedAssets)
        {
            if (path == "Assets/Art/Sneakers/Sneakers.obj" || path == "Assets/Art/Sneakers/sneaker_diffuse.png")
            {
                relevant = true;
                break;
            }
        }
        if (!relevant) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallSneaker3D.BuildPrefabAutomatic;
                return;
            }
            InstallSneaker3D.BuildPrefabAutomatic();
        };
    }
}
#endif
