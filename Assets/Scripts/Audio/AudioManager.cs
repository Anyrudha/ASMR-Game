using UnityEngine;

public static class AudioManager
{
    private static AudioSource source;
    private static AudioClip interaction;
    private static AudioClip stageAdvance;
    private static AudioClip completion;

    public static void Create()
    {
        if (source != null) return;

        GameObject audioObject = new GameObject("Audio Manager");
        source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.45f;

        interaction = CreateTone("ASMR Interaction", 0.09f, 420f, 0.07f);
        stageAdvance = CreateTone("Stage Advance", 0.28f, 660f, 0.10f);
        completion = CreateTone("Completion", 0.65f, 880f, 0.12f);
    }

    public static void Play(CleaningTool tool)
    {
        if (source == null) Create();
        source.pitch = tool == CleaningTool.Brush ? 0.9f : 1f;
        source.PlayOneShot(interaction, tool == CleaningTool.Brush ? 0.20f : 0.14f);
    }

    public static void PlayStageAdvance(CleaningTool tool)
    {
        if (source == null) Create();
        source.pitch = 1f + (int)tool * 0.06f;
        source.PlayOneShot(stageAdvance, 0.32f);
    }

    public static void PlayCompletion()
    {
        if (source == null) Create();
        source.pitch = 1f;
        source.PlayOneShot(completion, 0.55f);
    }

    private static AudioClip CreateTone(string name, float seconds, float frequency, float amplitude)
    {
        int sampleRate = 44100;
        int samples = Mathf.RoundToInt(sampleRate * seconds);
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-7f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope;
        }
        clip.SetData(data, 0);
        return clip;
    }
}
