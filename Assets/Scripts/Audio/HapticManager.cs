using UnityEngine;

public static class HapticManager
{
    private static float lastPulse;

    public static void Create() { }

    public static void Light()
    {
        if (Time.unscaledTime - lastPulse < 0.16f) return;
        Handheld.Vibrate();
        lastPulse = Time.unscaledTime;
    }

    public static void StageAdvance()
    {
        Handheld.Vibrate();
        lastPulse = Time.unscaledTime;
    }

    public static void Completion()
    {
        Handheld.Vibrate();
        lastPulse = Time.unscaledTime;
    }
}
