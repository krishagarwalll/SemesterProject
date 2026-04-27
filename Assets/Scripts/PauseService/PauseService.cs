using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[Flags]
public enum PauseType
{
    None      = 0,
    Physics   = 1 << 0,
    Input     = 1 << 1,
    Animation = 1 << 2,
    Particles = 1 << 3,
    UI        = 1 << 4,
    Audio     = 1 << 5
}

public static class PauseService
{
    // Gameplay pause keeps UI alive so pause menus and main menus remain clickable.
    private const PauseType FullGamePause = PauseType.Physics | PauseType.Input | PauseType.Animation | PauseType.Particles | PauseType.Audio;

    private static int physicsLocks;
    private static int inputLocks;
    private static int animationLocks;
    private static int particleLocks;
    private static int uiLocks;
    private static int audioLocks;
    private static PauseType activePauseTypes;

    private static SimulationMode2D resumePhysics2DSimulationMode = SimulationMode2D.FixedUpdate;

    private static readonly Dictionary<int, PauseType> pauseBypassById = new();
    private static readonly Dictionary<int, Animator> pausedAnimatorsById = new();
    private static readonly Dictionary<int, float> pausedAnimatorSpeedsById = new();
    private static readonly Dictionary<int, ParticleSystem> pausedParticlesById = new();
    private static readonly Dictionary<int, EventSystem> pausedEventSystemsById = new();

    public static PauseType ActivePauseTypes => activePauseTypes;
    public static event Action<PauseType> PauseChanged;

    // ── Granular API ───────────────────────────────────────────────────────────

    public static void Pause(PauseType pauseTypes)
    {
        if ((pauseTypes & PauseType.Physics) != 0)   physicsLocks++;
        if ((pauseTypes & PauseType.Input) != 0)     inputLocks++;
        if ((pauseTypes & PauseType.Animation) != 0) animationLocks++;
        if ((pauseTypes & PauseType.Particles) != 0) particleLocks++;
        if ((pauseTypes & PauseType.UI) != 0)        uiLocks++;
        if ((pauseTypes & PauseType.Audio) != 0)     audioLocks++;
        RecomputePauseState();
    }

    public static void Resume(PauseType pauseTypes)
    {
        if ((pauseTypes & PauseType.Physics) != 0)   physicsLocks   = Mathf.Max(0, physicsLocks - 1);
        if ((pauseTypes & PauseType.Input) != 0)     inputLocks     = Mathf.Max(0, inputLocks - 1);
        if ((pauseTypes & PauseType.Animation) != 0) animationLocks = Mathf.Max(0, animationLocks - 1);
        if ((pauseTypes & PauseType.Particles) != 0) particleLocks  = Mathf.Max(0, particleLocks - 1);
        if ((pauseTypes & PauseType.UI) != 0)        uiLocks        = Mathf.Max(0, uiLocks - 1);
        if ((pauseTypes & PauseType.Audio) != 0)     audioLocks     = Mathf.Max(0, audioLocks - 1);
        RecomputePauseState();
    }

    public static void SetPaused(PauseType pauseTypes, bool paused)
    {
        if (paused) Pause(pauseTypes);
        else Resume(pauseTypes);
    }

    public static bool IsPaused(PauseType pauseType) => (activePauseTypes & pauseType) != 0;

    public static bool IsGameplayInputPaused(UnityEngine.Object context = null) => IsPaused(PauseType.Input, context);

    public static bool IsPaused(PauseType pauseType, UnityEngine.Object context)
    {
        if (!context) return IsPaused(pauseType);
        var bypassMask = GetBypassMask(context);
        pauseType &= ~bypassMask;
        return IsPaused(pauseType);
    }

    public static void SetPauseBypass(UnityEngine.Object context, PauseType pauseTypes, bool enabled)
    {
        if (!context) return;
        var contextId = context.GetInstanceID();
        if (!pauseBypassById.TryGetValue(contextId, out var existing)) existing = PauseType.None;
        var updated = enabled ? existing | pauseTypes : existing & ~pauseTypes;
        if (updated == PauseType.None) pauseBypassById.Remove(contextId);
        else pauseBypassById[contextId] = updated;
    }

    public static void SetAnimationPauseBypass(UnityEngine.Object context, bool enabled)
        => SetPauseBypass(context, PauseType.Animation, enabled);

    public static void ClearAll()
    {
        physicsLocks = inputLocks = animationLocks = particleLocks = uiLocks = audioLocks = 0;
        pauseBypassById.Clear();
        RecomputePauseState();
    }

    // ── Backwards-compatible no-arg overloads ─────────────────────────────────

    public static void Pause()   => Pause(FullGamePause);
    public static void Resume()  => Resume(FullGamePause);
    public static void Toggle()
    {
        if (IsPaused(PauseType.Physics)) Resume(FullGamePause);
        else Pause(FullGamePause);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        physicsLocks = inputLocks = animationLocks = particleLocks = uiLocks = audioLocks = 0;
        activePauseTypes = PauseType.None;
        pauseBypassById.Clear();
        pausedAnimatorsById.Clear();
        pausedAnimatorSpeedsById.Clear();
        pausedParticlesById.Clear();
        pausedEventSystemsById.Clear();
        resumePhysics2DSimulationMode = Physics2D.simulationMode;
        AudioListener.pause = false;
    }

