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
        GameObject layerObject = new GameObject("Dirty Sneaker");
        DirtLayer layer = layerObject.AddComponent<DirtLayer>();
        layerObject.transform.position = new Vector3(0f, -0.15f, 0f);
        layerObject.transform.localScale = Vector3.one * 1.42f;
        layer.BuildSneaker();

        GameObject managerObject = new GameObject("Restoration Manager");
        RestorationManager manager = managerObject.AddComponent<RestorationManager>();
        manager.Initialise(layer, camera);
        TouchInputManager input = managerObject.AddComponent<TouchInputManager>();
        input.Initialise(manager, camera);
        UIManager.Create(manager);
    }

    private Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.1f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color(0.91f, 0.95f, 0.94f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }
}
