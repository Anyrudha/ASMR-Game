using UnityEngine;

/// <summary>
/// A reusable, texture-mask based dirt surface.
/// Dirt is stored as alpha in a runtime texture, so brushing removes an actual area
/// instead of toggling whole dirt GameObjects on/off.
/// </summary>
public sealed class DirtLayer : MonoBehaviour
{
    [Header("Mask")]
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 320;
    [SerializeField] private int randomSeed = 2409;

    private Texture2D dirtTexture;
    private Sprite dirtSprite;
    private SpriteRenderer dirtRenderer;
    private Color32[] dirtPixels;
    private byte[] alpha;
    private int dirtyPixelCount;
    private int initialDirtyPixelCount;
    private int dirtyPixelCache;
    private bool dirtyCacheValid;
    private int[] shoeMask;

    public float Progress { get; private set; }
    public bool IsFullyClean => Progress >= 0.999f;

    public void BuildSneaker()
    {
        ClearChildren();
        CreateBackground();
        CreateSneakerBase();
        CreateDirtOverlay();
    }

    /// <summary>Erase dirt in normalized local coordinates (0..1).</summary>
    public float CleanAt(Vector2 localUv, float radiusPixels, float strength)
    {
        if (dirtTexture == null || alpha == null) return 0f;

        int cx = Mathf.RoundToInt(localUv.x * (textureWidth - 1));
        int cy = Mathf.RoundToInt(localUv.y * (textureHeight - 1));
        int radius = Mathf.Max(1, Mathf.RoundToInt(radiusPixels));
        int minX = Mathf.Max(0, cx - radius);
        int maxX = Mathf.Min(textureWidth - 1, cx + radius);
        int minY = Mathf.Max(0, cy - radius);
        int maxY = Mathf.Min(textureHeight - 1, cy + radius);
        float radiusSq = radius * radius;
        float removed = 0f;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float distSq = dx * dx + dy * dy;
                if (distSq > radiusSq) continue;

                int index = y * textureWidth + x;
                if (shoeMask[index] == 0 || alpha[index] == 0) continue;

                float distance01 = Mathf.Sqrt(distSq) / radius;
                float falloff = 1f - Mathf.SmoothStep(0f, 1f, distance01);
                int oldA = alpha[index];
                int remove = Mathf.Clamp(Mathf.RoundToInt(255f * strength * falloff), 1, 255);
                int newA = Mathf.Max(0, oldA - remove);
                alpha[index] = (byte)newA;
                dirtPixels[index].a = (byte)newA;
                removed += oldA - newA;
            }
        }

        if (removed > 0f)
        {
            dirtyCacheValid = false;
            dirtTexture.SetPixels32(dirtPixels);
            dirtTexture.Apply(false, false);
            RecalculateProgress();
        }

        return removed;
    }

    public Vector2 WorldToUv(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);

        // The SpriteRenderer is measured in world units, while the cleaning
        // texture uses normalized UV coordinates. Normalize against the actual
        // sprite bounds instead of assuming the local object is exactly 1x1.
        if (dirtRenderer != null && dirtRenderer.sprite != null)
        {
            Bounds bounds = dirtRenderer.sprite.bounds;
            float u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, local.x);
            float v = Mathf.InverseLerp(bounds.min.y, bounds.max.y, local.y);
            return new Vector2(u, v);
        }

        return new Vector2(local.x + 0.5f, local.y + 0.5f);
    }

    private void RecalculateProgress()
    {
        if (dirtyCacheValid) return;
        int remaining = 0;
        for (int i = 0; i < alpha.Length; i++)
            if (alpha[i] > 0) remaining++;

        dirtyPixelCache = remaining;
        dirtyCacheValid = true;
        Progress = initialDirtyPixelCount == 0
            ? 1f
            : 1f - Mathf.Clamp01((float)remaining / initialDirtyPixelCount);
    }

    private void CreateSneakerBase()
    {
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[textureWidth * textureHeight];

        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0);

        for (int y = 35; y < 130; y++)
        {
            for (int x = 80; x < 390; x++)
            {
                float nx = (x - 235f) / 180f;
                float ny = (y - 82f) / 70f;
                if (nx * nx + ny * ny < 1f)
                    pixels[y * textureWidth + x] = new Color(0.87f, 0.91f, 0.92f, 1f);
            }
        }

        for (int y = 70; y < 235; y++)
        {
            for (int x = 130; x < 355; x++)
            {
                float nx = (x - 235f) / 120f;
                float ny = (y - 145f) / 115f;
                if (nx * nx + ny * ny < 1f)
                    pixels[y * textureWidth + x] = new Color(0.82f, 0.87f, 0.89f, 1f);
            }
        }

        // Sole
        for (int y = 28; y < 70; y++)
        {
            for (int x = 58; x < 425; x++)
            {
                float nx = (x - 242f) / 190f;
                float ny = (y - 49f) / 26f;
                if (nx * nx + ny * ny < 1f)
                    pixels[y * textureWidth + x] = new Color(0.96f, 0.96f, 0.93f, 1f);
            }
        }

        // Collar and tongue
        for (int y = 178; y < 250; y++)
            for (int x = 130; x < 205; x++)
                if (((x - 168f) / 38f) * ((x - 168f) / 38f) + ((y - 214f) / 45f) * ((y - 214f) / 45f) < 1f)
                    pixels[y * textureWidth + x] = new Color(0.28f, 0.55f, 0.62f, 1f);

        // Laces
        for (int lace = 0; lace < 5; lace++)
        {
            int lx = 205 + lace * 25;
            for (int y = 150; y < 160; y++)
                for (int x = lx - 12; x < lx + 12; x++)
                    pixels[y * textureWidth + x] = Color.white;
        }

        // Small highlight strip.
        for (int x = 250; x < 365; x++)
            for (int y = 105; y < 112; y++)
                if (pixels[y * textureWidth + x].a > 0)
                    pixels[y * textureWidth + x] = new Color(1f, 1f, 1f, 0.75f);

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        GameObject shoe = new GameObject("Sneaker Base");
        shoe.transform.SetParent(transform, false);
        SpriteRenderer renderer = shoe.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 100f);
        renderer.sortingOrder = 0;
    }

    private void CreateDirtOverlay()
    {
        dirtTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        dirtTexture.filterMode = FilterMode.Bilinear;
        dirtTexture.wrapMode = TextureWrapMode.Clamp;
        alpha = new byte[textureWidth * textureHeight];
        shoeMask = new int[textureWidth * textureHeight];

        // Define the same approximate shoe silhouette used by the base.
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                bool upper = Ellipse(x, y, 235f, 82f, 180f, 70f);
                bool body = Ellipse(x, y, 235f, 145f, 120f, 115f);
                bool sole = Ellipse(x, y, 242f, 49f, 190f, 26f);
                if (upper || body || sole) shoeMask[y * textureWidth + x] = 1;
            }
        }

        Random.InitState(randomSeed);
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0);

        int dirtCount = 0;
        for (int i = 0; i < 115; i++)
        {
            int cx = Random.Range(75, 405);
            int cy = Random.Range(35, 220);
            int radius = Random.Range(7, 25);
            Color dirtColor = Color.Lerp(new Color(0.25f, 0.12f, 0.05f), new Color(0.48f, 0.28f, 0.12f), Random.value);

            for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(textureHeight - 1, cy + radius); y++)
            {
                for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(textureWidth - 1, cx + radius); x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > radius || shoeMask[y * textureWidth + x] == 0) continue;
                    float a = 0.78f * (1f - distance / radius);
                    int index = y * textureWidth + x;
                    pixels[index] = Color.Lerp(pixels[index], new Color(dirtColor.r, dirtColor.g, dirtColor.b, a), 0.9f);
                    alpha[index] = (byte)Mathf.Max(alpha[index], Mathf.RoundToInt(a * 255f));
                }
            }
            dirtCount++;
        }

        dirtPixels = new Color32[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) dirtPixels[i] = pixels[i];
        dirtTexture.SetPixels32(dirtPixels);
        dirtTexture.Apply(false, false);
        dirtRenderer = new GameObject("Dirt Mask").AddComponent<SpriteRenderer>();
        dirtRenderer.transform.SetParent(transform, false);
        dirtSprite = Sprite.Create(dirtTexture, new Rect(0, 0, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 100f);
        dirtRenderer.sprite = dirtSprite;
        dirtRenderer.sortingOrder = 2;

        RecalculateInitialDirtyPixels();
    }

    private void RecalculateInitialDirtyPixels()
    {
        initialDirtyPixelCount = 0;
        for (int i = 0; i < alpha.Length; i++)
            if (alpha[i] > 0) initialDirtyPixelCount++;
        dirtyCacheValid = false;
        RecalculateProgress();
    }

    private static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float nx = (x - cx) / rx;
        float ny = (y - cy) / ry;
        return nx * nx + ny * ny < 1f;
    }

    private void CreateBackground()
    {
        GameObject shadow = new GameObject("Soft Shadow");
        shadow.transform.SetParent(transform, false);
        SpriteRenderer renderer = shadow.AddComponent<SpriteRenderer>();
        Texture2D texture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[128 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 128; x++)
            {
                float dx = (x - 64f) / 64f;
                float dy = (y - 32f) / 32f;
                float a = Mathf.Clamp01(1f - (dx * dx + dy * dy));
                pixels[y * 128 + x] = new Color(0.15f, 0.18f, 0.19f, a * 0.18f);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 128, 64), new Vector2(0.5f, 0.5f), 35f);
        renderer.transform.localPosition = new Vector3(0, -1.35f, 0);
        renderer.transform.localScale = new Vector3(1.1f, 0.65f, 1f);
        renderer.sortingOrder = -1;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }
}
