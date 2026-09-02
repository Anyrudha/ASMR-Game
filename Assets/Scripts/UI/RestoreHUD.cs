using UnityEngine;
using UnityEngine.UI;

public sealed class RestoreHUD : MonoBehaviour
{
    private RestorationManager manager;
    private Text progressText;
    private Text instructionText;
    private Text toolNameText;
    private Image progressFill;
    private readonly Image[] toolCards = new Image[5];
    private readonly Image[] toolIcons = new Image[5];
    private readonly Text[] toolLabels = new Text[5];
    private readonly Text[] toolStates = new Text[5];
    private GameObject completion;
    private bool built;
    private float displayedProgress;
    private static Sprite roundedSprite;

    private static readonly Color Ink = new Color32(31, 45, 51, 255);
    private static readonly Color Muted = new Color32(104, 121, 126, 255);
    private static readonly Color Accent = new Color32(45, 158, 177, 255);
    private static readonly Color PanelWhite = new Color(1f, 1f, 1f, .94f);
    private static readonly string[] Names = { "WATER", "FOAM", "BRUSH", "RINSE", "DRY" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        GameObject go = new GameObject("Restore HUD");
        go.AddComponent<RestoreHUD>();
    }

    private void Start() => TryInitialise();

    private void TryInitialise()
    {
        if (built) return;
        if (manager == null) manager = FindFirstObjectByType<RestorationManager>();
        if (manager == null) return;
        HideLegacyUI();
        Build();
        built = true;
    }

    private void HideLegacyUI()
    {
        GameObject old = GameObject.Find("UI Manager");
        if (old == null) return;
        Canvas c = old.GetComponentInChildren<Canvas>();
        if (c != null) c.gameObject.SetActive(false);
    }

