using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CoreLogicTests
{
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private float originalTimeScale;

    [SetUp]
    public void SetUp()
    {
        originalTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        ClearDialogueGate();
        ClearPauseManager();
    }

    [TearDown]
    public void TearDown()
    {
        ClearDialogueGate();
        ClearPauseManager();

        foreach (GameObject spawned in spawnedObjects)
        {
            if (spawned != null)
            {
                Object.DestroyImmediate(spawned);
            }
        }

        spawnedObjects.Clear();
        Time.timeScale = originalTimeScale;
    }

    [Test]
    public void Health_TakeDamage_ReducesHealthAndClampsAtZero()
    {
        Health health = CreateComponent<Health>("HealthDamageTest");

        Assert.That(health.CurrentHealth, Is.EqualTo(10));
        Assert.That(health.TakeDamage(3), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(7));

        Assert.That(health.TakeDamage(20), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(0));
        Assert.That(health.IsDead, Is.True);
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void Health_TakeDamage_WithNonPositiveAmount_IsRejected(int amount)
    {
        Health health = CreateComponent<Health>("HealthInvalidDamageTest");

        Assert.That(health.TakeDamage(amount), Is.False);
        Assert.That(health.CurrentHealth, Is.EqualTo(10));
        Assert.That(health.IsDead, Is.False);
    }

    [Test]
    public void Health_TakeDamage_ExactZero_FiresZeroHealthOnce()
    {
        Health health = CreateComponent<Health>("HealthZeroTest");
        int zeroHealthCount = 0;
        int healthChangedCount = 0;

        health.OnZeroHealth += () => zeroHealthCount++;
        health.OnHealthChanged += (_, _) => healthChangedCount++;

        Assert.That(health.TakeDamage(10), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(0));
        Assert.That(zeroHealthCount, Is.EqualTo(1));
        Assert.That(healthChangedCount, Is.EqualTo(1));

        Assert.That(health.TakeDamage(1), Is.False);
        Assert.That(zeroHealthCount, Is.EqualTo(1));
        Assert.That(healthChangedCount, Is.EqualTo(1));
    }

    [Test]
    public void Health_Heal_RestoresHealthAndClampsToMax()
    {
        Health health = CreateComponent<Health>("HealthHealTest");

        Assert.That(health.TakeDamage(5), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(5));

        Assert.That(health.Heal(3), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(8));

        Assert.That(health.Heal(100), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(10));
    }

    [TestCase(0)]
    [TestCase(-2)]
    public void Health_Heal_WithNonPositiveAmount_IsRejected(int amount)
    {
        Health health = CreateComponent<Health>("HealthInvalidHealTest");

        Assert.That(health.Heal(amount), Is.False);
        Assert.That(health.CurrentHealth, Is.EqualTo(10));
    }

    [Test]
    public void PlayerExperience_AddExperience_WithoutLevelUp_UpdatesCurrentExperience()
    {
        PlayerExperience experience = CreateComponent<PlayerExperience>("ExperienceNoLevelUpTest");

        int levelUpCount = 0;
        experience.OnLevelUp += _ => levelUpCount++;

        Assert.That(experience.AddExperience(5), Is.True);
        Assert.That(experience.Level, Is.EqualTo(1));
        Assert.That(experience.CurrentExperience, Is.EqualTo(5));
        Assert.That(experience.ExperienceToNext, Is.EqualTo(10));
        Assert.That(levelUpCount, Is.EqualTo(0));
    }

    [Test]
    public void PlayerExperience_AddExperience_ExactThreshold_LevelsUpOnce()
    {
        PlayerExperience experience = CreateComponent<PlayerExperience>("ExperienceExactThresholdTest");

        int newLevel = 0;
        experience.OnLevelUp += level => newLevel = level;

        Assert.That(experience.AddExperience(10), Is.True);
        Assert.That(experience.Level, Is.EqualTo(2));
        Assert.That(experience.CurrentExperience, Is.EqualTo(0));
        Assert.That(experience.ExperienceToNext, Is.EqualTo(20));
        Assert.That(newLevel, Is.EqualTo(2));
    }

    [Test]
    public void PlayerExperience_AddExperience_CarriesExcessAndCanLevelUpMultipleTimes()
    {
        PlayerExperience experience = CreateComponent<PlayerExperience>("ExperienceMultiLevelTest");

        List<int> levels = new List<int>();
        experience.OnLevelUp += level => levels.Add(level);

        Assert.That(experience.AddExperience(60), Is.True);
        Assert.That(experience.Level, Is.EqualTo(4));
        Assert.That(experience.CurrentExperience, Is.EqualTo(0));
        Assert.That(experience.ExperienceToNext, Is.EqualTo(40));
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, levels);
    }

    [Test]
    public void PlayerExperience_AddExperience_WithZeroOrNegative_IsRejected()
    {
        PlayerExperience experience = CreateComponent<PlayerExperience>("ExperienceInvalidTest");

        Assert.That(experience.AddExperience(0), Is.False);
        Assert.That(experience.AddExperience(-1), Is.False);
        Assert.That(experience.Level, Is.EqualTo(1));
        Assert.That(experience.CurrentExperience, Is.EqualTo(0));
    }

    [Test]
    public void PlayerBossAttackPieces_AddAndConsumePieces_TracksAvailability()
    {
        PlayerBossAttackPieces pieces = CreateComponent<PlayerBossAttackPieces>("BossPiecesTest");

        int lastCount = -1;
        pieces.OnPiecesChanged += count => lastCount = count;

        pieces.AddPiece();
        Assert.That(pieces.PiecesCollected, Is.EqualTo(1));
        Assert.That(pieces.PiecesStillNeeded, Is.EqualTo(2));
        Assert.That(pieces.HasMegaAttackAvailable, Is.False);
        Assert.That(lastCount, Is.EqualTo(1));

        pieces.AddPiece();
        Assert.That(pieces.PiecesCollected, Is.EqualTo(2));
        Assert.That(pieces.HasMegaAttackAvailable, Is.False);

        pieces.AddPiece();
        Assert.That(pieces.PiecesCollected, Is.EqualTo(3));
        Assert.That(pieces.HasMegaAttackAvailable, Is.True);
        Assert.That(pieces.PiecesStillNeeded, Is.EqualTo(0));

        Assert.That(pieces.TryConsumePieces(3), Is.True);
        Assert.That(pieces.PiecesCollected, Is.EqualTo(0));
        Assert.That(pieces.HasMegaAttackAvailable, Is.False);
        Assert.That(lastCount, Is.EqualTo(0));

        Assert.That(pieces.TryConsumePieces(1), Is.False);
        Assert.That(pieces.PiecesCollected, Is.EqualTo(0));
    }

    [Test]
    public void PlayerBossAttackPieces_CannotConsumeMoreThanAvailable()
    {
        PlayerBossAttackPieces pieces = CreateComponent<PlayerBossAttackPieces>("BossPiecesOverConsumeTest");

        pieces.AddPiece();
        pieces.AddPiece();

        Assert.That(pieces.TryConsumePieces(3), Is.False);
        Assert.That(pieces.PiecesCollected, Is.EqualTo(2));
        Assert.That(pieces.HasMegaAttackAvailable, Is.False);
    }

    [Test]
    public void PauseManager_OneReason_PausesAndUnpausesCorrectly()
    {
        PauseManager pauseManager = PauseManager.Instance;

        pauseManager.Pause(PauseManager.PauseReason.Death);
        Assert.That(pauseManager.IsPaused, Is.True);
        Assert.That(pauseManager.IsPausedFor(PauseManager.PauseReason.Death), Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(0f));

        pauseManager.Unpause(PauseManager.PauseReason.Death);
        Assert.That(pauseManager.IsPaused, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void PauseManager_TwoReasons_RemainsPausedUntilBothRemoved()
    {
        PauseManager pauseManager = PauseManager.Instance;

        pauseManager.Pause(PauseManager.PauseReason.LevelUp);
        pauseManager.Pause(PauseManager.PauseReason.Death);
        Assert.That(pauseManager.IsPaused, Is.True);
        Assert.That(pauseManager.IsPausedFor(PauseManager.PauseReason.LevelUp), Is.True);
        Assert.That(pauseManager.IsPausedFor(PauseManager.PauseReason.Death), Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(0f));

        pauseManager.Unpause(PauseManager.PauseReason.LevelUp);
        Assert.That(pauseManager.IsPaused, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(0f));

        pauseManager.Unpause(PauseManager.PauseReason.Death);
        Assert.That(pauseManager.IsPaused, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void PauseManager_ClearAll_ResetsFinalState()
    {
        PauseManager pauseManager = PauseManager.Instance;

        pauseManager.Pause(PauseManager.PauseReason.Victory);
        pauseManager.Pause(PauseManager.PauseReason.Debug);
        Assert.That(pauseManager.IsPaused, Is.True);

        pauseManager.ClearAll();
        Assert.That(pauseManager.IsPaused, Is.False);
        Assert.That(pauseManager.IsPausedFor(PauseManager.PauseReason.Victory), Is.False);
        Assert.That(pauseManager.IsPausedFor(PauseManager.PauseReason.Debug), Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void DialogueGate_BeginAndEnd_BalancesActiveState()
    {
        Assert.That(DialogueGate.IsDialogueActive, Is.False);

        DialogueGate.Begin();
        Assert.That(DialogueGate.IsDialogueActive, Is.True);

        DialogueGate.End();
        Assert.That(DialogueGate.IsDialogueActive, Is.False);
    }

    [Test]
    public void DialogueGate_MultipleBegins_RequireMatchingEnds()
    {
        DialogueGate.Begin();
        DialogueGate.Begin();
        DialogueGate.Begin();

        Assert.That(DialogueGate.IsDialogueActive, Is.True);

        DialogueGate.End();
        Assert.That(DialogueGate.IsDialogueActive, Is.True);

        DialogueGate.End();
        DialogueGate.End();
        Assert.That(DialogueGate.IsDialogueActive, Is.False);
    }

    [Test]
    public void DialogueGate_End_WhenAlreadyInactive_KeepsItInactive()
    {
        Assert.That(DialogueGate.IsDialogueActive, Is.False);

        DialogueGate.End();

        Assert.That(DialogueGate.IsDialogueActive, Is.False);
    }

    private static T CreateComponent<T>(string objectName) where T : Component
    {
        GameObject go = new GameObject(objectName);
        return go.AddComponent<T>();
    }

    private void ClearPauseManager()
    {
        PauseManager pauseManager = Object.FindAnyObjectByType<PauseManager>();
        if (pauseManager != null)
        {
            Object.DestroyImmediate(pauseManager.gameObject);
        }

        Time.timeScale = 1f;
    }

    private static void ClearDialogueGate()
    {
        int safety = 0;
        while (DialogueGate.IsDialogueActive && safety < 32)
        {
            DialogueGate.End();
            safety++;
        }
    }
}