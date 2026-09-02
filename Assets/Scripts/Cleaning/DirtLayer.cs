using UnityEngine;

/// <summary>Stylised sneaker surface plus a progressive alpha dirt mask.</summary>
public sealed class DirtLayer : MonoBehaviour
{
    [SerializeField] private int textureWidth = 640;
    [SerializeField] private int textureHeight = 480;
    [SerializeField] private int randomSeed = 2409;

    private Texture2D dirtTexture;
    private SpriteRenderer dirtRenderer;
    private Color32[] dirtPixels;
    private byte[] alpha;
    private byte[] shoeAlpha;
    private int initialDirtyPixelCount;
    private int remainingDirtyPixelCount;

    public float Progress { get; private set; }
    public bool IsFullyClean => Progress >= 0.999f;

    public void BuildSneaker()
    {
        ClearChildren();
        CreateBackground();
        CreateSneakerBase();
        CreateDirtOverlay();
    }

    public float CleanAt(Vector2 localUv, float radiusPixels, float strength)
    {
        if (dirtTexture == null || alpha == null || shoeAlpha == null) return 0f;
        int cx = Mathf.RoundToInt(Mathf.Clamp01(localUv.x) * (textureWidth - 1));
        int cy = Mathf.RoundToInt(Mathf.Clamp01(localUv.y) * (textureHeight - 1));
        int radius = Mathf.Max(1, Mathf.RoundToInt(radiusPixels));
        float radiusSq = radius * radius;
        float removed = 0f;

        for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(textureHeight - 1, cy + radius); y++)
        for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(textureWidth - 1, cx + radius); x++)
        {
            float dx = x - cx, dy = y - cy, distSq = dx * dx + dy * dy;
            if (distSq > radiusSq) continue;
            int index = y * textureWidth + x;
            if (shoeAlpha[index] == 0 || alpha[index] == 0) continue;
            float d = Mathf.Sqrt(distSq) / radius;
            float falloff = 1f - Mathf.SmoothStep(0f, 1f, d);
            int oldA = alpha[index];
            int remove = Mathf.Clamp(Mathf.RoundToInt(255f * strength * falloff), 1, 255);
            int nextA = Mathf.Max(0, oldA - remove);
            alpha[index] = (byte)nextA;
            dirtPixels[index].a = (byte)nextA;
            removed += oldA - nextA;
        }

        if (removed > 0f)
        {
            RecalculateProgress();
            dirtTexture.SetPixels32(dirtPixels);
            dirtTexture.Apply(false, false);
        }
        return removed;
    }

    public Vector2 WorldToUv(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        if (dirtRenderer == null || dirtRenderer.sprite == null) return new Vector2(-1f, -1f);
        Bounds b = dirtRenderer.sprite.bounds;
        return new Vector2(Mathf.InverseLerp(b.min.x, b.max.x, local.x), Mathf.InverseLerp(b.min.y, b.max.y, local.y));
    }

    private void CreateSneakerBase()
    {
        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] p = new Color32[textureWidth * textureHeight];
        for (int i = 0; i < p.Length; i++) p[i] = new Color32(0, 0, 0, 0);

        Color32 upper = new Color32(232, 238, 240, 255);
        Color32 upperShadow = new Color32(198, 209, 213, 255);
        Color32 panel = new Color32(53, 104, 121, 255);
        Color32 panelLight = new Color32(77, 132, 146, 255);
        Color32 sole = new Color32(249, 248, 242, 255);
        Color32 soleShadow = new Color32(211, 213, 207, 255);

        Polygon(p, new[] { P(105,176), P(135,286), P(212,337), P(318,342), P(414,282), P(474,226), P(560,203), P(590,174), P(562,153), P(430,161), P(326,188), P(210,174) }, upper);
        Ellipse(p, 122, 167, 76, 126, upperShadow);
        Polygon(p, new[] { P(190,174), P(270,190), P(343,181), P(412,168), P(450,224), P(372,279), P(270,292), P(208,255) }, panel);
        Polygon(p, new[] { P(105,176), P(145,286), P(208,312), P(206,255), P(166,202) }, panelLight);
        Polygon(p, new[] { P(205,255), P(275,294), P(373,281), P(449,224), P(412,281), P(321,326), P(215,314) }, upperShadow);
        Polygon(p, new[] { P(275,294), P(330,312), P(390,282), P(449,224), P(416,280), P(340,326) }, new Color32(219,226,228,255));

        // Tongue and padded collar.
        Ellipse(p, 132, 280, 105, 76, new Color32(43,82,95,255));
        Polygon(p, new[] { P(190,254), P(250,285), P(282,397), P(228,417), P(194,318) }, panelLight);
        Polygon(p, new[] { P(205,285), P(251,300), P(270,383), P(238,395), P(218,322) }, new Color32(235,240,241,255));

        // Sole.
        RoundRect(p, 72, 118, 500, 78, 30, sole);
        RoundRect(p, 86, 128, 472, 38, 16, soleShadow);
        RoundRect(p, 86, 139, 472, 48, 20, sole);
        for (int x = 110; x < 550; x += 34) Line(p, x, 142, x - 7, 181, 3, new Color32(190,194,188,255));

        // Laces and eyelets.
        for (int row = 0; row < 5; row++)
        {
            int y = 296 + row * 19;
            Ellipse(p, 218, y - 4, 8, 8, new Color32(82,92,95,255));
            Ellipse(p, 272, y - 4, 8, 8, new Color32(82,92,95,255));
            Line(p, 222, y, 270, y + 9, 5, new Color32(250,250,247,255));
            Line(p, 272, y + 9, 222, y + 18, 5, new Color32(250,250,247,255));
        }
        for (int x = 290; x < 438; x += 13) Line(p, x, 208, x + 5, 203, 2, new Color32(176,188,191,255));

        // Side stripes.
        Line(p, 312, 205, 354, 252, 9, new Color32(238,243,243,255));
        Line(p, 334, 200, 376, 244, 9, new Color32(238,243,243,255));
        Line(p, 356, 197, 398, 237, 9, new Color32(238,243,243,255));
        Ellipse(p, 470, 188, 92, 52, new Color32(244,247,247,255));
        RoundRect(p, 105, 284, 28, 72, 10, new Color32(43,82,95,255));

        // Keep the texture CPU-readable because the runtime dirt system samples its pixels.
        tex.SetPixels32(p);
        tex.Apply(false, false);

        GameObject shoe = new GameObject("Sneaker Surface");
        shoe.transform.SetParent(transform, false);
        SpriteRenderer r = shoe.AddComponent<SpriteRenderer>();
        r.sprite = Sprite.Create(tex, new Rect(0, 0, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 100f);
        r.sortingOrder = 0;
    }

    private void CreateDirtOverlay()
    {
        SpriteRenderer baseRenderer = transform.Find("Sneaker Surface").GetComponent<SpriteRenderer>();
        Color32[] basePixels = baseRenderer.sprite.texture.GetPixels32();
        shoeAlpha = new byte[basePixels.Length];
        for (int i = 0; i < basePixels.Length; i++) shoeAlpha[i] = basePixels[i].a;

        dirtTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        dirtTexture.filterMode = FilterMode.Bilinear;
        dirtTexture.wrapMode = TextureWrapMode.Clamp;
        alpha = new byte[textureWidth * textureHeight];
        dirtPixels = new Color32[textureWidth * textureHeight];
        Random.InitState(randomSeed);

        for (int i = 0; i < 145; i++)
        {
            int cx = Random.Range(92, 565), cy = Random.Range(150, 330), radius = Random.Range(7, 28);
            Color32 c = Color32.Lerp(new Color32(66,52,42,175), new Color32(132,91,54,220), Random.value);
            Stamp(cx, cy, radius, c);
        }
        for (int i = 0; i < 45; i++)
            Stamp(Random.Range(420, 565), Random.Range(150, 225), Random.Range(5, 18), new Color32(82,65,51,205));

        dirtTexture.SetPixels32(dirtPixels);
        dirtTexture.Apply(false, false);
        GameObject dirt = new GameObject("Dirt Mask");
        dirt.transform.SetParent(transform, false);
        dirtRenderer = dirt.AddComponent<SpriteRenderer>();
        dirtRenderer.sprite = Sprite.Create(dirtTexture, new Rect(0, 0, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 100f);
        dirtRenderer.sortingOrder = 2;
        RecalculateProgress();
    }

    private void Stamp(int cx, int cy, int radius, Color32 color)
    {
        for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(textureHeight - 1, cy + radius); y++)
        for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(textureWidth - 1, cx + radius); x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / radius;
            if (d > 1f) continue;
            int idx = y * textureWidth + x;
            if (shoeAlpha[idx] == 0) continue;
            byte a = (byte)Mathf.RoundToInt(color.a * (1f - d));
            if (a > alpha[idx])
            {
                alpha[idx] = a;
                dirtPixels[idx] = new Color32(color.r, color.g, color.b, a);
            }
        }
    }

    private void RecalculateProgress()
    {
        if (initialDirtyPixelCount == 0)
        {
            for (int i = 0; i < alpha.Length; i++) if (alpha[i] > 8) initialDirtyPixelCount++;
        }
        remainingDirtyPixelCount = 0;
        for (int i = 0; i < alpha.Length; i++) if (alpha[i] > 8) remainingDirtyPixelCount++;
        Progress = initialDirtyPixelCount == 0 ? 1f : 1f - Mathf.Clamp01((float)remainingDirtyPixelCount / initialDirtyPixelCount);
    }

    private static Vector2 P(float x, float y) => new Vector2(x, y);

    private static void Ellipse(Color32[] p, float cx, float cy, float rx, float ry, Color32 color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), maxX = Mathf.Min(639, Mathf.CeilToInt(cx + rx));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry)), maxY = Mathf.Min(479, Mathf.CeilToInt(cy + ry));
        for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
        {
            float nx = (x - cx) / rx, ny = (y - cy) / ry;
            if (nx * nx + ny * ny <= 1f) p[y * 640 + x] = color;
        }
    }

    private static void RoundRect(Color32[] p, int x, int y, int w, int h, int r, Color32 color)
    {
        for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++)
        {
            int qx = xx < x + r ? x + r : (xx >= x + w - r ? x + w - r - 1 : xx);
            int qy = yy < y + r ? y + r : (yy >= y + h - r ? y + h - r - 1 : yy);
            if ((xx - qx) * (xx - qx) + (yy - qy) * (yy - qy) <= r * r) p[yy * 640 + xx] = color;
        }
    }

    private static void Line(Color32[] p, int x0, int y0, int x1, int y1, int thickness, Color32 color)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            int r = Mathf.Max(1, thickness / 2);
            for (int oy = -r; oy <= r; oy++) for (int ox = -r; ox <= r; ox++)
                if (x + ox >= 0 && x + ox < 640 && y + oy >= 0 && y + oy < 480) p[(y + oy) * 640 + x + ox] = color;
        }
    }

    private static void Polygon(Color32[] p, Vector2[] points, Color32 color)
    {
        float minX = points[0].x, maxX = points[0].x, minY = points[0].y, maxY = points[0].y;
        foreach (Vector2 q in points)
        {
            minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x);
            minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y);
        }
        for (int y = Mathf.Max(0, Mathf.FloorToInt(minY)); y <= Mathf.Min(479, Mathf.CeilToInt(maxY)); y++)
        for (int x = Mathf.Max(0, Mathf.FloorToInt(minX)); x <= Mathf.Min(639, Mathf.CeilToInt(maxX)); x++)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                if (((points[i].y > y) != (points[j].y > y)) &&
                    x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                    inside = !inside;
            }
            if (inside) p[y * 640 + x] = color;
        }
    }

    private void CreateBackground()
    {
        Texture2D tex = new Texture2D(256, 96, TextureFormat.RGBA32, false);
        Color32[] p = new Color32[256 * 96];
        for (int y = 0; y < 96; y++) for (int x = 0; x < 256; x++)
        {
            float dx = (x - 128f) / 128f, dy = (y - 48f) / 48f;
            byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(1f - dx * dx - dy * dy) * 45f);
            p[y * 256 + x] = new Color32(50, 62, 66, a);
        }
        tex.SetPixels32(p);
        tex.Apply(false, false);
        GameObject shadow = new GameObject("Soft Shadow");
        shadow.transform.SetParent(transform, false);
        SpriteRenderer r = shadow.AddComponent<SpriteRenderer>();
        r.sprite = Sprite.Create(tex, new Rect(0, 0, 256, 96), new Vector2(0.5f, 0.5f), 70f);
        r.transform.localPosition = new Vector3(0f, -1.75f, 0f);
        r.transform.localScale = new Vector3(2.1f, 0.8f, 1f);
        r.sortingOrder = -1;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
    }
}
