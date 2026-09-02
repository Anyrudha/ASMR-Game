using UnityEngine;

public enum CleaningTool { Water, Foam, Brush, Rinse, Dryer }

public static class CleaningToolRules
{
    public static string Label(this CleaningTool tool) => tool == CleaningTool.Dryer ? "AIR DRY" : tool.ToString().ToUpperInvariant();
    public static bool CanClean(this CleaningTool tool, DirtPatch patch) => tool != CleaningTool.Foam || patch != null;
}