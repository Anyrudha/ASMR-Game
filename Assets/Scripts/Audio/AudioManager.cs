using UnityEngine;

/// <summary>Layered procedural ASMR mixer. Designed to feel like continuous tactile foley rather than UI sound effects.</summary>
public static class AudioManager
{
    private static AudioSource bedSource;
    private static AudioSource textureSource;
    private static AudioSource detailSource;
    private static AudioSource oneShotSource;
    private static AudioClip[][] layers;
    private static AudioClip stageAdvance;
    private static AudioClip completion;
    private static bool initialised;
    private static CleaningTool activeTool = CleaningTool.Water;
    private static float smoothedIntensity;

    public static void Create()
    {
        if (initialised) return;
        initialised = true;
        GameObject go = new GameObject("Audio Manager");
        Object.DontDestroyOnLoad(go);

        bedSource = MakeSource(go, "ASMR Bed");
        textureSource = MakeSource(go, "ASMR Texture");
        detailSource = MakeSource(go, "ASMR Detail");
        oneShotSource = MakeSource(go, "Feedback");

        layers = new[]
        {
            new[] { CreateWaterBed(), CreateWaterTexture(), CreateWaterDetail() },
            new[] { CreateFoamBed(), CreateFoamTexture(), CreateFoamDetail() },
            new[] { CreateBrushBed(), CreateBrushTexture(), CreateBrushDetail() },
            new[] { CreateRinseBed(), CreateRinseTexture(), CreateRinseDetail() },
            new[] { CreateDryerBed(), CreateDryerTexture(), CreateDryerDetail() }
        };
        stageAdvance = Tone("Stage Advance", 0.34f, new[] { 523.25f, 659.25f, 783.99f }, 0.10f);
        completion = Tone("Completion", 1.1f, new[] { 392f, 493.88f, 587.33f, 783.99f }, 0.11f);
    }

    private static AudioSource MakeSource(GameObject go, string name)
    {
        AudioSource s = go.AddComponent<AudioSource>();
        s.name = name;
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.volume = 0f;
        return s;
    }

