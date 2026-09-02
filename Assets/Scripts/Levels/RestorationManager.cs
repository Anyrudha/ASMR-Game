using System.Collections;
using UnityEngine;

public sealed class RestorationManager : MonoBehaviour
{
    public CleaningTool CurrentTool { get; private set; } = CleaningTool.Water;
    public float Progress { get; private set; }
    public int StageIndex => stage;
    public bool IsComplete { get; private set; }

    private DirtLayer layer;
    private MeshDirtSurface meshSurface;
    private Camera targetCamera;
    private int stage;
    private int foamTouches;
    private float lastInteraction;
    private ParticleSystem feedbackParticles;
    private Coroutine completionRoutine;

    public void Initialise(DirtLayer target, Camera camera)
    {
        layer = target; meshSurface = null; targetCamera = camera;
        ResetState();
    }

    public void Initialise(MeshDirtSurface target, Camera camera)
    {
        meshSurface = target; layer = null; targetCamera = camera;
        ResetState();
    }

    private void ResetState()
    {
        stage = 0; foamTouches = 0; CurrentTool = CleaningTool.Water;
        Progress = 0f; IsComplete = false;
        AudioManager.Create(); HapticManager.Create(); CreateFeedbackParticles();
    }

    public void ApplyScreenPosition(Vector2 screenPosition) => ApplyScreenPosition(screenPosition, 0.6f);

    public void ApplyScreenPosition(Vector2 screenPosition, float intensity)
    {
        if (IsComplete || targetCamera == null) return;

        float i = Mathf.Clamp01(intensity);
        bool changed = false;
        Vector3 feedbackWorld = Vector3.zero;

        if (meshSurface != null)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;
            if (!hit.collider.transform.IsChildOf(meshSurface.transform) && hit.collider.transform != meshSurface.transform) return;
            feedbackWorld = hit.point;
            switch (CurrentTool)
            {
                case CleaningTool.Water:
                    changed = meshSurface.CleanAt(hit.textureCoord, 0.065f, 0.075f * Mathf.Lerp(0.75f, 1.25f, i)) > 0f; break;
                case CleaningTool.Foam:
                    foamTouches = Mathf.Min(14, foamTouches + 1); changed = true; break;
                case CleaningTool.Brush:
                    changed = meshSurface.CleanAt(hit.textureCoord, 0.045f, 0.24f * Mathf.Lerp(0.7f, 1.35f, i)) > 0f; break;
                case CleaningTool.Rinse:
                    changed = meshSurface.CleanAt(hit.textureCoord, 0.075f, 0.14f * Mathf.Lerp(0.8f, 1.2f, i)) > 0f; break;
                default:
                    changed = meshSurface.CleanAt(hit.textureCoord, 0.055f, 0.07f * Mathf.Lerp(0.8f, 1.2f, i)) > 0f; break;
            }
        }
        else if (layer != null)
        {
            Vector3 world = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z));
            world.z = 0f;
            Vector2 uv = layer.WorldToUv(world);
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) return;
            feedbackWorld = world;
            switch (CurrentTool)
            {
                case CleaningTool.Water: changed = layer.CleanAt(uv, 28f, 0.085f * Mathf.Lerp(0.75f, 1.25f, i)) > 0f; break;
                case CleaningTool.Foam: foamTouches = Mathf.Min(14, foamTouches + 1); changed = true; break;
                case CleaningTool.Brush: changed = layer.CleanAt(uv, 22f, 0.30f * Mathf.Lerp(0.7f, 1.35f, i)) > 0f; break;
                case CleaningTool.Rinse: changed = layer.CleanAt(uv, 30f, 0.17f * Mathf.Lerp(0.8f, 1.2f, i)) > 0f; break;
                default: changed = layer.CleanAt(uv, 20f, 0.075f * Mathf.Lerp(0.8f, 1.2f, i)) > 0f; break;
            }
        }
        else return;

        UpdateProgressAndStage();
        SpawnFeedback(feedbackWorld, changed, i);
        if (changed && Time.unscaledTime - lastInteraction > 0.11f)
        {
            HapticManager.Light();
            lastInteraction = Time.unscaledTime;
        }
        if (Progress >= 0.999f && stage == 4 && completionRoutine == null)
            completionRoutine = StartCoroutine(CompleteAfterPause());
    }

    public void SetTool(CleaningTool tool)
    {
        if (!IsComplete && (int)tool <= stage) CurrentTool = tool;
    }

    private float SurfaceProgress => meshSurface != null ? meshSurface.Progress : (layer != null ? layer.Progress : 0f);

    private void UpdateProgressAndStage()
    {
        float dirt = SurfaceProgress;
        switch (stage)
        {
            case 0:
                Progress = Mathf.Lerp(0f, 0.20f, Mathf.InverseLerp(0f, 0.10f, dirt));
                if (dirt >= 0.10f) { AdvanceStage(1); return; } break;
            case 1:
                Progress = Mathf.Lerp(0.20f, 0.25f, foamTouches / 14f);
                if (foamTouches >= 14) { AdvanceStage(2); return; } break;
            case 2:
                Progress = Mathf.Lerp(0.25f, 0.80f, Mathf.InverseLerp(0.10f, 0.78f, dirt));
                if (dirt >= 0.78f) { AdvanceStage(3); return; } break;
            case 3:
                Progress = Mathf.Lerp(0.80f, 0.93f, Mathf.InverseLerp(0.78f, 0.93f, dirt));
                if (dirt >= 0.93f) { AdvanceStage(4); return; } break;
            case 4:
                Progress = Mathf.Lerp(0.93f, 1f, Mathf.InverseLerp(0.93f, 0.999f, dirt));
                if (dirt >= 0.999f) Progress = 1f; break;
        }
    }

    private void AdvanceStage(int nextStage)
    {
        stage = Mathf.Clamp(nextStage, 0, 4); CurrentTool = (CleaningTool)stage;
        AudioManager.PlayStageAdvance(CurrentTool); HapticManager.StageAdvance();
    }

    private void SpawnFeedback(Vector3 world, bool changed, float intensity)
    {
        if (!changed || feedbackParticles == null) return;
        int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 4f, intensity)), 1, 4);
        var emit = new ParticleSystem.EmitParams
        {
            position = world,
            startSize = CurrentTool == CleaningTool.Brush ? 0.055f : 0.085f,
            startLifetime = 0.28f,
            velocity = Vector3.up * (0.12f + intensity * 0.25f),
            startColor = CurrentTool == CleaningTool.Foam ? Color.white : new Color(0.78f, 0.92f, 1f, 0.8f)
        };
        feedbackParticles.Emit(emit, count);
    }

    private IEnumerator CompleteAfterPause()
    {
        IsComplete = true; AudioManager.SetInteraction(CurrentTool, false, 0f);
        yield return new WaitForSecondsRealtime(0.8f);
        AudioManager.PlayCompletion(); HapticManager.Completion(); UIManager.ShowCompletion();
    }

    private void CreateFeedbackParticles()
    {
        GameObject go = new GameObject("Tool Feedback Particles");
        feedbackParticles = go.AddComponent<ParticleSystem>();
        var main = feedbackParticles.main; main.playOnAwake = false; main.startLifetime = 0.3f; main.startSize = 0.08f; main.maxParticles = 120;
        var emission = feedbackParticles.emission; emission.rateOverTime = 0f;
    }
}
