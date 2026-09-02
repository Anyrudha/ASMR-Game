using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates the restoration sequence. Each tool has a different job and the
/// same underlying DirtLayer can be reused by future restoration levels.
/// </summary>
public sealed class RestorationManager : MonoBehaviour
{
    public CleaningTool CurrentTool { get; private set; } = CleaningTool.Water;
    public float Progress { get; private set; }
    public int StageIndex => stage;
    public bool IsComplete { get; private set; }

    private DirtLayer layer;
    private Camera targetCamera;
    private int stage;
    private int foamTouches;
    private float lastInteraction;
    private ParticleSystem feedbackParticles;
    private Coroutine completionRoutine;

    public void Initialise(DirtLayer target, Camera camera)
    {
        layer = target;
        targetCamera = camera;
        stage = 0;
        foamTouches = 0;
        CurrentTool = CleaningTool.Water;
        Progress = 0f;
        IsComplete = false;

        AudioManager.Create();
        HapticManager.Create();
        CreateFeedbackParticles();
    }

    public void ApplyScreenPosition(Vector2 screenPosition)
    {
        if (IsComplete || layer == null || targetCamera == null) return;

        Vector3 world = targetCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z));
        world.z = 0f;

        Vector2 uv = layer.WorldToUv(world);
        if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) return;

        float before = layer.Progress;
        bool changed = false;

        switch (CurrentTool)
        {
            case CleaningTool.Water:
                // Water loosens a small amount of mud. The player has to actually
                // drag over the dirty area to progress.
                changed = layer.CleanAt(uv, 24f, 0.11f) > 0f;
                break;

            case CleaningTool.Foam:
                // Foam is preparation, not removal. We track real touches so this
                // stage feels like covering the shoe rather than deleting dirt.
                foamTouches = Mathf.Min(12, foamTouches + 1);
                changed = true;
                break;

            case CleaningTool.Brush:
                // The brush is the main restoration action.
                changed = layer.CleanAt(uv, 19f, 0.34f) > 0f;
                break;

            case CleaningTool.Rinse:
                changed = layer.CleanAt(uv, 27f, 0.18f) > 0f;
                break;

            case CleaningTool.Dryer:
                changed = layer.CleanAt(uv, 18f, 0.08f) > 0f;
                break;
        }

        UpdateProgressAndStage();
        SpawnFeedback(world, changed);

        if (changed && Time.unscaledTime - lastInteraction > 0.12f)
        {
            AudioManager.Play(CurrentTool);
            HapticManager.Light();
            lastInteraction = Time.unscaledTime;
        }

        if (Progress >= 0.999f && stage == 4 && completionRoutine == null)
            completionRoutine = StartCoroutine(CompleteAfterPause());
    }

    public void SetTool(CleaningTool tool)
    {
        if (IsComplete) return;
        // During the guided prototype, tools unlock in order. This prevents the
        // player from accidentally skipping the satisfying sequence.
        if ((int)tool <= stage)
            CurrentTool = tool;
    }

    private void UpdateProgressAndStage()
    {
        float dirt = layer.Progress;

        switch (stage)
        {
            case 0: // Water: 0 -> 20% overall, requires 10% dirt removed.
                Progress = Mathf.Lerp(0f, 0.20f, Mathf.InverseLerp(0f, 0.10f, dirt));
                if (dirt >= 0.10f)
                {
                    AdvanceStage(1);
                    return;
                }
                break;

            case 1: // Foam: 20 -> 25% overall.
                Progress = Mathf.Lerp(0.20f, 0.25f, foamTouches / 12f);
                if (foamTouches >= 12)
                {
                    AdvanceStage(2);
                    return;
                }
                break;

            case 2: // Brush: 25 -> 80% overall.
                Progress = Mathf.Lerp(0.25f, 0.80f, Mathf.InverseLerp(0.10f, 0.78f, dirt));
                if (dirt >= 0.78f)
                {
                    AdvanceStage(3);
                    return;
                }
                break;

            case 3: // Rinse: 80 -> 93% overall.
                Progress = Mathf.Lerp(0.80f, 0.93f, Mathf.InverseLerp(0.78f, 0.93f, dirt));
                if (dirt >= 0.93f)
                {
                    AdvanceStage(4);
                    return;
                }
                break;

            case 4: // Dryer: final 7%.
                Progress = Mathf.Lerp(0.93f, 1f, Mathf.InverseLerp(0.93f, 1f, dirt));
                if (dirt >= 0.999f)
                    Progress = 1f;
                break;
        }
    }

    private void AdvanceStage(int nextStage)
    {
        stage = Mathf.Clamp(nextStage, 0, 4);
        CurrentTool = (CleaningTool)stage;
        AudioManager.PlayStageAdvance(CurrentTool);
        HapticManager.StageAdvance();
    }

    private void SpawnFeedback(Vector3 world, bool changed)
    {
        if (feedbackParticles == null || !changed) return;

        var emit = new ParticleSystem.EmitParams
        {
            position = world,
            startSize = CurrentTool == CleaningTool.Brush ? 0.07f : 0.11f,
            startLifetime = 0.3f,
            startSpeed = 0.25f,
            startColor = CurrentTool == CleaningTool.Foam
                ? Color.white
                : new Color(0.75f, 0.9f, 1f, 0.85f)
        };
        feedbackParticles.Emit(emit, 1);
    }

    private IEnumerator CompleteAfterPause()
    {
        IsComplete = true;
        yield return new WaitForSecondsRealtime(0.9f);
        AudioManager.PlayCompletion();
        HapticManager.Completion();
        UIManager.ShowCompletion();
    }

    private void CreateFeedbackParticles()
    {
        GameObject particleObject = new GameObject("Tool Feedback Particles");
        feedbackParticles = particleObject.AddComponent<ParticleSystem>();
        var main = feedbackParticles.main;
        main.playOnAwake = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 0.25f;
        main.startSize = 0.1f;
        var emission = feedbackParticles.emission;
        emission.rateOverTime = 0f;
    }
}
