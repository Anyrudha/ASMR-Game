using UnityEngine;

/// <summary>Continuous, movement-driven ASMR mixer. Replace procedural clips with recorded foley later.</summary>
public static class AudioManager
{
    private static AudioSource loopSource;
    private static AudioSource oneShotSource;
    private static AudioClip[] loops;
    private static AudioClip stageAdvance;
    private static AudioClip completion;
    private static bool initialised;
    private static CleaningTool activeTool = CleaningTool.Water;

    public static void Create()
    {
        if (initialised) return;
        initialised = true;
        GameObject go = new GameObject("Audio Manager");
        Object.DontDestroyOnLoad(go);
        loopSource = go.AddComponent<AudioSource>();
        oneShotSource = go.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.spatialBlend = 0f;
        oneShotSource.playOnAwake = false;
        oneShotSource.spatialBlend = 0f;
        loops = new[] { CreateWater(), CreateFoam(), CreateBrush(), CreateRinse(), CreateDryer() };
        stageAdvance = Tone("Stage Advance", 0.38f, new[] { 660f, 880f }, 0.16f);
        completion = Tone("Completion", 0.9f, new[] { 523.25f, 659.25f, 783.99f }, 0.14f);
    }

    public static void SetInteraction(CleaningTool tool, bool active, float intensity)
    {
        if (!initialised) Create();
        if (!active || intensity <= 0.01f)
        {
            loopSource.volume = Mathf.MoveTowards(loopSource.volume, 0f, Time.unscaledDeltaTime * 5f);
            if (loopSource.volume <= 0.001f && loopSource.isPlaying) loopSource.Stop();
            return;
        }

        int index = Mathf.Clamp((int)tool, 0, loops.Length - 1);
        if (loopSource.clip != loops[index])
        {
            loopSource.Stop();
            loopSource.clip = loops[index];
            loopSource.Play();
            activeTool = tool;
        }
        float i = Mathf.Clamp01(intensity);
        loopSource.volume = Mathf.MoveTowards(loopSource.volume, Mathf.Lerp(0.06f, 0.34f, i), Time.unscaledDeltaTime * 6f);
        loopSource.pitch = Mathf.Lerp(0.94f, 1.07f, i);
    }

    public static void Play(CleaningTool tool, float intensity = 0.65f) => SetInteraction(tool, true, intensity);

    public static void PlayStageAdvance(CleaningTool tool)
    {
        if (!initialised) Create();
        oneShotSource.pitch = 0.94f + (int)tool * 0.03f;
        oneShotSource.PlayOneShot(stageAdvance, 0.2f);
    }

    public static void PlayCompletion()
    {
        if (!initialised) Create();
        SetInteraction(activeTool, false, 0f);
        oneShotSource.pitch = 1f;
        oneShotSource.PlayOneShot(completion, 0.4f);
    }

    private static AudioClip CreateWater() => Noise("Water", 2.4f, 11, (t, r) => ((float)r.NextDouble() * 2f - 1f) * 0.34f + Mathf.Sin(t * 31f) * 0.08f);
    private static AudioClip CreateFoam() => Noise("Foam", 2.2f, 17, (t, r) => ((float)r.NextDouble() * 2f - 1f) * 0.14f + Mathf.Sin(t * 21f) * 0.05f);
    private static AudioClip CreateBrush() => Noise("Brush", 2.0f, 23, (t, r) => (((float)r.NextDouble() * 2f - 1f) * 0.28f + Mathf.Sin(t * 145f) * 0.13f + Mathf.Sin(t * 233f) * 0.06f) * (0.72f + 0.28f * Mathf.Sin(t * 8.5f)));
    private static AudioClip CreateRinse() => Noise("Rinse", 2.3f, 29, (t, r) => ((float)r.NextDouble() * 2f - 1f) * 0.3f + Mathf.Sin(t * 53f) * 0.07f);
    private static AudioClip CreateDryer() => Noise("Dryer", 2.0f, 37, (t, r) => ((float)r.NextDouble() * 2f - 1f) * 0.24f + Mathf.Sin(t * 92f) * 0.07f + Mathf.Sin(t * 184f) * 0.03f);

    private delegate float Generator(float time, System.Random random);
    private static AudioClip Noise(string name, float seconds, int seed, Generator generator)
    {
        const int rate = 44100;
        int count = Mathf.RoundToInt(rate * seconds);
        AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
        float[] data = new float[count];
        System.Random random = new System.Random(seed);
        for (int n = 0; n < count; n++)
        {
            float t = n / (float)rate;
            float edge = Mathf.Min(1f, Mathf.Min(t, seconds - t) * 20f);
            data[n] = generator(t, random) * edge * 0.55f;
        }
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip Tone(string name, float seconds, float[] frequencies, float amplitude)
    {
        const int rate = 44100;
        int count = Mathf.RoundToInt(rate * seconds);
        AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
        float[] data = new float[count];
        for (int n = 0; n < count; n++)
        {
            float t = n / (float)rate;
            float env = Mathf.Sin(Mathf.PI * t / seconds);
            float value = 0f;
            foreach (float f in frequencies) value += Mathf.Sin(2f * Mathf.PI * f * t) / frequencies.Length;
            data[n] = value * amplitude * env;
        }
        clip.SetData(data, 0);
        return clip;
    }
}
