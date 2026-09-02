using UnityEngine;
using UnityEngine.UI;

public sealed class UIManager : MonoBehaviour
{
    private static UIManager instance;
    private RestorationManager manager;
    private Text progressText;
    private Text instructionText;
    private Text toolText;
    private GameObject completion;

    public static void Create(RestorationManager restorationManager)
    {
        GameObject item = new GameObject("UI Manager");
        instance = item.AddComponent<UIManager>();
        instance.manager = restorationManager;
        instance.Build();
    }

    private void Build()
    {
        GameObject canvasObject = new GameObject("Canvas");
        canvasObject.transform.SetParent(transform);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObject.AddComponent<GraphicRaycaster>();

        progressText = AddText(canvasObject.transform, "RESTORATION  0%", new Vector2(0.5f, 0.93f), 34, Color.white);
        instructionText = AddText(canvasObject.transform, "Rinse the dirt away", new Vector2(0.5f, 0.84f), 22, Color.white);
        toolText = AddText(canvasObject.transform, "WATER", new Vector2(0.5f, 0.11f), 30, new Color(0.1f, 0.35f, 0.4f));

        completion = AddText(canvasObject.transform,
            "PERFECT RESTORATION\n\n✦  ✦  ✦\n\nTake a breath. You did it.",
            new Vector2(0.5f, 0.53f), 38, Color.white).gameObject;
        completion.SetActive(false);

        for (int index = 0; index < 5; index++)
            CreateToolButton(canvasObject.transform, (CleaningTool)index, index);
    }

    private void Update()
    {
        if (manager == null || progressText == null) return;

        progressText.text = "RESTORATION  " + Mathf.RoundToInt(manager.Progress * 100f) + "%";
        toolText.text = manager.CurrentTool.Label();
        instructionText.text = GetInstruction(manager.CurrentTool);
    }

    public static void ShowCompletion()
    {
        if (instance != null && instance.completion != null)
            instance.completion.SetActive(true);
    }

    private string GetInstruction(CleaningTool tool)
    {
        switch (tool)
        {
            case CleaningTool.Water: return "Drag across the muddy areas";
            case CleaningTool.Foam: return "Cover the sneaker with soft foam";
            case CleaningTool.Brush: return "Slowly scrub away the remaining dirt";
            case CleaningTool.Rinse: return "Rinse until the foam is gone";
            case CleaningTool.Dryer: return "Gently dry the restored sneaker";
            default: return "Restore it at your own pace";
        }
    }

    private void CreateToolButton(Transform parent, CleaningTool tool, int index)
    {
        GameObject item = new GameObject(tool.Label() + " Button");
        item.transform.SetParent(parent);
        RectTransform rect = item.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.17f);
        rect.anchoredPosition = new Vector2(-240f + index * 120f, 0f);
        rect.sizeDelta = new Vector2(112f, 54f);

        Image image = item.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.86f);
        Button button = item.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => manager.SetTool(tool));

        Text label = AddText(item.transform, tool.Label(), new Vector2(0.5f, 0.5f), 15, new Color(0.1f, 0.35f, 0.4f));
        label.rectTransform.sizeDelta = new Vector2(112f, 54f);
    }

    private static Text AddText(Transform parent, string text, Vector2 anchor, int size, Color color)
    {
        GameObject item = new GameObject("Label");
        item.transform.SetParent(parent);
        RectTransform rect = item.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800, 180);

        Text label = item.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }
}
