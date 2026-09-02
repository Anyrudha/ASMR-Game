using UnityEngine;

public sealed class RestoreBootstrap : MonoBehaviour
{
    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        CreateWorld();
    }

    private void CreateWorld()
    {
        Camera camera = CreateCamera();
        GameObject managerObject = new GameObject("Restoration Manager");
        RestorationManager manager = managerObject.AddComponent<RestorationManager>();

        GameObject prefab = Resources.Load<GameObject>("Sneakers/Sneakers");
        if (prefab != null)
        {
            GameObject sneaker = Instantiate(prefab);
            sneaker.name = "Restoration Sneaker";

            // Use a calm three-quarter product view instead of looking directly at
            // the heel/toe. This makes the object read immediately as a sneaker and
            // leaves broad surfaces visible for the cleaning interaction.
            sneaker.transform.rotation = Quaternion.Euler(9f, -24f, 0f);

            Bounds bounds = CalculateBounds(sneaker);
            float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (longest > 0.0001f)
                sneaker.transform.localScale *= 5.15f / longest;

            bounds = CalculateBounds(sneaker);
            sneaker.transform.position += new Vector3(-bounds.center.x, -bounds.center.y - 0.05f, -bounds.center.z);

            MeshDirtSurface surface = sneaker.GetComponent<MeshDirtSurface>();
            if (surface == null) surface = sneaker.AddComponent<MeshDirtSurface>();
            manager.Initialise(surface, camera);
        }
        else
        {
            GameObject layerObject = new GameObject("Dirty Sneaker");
            DirtLayer layer = layerObject.AddComponent<DirtLayer>();
            layerObject.transform.position = new Vector3(0f, -0.05f, 0f);
            layerObject.transform.localScale = Vector3.one * 1.08f;
            layer.BuildSneaker();
            manager.Initialise(layer, camera);
        }

        TouchInputManager input = managerObject.AddComponent<TouchInputManager>();
        input.Initialise(manager, camera);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.15f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color(0.91f, 0.95f, 0.94f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }
}
