using UnityEngine;

public sealed class DirtPatch : MonoBehaviour
{
    public Vector2 Position { get; private set; }
    public float Radius { get; private set; }
    public bool IsClean { get; private set; }
    private DirtLayer owner;

    public void Initialise(DirtLayer layer, Vector2 position, float radius)
    {
        owner = layer;
        Position = position;
        Radius = radius;
    }

    public bool Clean(CleaningTool tool)
    {
        if (IsClean || !tool.CanClean(this)) return false;
        IsClean = true;
        gameObject.SetActive(false);
        return true;
    }
}