    private void Build()
    {
        GameObject canvasObject = new GameObject("Restore HUD Canvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = .5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        BuildHeader(canvasObject.transform);
        BuildInstruction(canvasObject.transform);
        BuildToolDock(canvasObject.transform);
        BuildCompletion(canvasObject.transform);
    }

    private void BuildHeader(Transform parent)
    {
        Image panel = RoundedPanel(parent, new Vector2(.5f, .925f), new Vector2(920, 124), PanelWhite);
        Text(panel.transform, "RESTORE", new Vector2(.43f, .64f), new Vector2(430, 34), 21, FontStyle.Bold, Ink);
        progressText = Text(panel.transform, "0%", new Vector2(.82f, .64f), new Vector2(130, 34), 20, FontStyle.Bold, Accent);
        Text(panel.transform, "TAKE YOUR TIME", new Vector2(.5f, .34f), new Vector2(360, 22), 9, FontStyle.Bold, Muted);

        Image track = RoundedPanel(panel.transform, new Vector2(.5f, .08f), new Vector2(820, 10), new Color32(224, 232, 233, 255));
        progressFill = RoundedPanel(track.transform, new Vector2(0f, .5f), new Vector2(820, 10), Accent);
        progressFill.rectTransform.anchorMin = new Vector2(0f, 0f);
        progressFill.rectTransform.anchorMax = new Vector2(1f, 1f);
        progressFill.rectTransform.offsetMin = Vector2.zero;
        progressFill.rectTransform.offsetMax = Vector2.zero;
        progressFill.rectTransform.pivot = new Vector2(0f, .5f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
    }

    private void BuildInstruction(Transform parent)
    {
        Image panel = RoundedPanel(parent, new Vector2(.5f, .755f), new Vector2(900, 88), new Color(1f, 1f, 1f, .76f));
        Text(panel.transform, "NOW", new Vector2(.13f, .50f), new Vector2(90, 22), 8, FontStyle.Bold, Muted);
        toolNameText = Text(panel.transform, "WATER", new Vector2(.30f, .50f), new Vector2(210, 30), 15, FontStyle.Bold, Ink);
        instructionText = Text(panel.transform, "Drag across the muddy areas", new Vector2(.67f, .50f), new Vector2(510, 46), 14, FontStyle.Normal, Ink);
    }

    private void BuildToolDock(Transform parent)
    {
        Image dock = RoundedPanel(parent, new Vector2(.5f, .105f), new Vector2(1010, 196), new Color32(24, 39, 44, 247));
        Text(dock.transform, "TOOLS", new Vector2(.5f, .83f), new Vector2(220, 20), 9, FontStyle.Bold, new Color(1, 1, 1, .45f));

        for (int i = 0; i < 5; i++)
        {
            int index = i;
            Image card = RoundedPanel(dock.transform, new Vector2(.5f, .40f), new Vector2(166, 116), new Color(1, 1, 1, .075f));
            card.rectTransform.anchoredPosition = new Vector2(-400 + i * 200, -3);
            toolCards[i] = card;

            Image icon = RoundedPanel(card.transform, new Vector2(.5f, .68f), new Vector2(48, 48), new Color(1, 1, 1, .10f));
            icon.sprite = CreateIconSprite(i);
            icon.preserveAspect = true;
            toolIcons[i] = icon;

            toolLabels[i] = Text(card.transform, Names[i], new Vector2(.5f, .28f), new Vector2(150, 22), 9, FontStyle.Bold, Color.white);
            toolStates[i] = Text(card.transform, "LOCKED", new Vector2(.5f, .10f), new Vector2(150, 18), 7, FontStyle.Bold, new Color(1, 1, 1, .32f));

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 1f);
            colors.pressedColor = new Color(.88f, .96f, .97f, 1f);
            colors.disabledColor = new Color(1, 1, 1, .55f);
            button.colors = colors;
            button.onClick.AddListener(() => manager.SetTool((CleaningTool)index));
        }
    }

    private void BuildCompletion(Transform parent)
    {
        completion = new GameObject("Completion");
        completion.transform.SetParent(parent, false);
        RectTransform root = completion.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        Image dim = completion.AddComponent<Image>();
        dim.color = new Color(.02f, .07f, .08f, .58f);
        Image card = RoundedPanel(completion.transform, new Vector2(.5f, .5f), new Vector2(850, 500), PanelWhite);
        Text(card.transform, "RESTORED", new Vector2(.5f, .67f), new Vector2(650, 60), 36, FontStyle.Bold, Ink);
        Text(card.transform, "A little care makes a big difference.", new Vector2(.5f, .48f), new Vector2(700, 50), 18, FontStyle.Normal, Muted);
        Text(card.transform, "✦   ✦   ✦", new Vector2(.5f, .28f), new Vector2(500, 55), 24, FontStyle.Bold, Accent);
        completion.SetActive(false);
    }

    private void Update()
    {
        TryInitialise();
        if (!built) return;
        float target = Mathf.Clamp01(manager.Progress);
        displayedProgress = Mathf.MoveTowards(displayedProgress, target, Time.deltaTime * .9f);
        progressText.text = Mathf.RoundToInt(displayedProgress * 100f) + "%";
        progressFill.fillAmount = displayedProgress;
        toolNameText.text = manager.CurrentTool.Label();
        instructionText.text = GetInstruction(manager.CurrentTool);

        int stage = Mathf.Clamp(manager.StageIndex, 0, 4);
        for (int i = 0; i < 5; i++)
        {
            bool active = i == stage;
            bool unlocked = i <= manager.StageIndex;
            toolCards[i].color = active ? Accent : unlocked ? new Color(1, 1, 1, .13f) : new Color(1, 1, 1, .065f);
            toolIcons[i].color = active ? Color.white : unlocked ? new Color(1, 1, 1, .72f) : new Color(1, 1, 1, .28f);
            toolStates[i].text = active ? "ACTIVE" : unlocked ? "READY" : "LOCKED";
            toolStates[i].color = active ? new Color(1, 1, 1, .78f) : new Color(1, 1, 1, .32f);
            toolCards[i].GetComponent<Button>().interactable = unlocked && !manager.IsComplete;
        }
        if (manager.IsComplete && !completion.activeSelf) completion.SetActive(true);
    }

    private static string GetInstruction(CleaningTool tool)
    {
        switch (tool)
        {
            case CleaningTool.Water: return "Drag across the muddy areas";
            case CleaningTool.Foam: return "Spread a soft layer of foam";
            case CleaningTool.Brush: return "Slowly scrub until the dirt melts away";
            case CleaningTool.Rinse: return "Rinse the surface until it shines";
            case CleaningTool.Dryer: return "Gently dry the restored sneaker";
            default: return "Restore it at your own pace";
        }
    }

    private static Image RoundedPanel(Transform parent, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 64;
        const float radius = 14f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
            float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            pixels[y * size + x] = new Color(1f, 1f, 1f, distance <= radius ? 1f : 0f);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64f, 0, SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
        return roundedSprite;
    }

    private static Text Text(Transform parent, string value, Vector2 anchor, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        Text text = go.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Sprite CreateIconSprite(int tool)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(1, 1, 1, 0);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = x - 31.5f, py = y - 31.5f;
            float r = Mathf.Sqrt(px * px + py * py);
            bool on = false;
            switch (tool)
            {
                case 0:
                    float dx = px * .9f, dy = py + 7f;
                    on = (dx * dx + dy * dy < 15f * 15f && py < 13f) || (Mathf.Abs(px) < 3f && py > 8f && py < 27f);
                    break;
                case 1:
                    on = Mathf.Abs(r - 13f) < 3f || Mathf.Abs(Mathf.Sqrt((px + 13) * (px + 13) + (py - 8) * (py - 8)) - 8f) < 3f || Mathf.Abs(Mathf.Sqrt((px - 12) * (px - 12) + (py + 9) * (py + 9)) - 6f) < 3f;
                    break;
                case 2:
                    on = Mathf.Abs(px + py * .7f) < 5f && py > -17f && py < 17f || Mathf.Abs(py - 19f) < 5f && Mathf.Abs(px) < 22f;
                    break;
                case 3:
                    on = Mathf.Abs(px + 17f) < 4f && Mathf.Abs(py) < 20f || px > -6f && px < 21f && Mathf.Abs(py - px * .55f) < 3f || px > -6f && px < 21f && Mathf.Abs(py + px * .55f) < 3f;
                    break;
                default:
                    on = Mathf.Abs(px + 14f) < 4f && Mathf.Abs(py) < 17f || Mathf.Abs(py) < 4f && px > -10f && px < 21f || Mathf.Abs(py - 10f) < 3f && px > -4f && px < 20f || Mathf.Abs(py + 10f) < 3f && px > -4f && px < 20f;
                    break;
            }
            if (on) pixels[y * size + x] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64f);
    }
}
