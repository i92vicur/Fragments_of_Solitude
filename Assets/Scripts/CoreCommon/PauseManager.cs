/*
PauseManager.cs

Purpose:
- Controls the pause manager flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PauseManager : MonoBehaviour
{
public enum PauseReason
{
    LevelUp,
    Death,
    Debug,
    Victory,
    PlatformerMenu,
    Level5Menu
}
private static PauseManager instance;
public static PauseManager Instance
{
    get
    {
        if (instance == null)
        {
            instance = FindAnyObjectByType<PauseManager>();
            if (instance == null)
            {
                // Create a temporary GameObject to host the manager so callers can use PauseManager without scene setup.
                var go = new GameObject("PauseManager");
                instance = go.AddComponent<PauseManager>();
            }
        }
        return instance;
    }
}
// Event invoked when pause state changes; provides a snapshot copy of active reasons;
public event Action<HashSet<PauseReason>> OnPauseStateChanged;
private HashSet<PauseReason> activeReasons = new HashSet<PauseReason>();
public bool IsPaused => activeReasons.Count > 0;
public IReadOnlyCollection<PauseReason> ActiveReasons => activeReasons;
void Awake()
{
    // Ensure singleton uniqueness if created in scene
    if (instance == null)
    {
        instance = this;
    }
    else if (instance != this)
    {
            Destroy(this.gameObject);
            return;
        }
    }

    /// <summary>
    /// Request pause for the given reason. Multiple reasons can be active simultaneously.
    /// </summary>
    public void Pause(PauseReason reason)
    {
        if (!activeReasons.Add(reason)) return; // already paused for this reason
        ApplyPauseState();
    }

    /// <summary>
    /// Remove the pause reason. Time will only resume if no other pause reasons remain active.
    /// </summary>
    public void Unpause(PauseReason reason)
    {
        if (!activeReasons.Remove(reason)) return; // wasn't present
        ApplyPauseState();
    }

    /// <summary>
    /// Clear all pause reasons and resume time.
    /// </summary>
    public void ClearAll()
    {
        if (activeReasons.Count == 0) return;
        activeReasons.Clear();
        ApplyPauseState();
    }

    public bool IsPausedFor(PauseReason reason)
    {
        return activeReasons.Contains(reason);
    }
private void OnDestroy()
{
    // Clear the static reference when this instance is destroyed (e.g. scene unload).
    // Prevents the getter from returning a destroyed object during the next scene's startup.
    if (instance == this)
        instance = null;
}
private void ApplyPauseState()
    {
        // Apply authoritative Time.timeScale: 0 if any reason active, otherwise 1.
        Time.timeScale = activeReasons.Count > 0 ? 0f : 1f;

        // Fire event with a defensive copy to avoid external mutation
        OnPauseStateChanged?.Invoke(new HashSet<PauseReason>(activeReasons));
    }
}