    private static void RecomputePauseState()
    {
        var nextPauseTypes = PauseType.None;
        if (physicsLocks > 0)   nextPauseTypes |= PauseType.Physics;
        if (inputLocks > 0)     nextPauseTypes |= PauseType.Input;
        if (animationLocks > 0) nextPauseTypes |= PauseType.Animation;
        if (particleLocks > 0)  nextPauseTypes |= PauseType.Particles;
        if (uiLocks > 0)        nextPauseTypes |= PauseType.UI;
        if (audioLocks > 0)     nextPauseTypes |= PauseType.Audio;

        if (nextPauseTypes == activePauseTypes) return;

        var previousPauseTypes = activePauseTypes;
        activePauseTypes = nextPauseTypes;
        ApplyPauseState(previousPauseTypes, activePauseTypes);
        PauseChanged?.Invoke(activePauseTypes);
    }

    private static void ApplyPauseState(PauseType previousPauseTypes, PauseType nextPauseTypes)
    {
        ApplyPhysicsPause(
            (previousPauseTypes & PauseType.Physics) != 0,
            (nextPauseTypes & PauseType.Physics) != 0);

        ApplyAnimationPause(
            (previousPauseTypes & PauseType.Animation) != 0,
            (nextPauseTypes & PauseType.Animation) != 0);

        ApplyParticlePause(
            (previousPauseTypes & PauseType.Particles) != 0,
            (nextPauseTypes & PauseType.Particles) != 0);

        ApplyUiPause(
            (previousPauseTypes & PauseType.UI) != 0,
            (nextPauseTypes & PauseType.UI) != 0);

        AudioListener.pause = (nextPauseTypes & PauseType.Audio) != 0;
    }

    private static void ApplyPhysicsPause(bool wasPaused, bool isPaused)
    {
        if (wasPaused == isPaused) return;
        if (isPaused)
        {
            resumePhysics2DSimulationMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }
        else
        {
            Physics2D.simulationMode = resumePhysics2DSimulationMode;
        }
    }

    private static void ApplyAnimationPause(bool wasPaused, bool isPaused)
    {
        if (wasPaused == isPaused) return;

        if (isPaused)
        {
            var animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var anim in animators)
            {
                if (!anim || IsBypassed(anim, PauseType.Animation)) continue;
                var id = anim.GetInstanceID();
                if (pausedAnimatorsById.ContainsKey(id)) continue;
                pausedAnimatorsById[id] = anim;
                pausedAnimatorSpeedsById[id] = anim.speed;
                anim.speed = 0f;
            }
            return;
        }

        foreach (var pair in pausedAnimatorsById)
        {
            if (!pair.Value) continue;
            pair.Value.speed = pausedAnimatorSpeedsById.TryGetValue(pair.Key, out var speed) ? speed : 1f;
        }
        pausedAnimatorsById.Clear();
        pausedAnimatorSpeedsById.Clear();
    }

    private static void ApplyParticlePause(bool wasPaused, bool isPaused)
    {
        if (wasPaused == isPaused) return;

        if (isPaused)
        {
            var particles = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ps in particles)
            {
                if (!ps || !ps.isPlaying || IsBypassed(ps, PauseType.Particles)) continue;
                var id = ps.GetInstanceID();
                if (pausedParticlesById.ContainsKey(id)) continue;
                pausedParticlesById[id] = ps;
                ps.Pause(true);
            }
            return;
        }

        foreach (var pair in pausedParticlesById)
        {
            if (pair.Value) pair.Value.Play(true);
        }
        pausedParticlesById.Clear();
    }

    private static void ApplyUiPause(bool wasPaused, bool isPaused)
    {
        if (wasPaused == isPaused) return;

        if (isPaused)
        {
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var es in eventSystems)
            {
                if (!es || !es.enabled || IsBypassed(es, PauseType.UI)) continue;
                var id = es.GetInstanceID();
                if (pausedEventSystemsById.ContainsKey(id)) continue;
                pausedEventSystemsById[id] = es;
                es.enabled = false;
            }
            return;
        }

        foreach (var pair in pausedEventSystemsById)
        {
            if (pair.Value) pair.Value.enabled = true;
        }
        pausedEventSystemsById.Clear();
    }

    private static bool IsBypassed(UnityEngine.Object context, PauseType pauseType)
        => (GetBypassMask(context) & pauseType) != 0;

    private static PauseType GetBypassMask(UnityEngine.Object context)
    {
        if (!context) return PauseType.None;
        var mask = PauseType.None;
        if (pauseBypassById.TryGetValue(context.GetInstanceID(), out var directMask))
            mask |= directMask;
        if (context is Component component && component.gameObject &&
            pauseBypassById.TryGetValue(component.gameObject.GetInstanceID(), out var gameObjectMask))
            mask |= gameObjectMask;
        return mask;
    }
}
