/*
PauseManager.cs

Purpose: Centralized, multi-reason pause state manager for the prototype.

Design decisions:
- Multi-reason system: Maintains a HashSet of active PauseReasons (LevelUp, Death, Victory, Debug) so that pausing for one reason does not unpause when another remains active.
  Example: If paused for LevelUp and then Death occurs, the game stays paused until both reasons are cleared.
- Time.timeScale control: Automatically applies timeScale 0 when any reason is active, 1 when all reasons are cleared. This is the single authoritative source of pause state.
- Singleton pattern: Provides a static Instance property that auto-creates a PauseManager GameObject if none exists in the scene. This avoids requiring manual scene setup while keeping the API convenient.
- Event system: OnPauseStateChanged(HashSet<PauseReason>) notifies listeners when pause state changes, providing the current active reasons.
- Queryable API:
  - Pause(reason): Add a reason; applies timeScale 0.
  - Unpause(reason): Remove a reason; clears timeScale 1 only if no reasons remain.
  - IsPaused: True if any reason is active.
  - IsPausedFor(reason): True if a specific reason is active.
  - GetActiveReasons(): Returns a copy of active reasons for inspection.

Configuration:
- No manual configuration required if auto-created.
- If placed in scene, ensure only one instance exists (DisallowMultipleComponent enforces this).

Usage:
- LevelUpController pauses for LevelUp when level-up pending.
- PlayerDeath pauses for Death.
- Victory conditions pause for Victory.
- UI can subscribe to OnPauseStateChanged to show/hide pause screens accordingly.

Notes:
- PauseReason.Debug can be used for manual testing.
- Time.timeScale is the only pause mechanism; no input blocking or UI handling here.
- Game should check Time.deltaTime > 0 in physics or animation systems to respect pause.
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
    PlatformerMenu
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
