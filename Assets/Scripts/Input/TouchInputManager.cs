using UnityEngine;

public sealed class TouchInputManager : MonoBehaviour
{
    private RestorationManager manager;

    public void Initialise(RestorationManager restorationManager)
    {
        manager = restorationManager;
    }

    private void Update()
    {
        if (manager == null) return;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    manager.ApplyScreenPosition(touch.position);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            manager.ApplyScreenPosition(Input.mousePosition);
        }
    }
}
