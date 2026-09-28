/*
DialogueGate.cs

Purpose:
- Controls the dialogue gate flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

// Lightweight ref-counted flag so gameplay systems (e.g. EnemySpawner) can check
// whether any EasyDS dialogue is currently active, without touching Time.timeScale
// (which would also freeze the dialogue's own typewriter coroutine).
public static class DialogueGate
{
    private static int activeCount = 0;

    public static bool IsDialogueActive => activeCount > 0;

    public static void Begin() => activeCount++;

    public static void End()
    {
        if (activeCount > 0) activeCount--;
    }
}
