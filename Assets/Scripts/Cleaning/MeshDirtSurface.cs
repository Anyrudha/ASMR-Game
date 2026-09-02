using UnityEngine;

/// <summary>Reusable 3D restoration surface driven by mesh UVs and a CPU-editable dirt mask.</summary>
public sealed class MeshDirtSurface : MonoBehaviour
{
    [SerializeField] private int maskSize = 512;
    [SerializeField] private int dirtStampCount = 120;
    [SerializeField] private int randomSeed = 2409;
    [SerializeField] private Color dirtColor = new Color(0.20f, 0.13f, 0.08f, 1f);

    private MeshRenderer[] renderers;
    private Texture2D dirtMask;
    private Color32[] pixels;
    private int initialDirty;
    private int remainingDirty;

    public float Progress { get; private set; }
    public bool IsFullyClean => Progress >= 0.999f;

    public void Initialise()
    {
        renderers = GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) return;

        foreach (MeshRenderer renderer in renderers)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && renderer.GetComponent<MeshCollider>() == null)
            {
                MeshCollider collider = renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
            }
        }

        CreateMask();
        foreach (MeshRenderer renderer in renderers)
        {
            Material m = renderer.material;
            if (m.HasProperty("_DirtMask")) m.SetTexture("_DirtMask", dirtMask);
            if (m.HasProperty("_DirtColor")) m.SetColor("_DirtColor", dirtColor);
            if (m.HasProperty("_DirtStrength")) m.SetFloat("_DirtStrength", 1f);
        }
    }

    public float CleanAt(Vector2 uv, float radius, float strength)
    {
        if (pixels == null) return 0f;
        int cx = Mathf.RoundToInt(Mathf.Clamp01(uv.x) * (maskSize - 1));
        int cy = Mathf.RoundToInt(Mathf.Clamp01(uv.y) * (maskSize - 1));
        int r = Mathf.Max(1, Mathf.RoundToInt(radius * maskSize));
        float removed = 0f;

        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(maskSize - 1, cy + r); y++)
        for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(maskSize - 1, cx + r); x++)
        {
            float dx = x - cx, dy = y - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
            if (d > 1f) continue;
            int i = y * maskSize + x;
            byte old = pixels[i].r;
            if (old == 0) continue;
            float falloff = 1f - Mathf.SmoothStep(0f, 1f, d);
            int next = Mathf.Max(0, old - Mathf.RoundToInt(255f * strength * falloff));
            pixels[i].r = pixels[i].g = pixels[i].b = (byte)next;
            removed += old - next;
        }

        if (removed > 0f)
        {
            dirtMask.SetPixels32(pixels);
            dirtMask.Apply(false, false);
            RecalculateProgress();
        }
        return removed;
    }

    private void CreateMask()
    {
        dirtMask = new Texture2D(maskSize, maskSize, TextureFormat.R8, false, true);
        dirtMask.filterMode = FilterMode.Bilinear;
        dirtMask.wrapMode = TextureWrapMode.Clamp;
        pixels = new Color32[maskSize * maskSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 255);

        Random.InitState(randomSeed);
        for (int i = 0; i < dirtStampCount; i++)
        {
            float u = Random.Range(0.06f, 0.94f);
            float v = Random.Range(0.06f, 0.94f);
            Stamp(Mathf.RoundToInt(u * (maskSize - 1)), Mathf.RoundToInt(v * (maskSize - 1)), Random.Range(8, 38), Random.Range(130, 245));
        }

        dirtMask.SetPixels32(pixels);
        dirtMask.Apply(false, false);
        RecalculateProgress();
    }

    private void Stamp(int cx, int cy, int radius, int alpha)
    {
        for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(maskSize - 1, cy + radius); y++)
        for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(maskSize - 1, cx + radius); x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / radius;
            if (d > 1f) continue;
            int i = y * maskSize + x;
            byte a = (byte)Mathf.RoundToInt(alpha * (1f - d));
            if (a > pixels[i].r) pixels[i].r = pixels[i].g = pixels[i].b = a;
        }
    }

    private void RecalculateProgress()
    {
        if (initialDirty == 0)
            for (int i = 0; i < pixels.Length; i++) if (pixels[i].r > 8) initialDirty++;
        remainingDirty = 0;
        for (int i = 0; i < pixels.Length; i++) if (pixels[i].r > 8) remainingDirty++;
        Progress = initialDirty == 0 ? 1f : 1f - Mathf.Clamp01((float)remainingDirty / initialDirty);
    }
}
