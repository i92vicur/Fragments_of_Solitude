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