    public static void SetInteraction(CleaningTool tool, bool active, float intensity)
    {
        if (!initialised) Create();
        if (!active || intensity <= 0.01f)
        {
            smoothedIntensity = Mathf.MoveTowards(smoothedIntensity, 0f, Time.unscaledDeltaTime * 7f);
            FadeOut();
            return;
        }

        int index = Mathf.Clamp((int)tool, 0, layers.Length - 1);
        if (bedSource.clip != layers[index][0])
        {
            StopLayers();
            bedSource.clip = layers[index][0];
            textureSource.clip = layers[index][1];
            detailSource.clip = layers[index][2];
            bedSource.Play();
            textureSource.Play();
            detailSource.Play();
            activeTool = tool;
        }

        float target = Mathf.Clamp01(intensity);
        smoothedIntensity = Mathf.Lerp(smoothedIntensity, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
        float i = smoothedIntensity;

        // The sound should stay intimate. Movement changes texture, not just loudness.
        bedSource.volume = Mathf.Lerp(0.035f, 0.11f, i);
        textureSource.volume = Mathf.Lerp(0.015f, 0.16f, i);
        detailSource.volume = Mathf.Lerp(0.008f, 0.075f, i);
        bedSource.pitch = Mathf.Lerp(0.97f, 1.025f, i);
        textureSource.pitch = Mathf.Lerp(0.94f, 1.09f, i);
        detailSource.pitch = Mathf.Lerp(0.92f, 1.12f, i);
    }

    private static void FadeOut()
    {
        float step = Time.unscaledDeltaTime * 4.5f;
        bedSource.volume = Mathf.MoveTowards(bedSource.volume, 0f, step);
        textureSource.volume = Mathf.MoveTowards(textureSource.volume, 0f, step);
        detailSource.volume = Mathf.MoveTowards(detailSource.volume, 0f, step * 1.4f);
        if (bedSource.volume <= 0.001f) StopLayers();
    }

    private static void StopLayers()
    {
        bedSource.Stop(); textureSource.Stop(); detailSource.Stop();
        bedSource.clip = null; textureSource.clip = null; detailSource.clip = null;
    }

    public static void Play(CleaningTool tool, float intensity = 0.65f) => SetInteraction(tool, true, intensity);

    public static void PlayStageAdvance(CleaningTool tool)
    {
        if (!initialised) Create();
        oneShotSource.pitch = 0.92f + (int)tool * 0.025f;
        oneShotSource.PlayOneShot(stageAdvance, 0.16f);
    }

    public static void PlayCompletion()
    {
        if (!initialised) Create();
        SetInteraction(activeTool, false, 0f);
        oneShotSource.pitch = 0.96f;
        oneShotSource.PlayOneShot(completion, 0.26f);
    }

    // Water: airy spray bed + irregular droplets + tiny surface splashes.
    private static AudioClip CreateWaterBed() => FilteredNoise("Water Bed", 3.0f, 11, 0.22f, 0.035f, 0.72f);
    private static AudioClip CreateWaterTexture() => Droplets("Water Texture", 3.0f, 17, 0.42f, 0.12f);
    private static AudioClip CreateWaterDetail() => Grain("Water Detail", 3.0f, 23, 0.34f, 0.08f, 28f);

    // Foam: soft low hiss with tiny popping bubbles and rubbery squeaks.
    private static AudioClip CreateFoamBed() => FilteredNoise("Foam Bed", 3.0f, 31, 0.14f, 0.018f, 0.38f);
    private static AudioClip CreateFoamTexture() => BubbleTexture("Foam Texture", 3.0f, 37, 0.26f);
    private static AudioClip CreateFoamDetail() => Grain("Foam Detail", 3.0f, 41, 0.18f, 0.06f, 7f);

    // Brush: dry bristle bed + rhythmic bristle chatter + short micro squeaks.
    private static AudioClip CreateBrushBed() => FilteredNoise("Brush Bed", 3.0f, 47, 0.30f, 0.055f, 0.52f);
    private static AudioClip CreateBrushTexture() => BristleScrub("Brush Texture", 3.0f, 53);
    private static AudioClip CreateBrushDetail() => BristleDetails("Brush Detail", 3.0f, 59);

    // Rinse: deeper flowing water with intermittent drops.
    private static AudioClip CreateRinseBed() => FilteredNoise("Rinse Bed", 3.0f, 61, 0.34f, 0.025f, 0.62f);
    private static AudioClip CreateRinseTexture() => Droplets("Rinse Texture", 3.0f, 67, 0.32f, 0.07f);
    private static AudioClip CreateRinseDetail() => Grain("Rinse Detail", 3.0f, 71, 0.22f, 0.045f, 45f);

    // Dryer: warm broadband air, not an alarm-like high tone.
    private static AudioClip CreateDryerBed() => FilteredNoise("Dryer Bed", 3.0f, 73, 0.38f, 0.018f, 0.78f);
    private static AudioClip CreateDryerTexture() => FilteredNoise("Dryer Texture", 3.0f, 79, 0.25f, 0.04f, 0.55f);
    private static AudioClip CreateDryerDetail() => Grain("Dryer Detail", 3.0f, 83, 0.12f, 0.035f, 90f);

    private static AudioClip FilteredNoise(string name, float seconds, int seed, float amount, float low, float high)
    {
        const int rate = 44100; int count = Mathf.RoundToInt(rate * seconds);
        AudioClip clip = AudioClip.Create(name, count, 1, rate, false); float[] data = new float[count];
        System.Random r = new System.Random(seed); float lowState = 0f; float highState = 0f;
        for (int n = 0; n < count; n++)
        {
            float t=n/(float)rate; float white=(float)r.NextDouble()*2f-1f;
            lowState=Mathf.Lerp(lowState,white,low); highState=white-lowState;
            float shaped=lowState*(1f-high)+highState*high;
            float env=Mathf.Min(1f,Mathf.Min(t,seconds-t)*24f);
            data[n]=shaped*amount*env;
        }
        clip.SetData(data,0); return clip;
    }

    private static AudioClip Droplets(string name,float seconds,int seed,float amount,float density)
    {
        const int rate=44100; int count=Mathf.RoundToInt(rate*seconds); AudioClip clip=AudioClip.Create(name,count,1,rate,false); float[] data=new float[count]; System.Random r=new System.Random(seed);
        int next=0;
        while(next<count){ next+=Mathf.Max(900,(int)(rate*(0.025f+r.NextDouble()*density))); if(next>=count)break; float amp=0.25f+(float)r.NextDouble()*0.75f; int len=(int)(rate*(0.018f+r.NextDouble()*0.035f)); float f=500f+(float)r.NextDouble()*2200f;
            for(int j=0;j<len&&next+j<count;j++){float t=j/(float)rate;float e=Mathf.Exp(-t*95f);data[next+j]+=Mathf.Sin(2f*Mathf.PI*f*t)*e*amp*amount;}
        }
        clip.SetData(data,0);return clip;
    }

    private static AudioClip BubbleTexture(string name,float seconds,int seed,float amount)
    {
        const int rate=44100;int count=Mathf.RoundToInt(rate*seconds);AudioClip clip=AudioClip.Create(name,count,1,rate,false);float[] data=new float[count];System.Random r=new System.Random(seed);
        for(int n=0;n<count;n++){float t=n/(float)rate;float v=0f; if(r.NextDouble()<0.00055){float f=180f+(float)r.NextDouble()*420f; v=(float)(r.NextDouble()*2-1)*Mathf.Sin(t*f)*0.15f;} data[n]=v*amount;}
        clip.SetData(data,0);return clip;
    }

    private static AudioClip BristleScrub(string name,float seconds,int seed)
    {
        const int rate=44100;int count=Mathf.RoundToInt(rate*seconds);AudioClip clip=AudioClip.Create(name,count,1,rate,false);float[] data=new float[count];System.Random r=new System.Random(seed);float lp=0f;
        for(int n=0;n<count;n++){float t=n/(float)rate;float chatter=Mathf.Sin(2f*Mathf.PI*(145f+18f*Mathf.Sin(t*5.5f))*t);float noise=(float)r.NextDouble()*2f-1f;lp=Mathf.Lerp(lp,noise,0.18f);float pulse=0.58f+0.42f*Mathf.Sin(t*2f*Mathf.PI*7f);data[n]=(chatter*0.06f+lp*0.34f)*pulse*0.42f;}
        clip.SetData(data,0);return clip;
    }

    private static AudioClip BristleDetails(string name,float seconds,int seed)
    {
        const int rate=44100;int count=Mathf.RoundToInt(rate*seconds);AudioClip clip=AudioClip.Create(name,count,1,rate,false);float[] data=new float[count];System.Random r=new System.Random(seed);int next=0;
        while(next<count){next+=Mathf.Max(1800,(int)(rate*(0.07+r.NextDouble()*0.13)));if(next>=count)break;int len=(int)(rate*(0.012+r.NextDouble()*0.022));float f=2200f+(float)r.NextDouble()*1800f;for(int j=0;j<len&&next+j<count;j++){float t=j/(float)rate;float e=Mathf.Exp(-t*150f);data[next+j]+=Mathf.Sin(2f*Mathf.PI*f*t)*e*0.035f;}}
        clip.SetData(data,0);return clip;
    }

    private static AudioClip Grain(string name,float seconds,int seed,float amount,float low,float rateHz)
    {
        const int sampleRate=44100;int count=Mathf.RoundToInt(sampleRate*seconds);AudioClip clip=AudioClip.Create(name,count,1,sampleRate,false);float[] data=new float[count];System.Random r=new System.Random(seed);float state=0f;
        for(int n=0;n<count;n++){float t=n/(float)sampleRate;float noise=(float)r.NextDouble()*2f-1f;state=Mathf.Lerp(state,noise,low);float mod=0.72f+0.28f*Mathf.Sin(t*rateHz);data[n]=state*amount*mod*0.35f;}
        clip.SetData(data,0);return clip;
    }

    private static AudioClip Tone(string name,float seconds,float[] frequencies,float amplitude)
    {
        const int rate=44100;int count=Mathf.RoundToInt(rate*seconds);AudioClip clip=AudioClip.Create(name,count,1,rate,false);float[] data=new float[count];
        for(int n=0;n<count;n++){float t=n/(float)rate;float env=Mathf.Sin(Mathf.PI*t/seconds);float value=0f;foreach(float f in frequencies)value+=Mathf.Sin(2f*Mathf.PI*f*t)/frequencies.Length;data[n]=value*amplitude*env;}
        clip.SetData(data,0);return clip;
    }
}
