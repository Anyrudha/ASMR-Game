using UnityEngine;

public sealed class TouchInputManager : MonoBehaviour
{
    private RestorationManager manager;
    private Camera targetCamera;
    private Vector2 lastScreen;
    private bool dragging;

    public void Initialise(RestorationManager restorationManager, Camera camera)
    {
        manager = restorationManager;
        targetCamera = camera;
    }

    private void Update()
    {
        if (manager == null || targetCamera == null) return;
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { dragging = true; lastScreen = t.position; }
            if (dragging && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)) Send(t.position);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) Stop();
            return;
        }
        if (Input.GetMouseButtonDown(0)) { dragging = true; lastScreen = Input.mousePosition; }
        if (dragging && Input.GetMouseButton(0)) Send(Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) Stop();
    }

    private void Send(Vector2 screen)
    {
        float distance = Vector2.Distance(screen, lastScreen);
        float intensity = Mathf.Clamp01(0.2f + distance / Mathf.Max(1f, Screen.height * 0.018f));
        manager.ApplyScreenPosition(screen, intensity);
        AudioManager.SetInteraction(manager.CurrentTool, true, intensity);
        lastScreen = screen;
    }

    private void Stop()
    {
        dragging = false;
        AudioManager.SetInteraction(manager.CurrentTool, false, 0f);
    }
}
