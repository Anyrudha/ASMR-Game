#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class InstallSneaker3D
{
    private const string ModelPath = "Assets/Art/Sneakers/Sneakers.obj";
    private const string TexturePath = "Assets/Art/Sneakers/sneaker_diffuse.png";
    private const string ShaderName = "Restore/DirtSurface";
    private const string PrefabPath = "Assets/Resources/Sneakers/Sneakers.prefab";

    [MenuItem("Restore/3D Sneaker/Build Sneaker Prefab")]
    public static void BuildPrefab()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        Shader shader = Shader.Find(ShaderName);

        if (model == null)
        {
            EditorUtility.DisplayDialog("Restore", "Sneakers.obj was not found at Assets/Art/Sneakers/Sneakers.obj.", "OK");
            return;
        }
        if (texture == null)
        {
            EditorUtility.DisplayDialog("Restore", "sneaker_diffuse.png was not found at Assets/Art/Sneakers/sneaker_diffuse.png.", "OK");
            return;
        }
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Restore", "Restore/DirtSurface shader has not compiled yet. Wait for Unity to finish importing, then try again.", "OK");
            return;
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Sneakers");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Sneakers";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Material material = new Material(shader) { name = "Sneaker Restore Material" };
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_DirtStrength", 1f);
        material.SetColor("_DirtColor", new Color(0.20f, 0.13f, 0.08f, 1f));
        AssetDatabase.CreateAsset(material, "Assets/Resources/Sneakers/SneakerRestoreMaterial.mat");

        foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        if (PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out bool success) && success)
        {
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Restore", "3D sneaker prefab created. Restart Play Mode to use it.", "Great");
        }
        else
        {
            Object.DestroyImmediate(instance);
            EditorUtility.DisplayDialog("Restore", "Unity could not create the sneaker prefab.", "OK");
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
