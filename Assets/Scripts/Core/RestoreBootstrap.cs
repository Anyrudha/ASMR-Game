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

        // v0.3 uses the real imported sneaker when the local one-click installer
        // has generated Assets/Resources/Sneakers/Sneakers.prefab. The old 2D
        // surface remains as a safe fallback until the local asset is installed.
        GameObject prefab = Resources.Load<GameObject>("Sneakers/Sneakers");
        if (prefab != null)
        {
            GameObject sneaker = Instantiate(prefab);
            sneaker.name = "Dirty Sneaker 3D";
            sneaker.transform.position = new Vector3(0f, -0.15f, 0f);
            sneaker.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            sneaker.transform.localScale = Vector3.one * 10f;

            MeshDirtSurface surface = sneaker.GetComponent<MeshDirtSurface>();
            if (surface == null) surface = sneaker.AddComponent<MeshDirtSurface>();
            surface.Initialise();
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

    private Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 2.95f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color(0.91f, 0.95f, 0.94f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }
}
