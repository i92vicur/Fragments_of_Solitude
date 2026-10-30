using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreLogicTests
{
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private float originalTimeScale;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();
        originalTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        ClearDialogueGate();
        ClearPauseManager();
        ResetSettingsManager();
        ResetGameProgressManager();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        ClearDialogueGate();
        ClearPauseManager();
        ResetSettingsManager();
        ResetGameProgressManager();

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
        Health health = CreateFreshHealth("HealthDamageTest");

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
        Health health = CreateFreshHealth("HealthInvalidDamageTest");

        Assert.That(health.TakeDamage(amount), Is.False);
        Assert.That(health.CurrentHealth, Is.EqualTo(10));
        Assert.That(health.IsDead, Is.False);
    }

    [Test]
    public void Health_TakeDamage_ExactZero_FiresZeroHealthOnce()
    {
        Health health = CreateFreshHealth("HealthZeroTest");
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
        Health health = CreateFreshHealth("HealthHealTest");

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
        Health health = CreateFreshHealth("HealthInvalidHealTest");

        Assert.That(health.Heal(amount), Is.False);
        Assert.That(health.CurrentHealth, Is.EqualTo(10));
    }

    [Test]
    public void PlayerExperience_AddExperience_WithoutLevelUp_UpdatesCurrentExperience()
    {
        PlayerExperience experience = CreateFreshPlayerExperience("ExperienceNoLevelUpTest");

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
        PlayerExperience experience = CreateFreshPlayerExperience("ExperienceExactThresholdTest");

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
        PlayerExperience experience = CreateFreshPlayerExperience("ExperienceMultiLevelTest");

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

    [Test]
    public void SettingsManager_SetWindowSize_AcceptsAllowedSizesAndPersists()
    {
        ResetSettingsManager();
        SettingsManager settings = SettingsManager.Instance;

        foreach (Vector2Int size in SettingsManager.AllowedWindowSizes)
        {
            Assert.That(settings.SetWindowSize(size.x, size.y), Is.True);
            Assert.That(settings.WindowSize, Is.EqualTo(size));
            Assert.That(PlayerPrefs.GetInt(SettingsManager.WindowWidthKey), Is.EqualTo(size.x));
            Assert.That(PlayerPrefs.GetInt(SettingsManager.WindowHeightKey), Is.EqualTo(size.y));
        }
    }

    [Test]
    public void SettingsManager_SetWindowSize_RejectsUnknownNegativeAndZeroSizes()
    {
        ResetSettingsManager();
        SettingsManager settings = SettingsManager.Instance;
        Vector2Int before = settings.WindowSize;

        Assert.That(settings.SetWindowSize(800, 600), Is.False);
        Assert.That(settings.SetWindowSize(-960, 540), Is.False);
        Assert.That(settings.SetWindowSize(0, 0), Is.False);
        Assert.That(settings.SetWindowSize(1920, 0), Is.False);
        Assert.That(settings.WindowSize, Is.EqualTo(before));
        Assert.That(PlayerPrefs.HasKey(SettingsManager.WindowWidthKey), Is.False);
    }

    [Test]
    public void SettingsManager_WindowSize_SurvivesReload()
    {
        ResetSettingsManager();
        SettingsManager.Instance.SetWindowSize(1280, 720);

        RestartSettingsManager();

        Assert.That(SettingsManager.Instance.WindowSize, Is.EqualTo(new Vector2Int(1280, 720)));
    }

    [Test]
    public void SettingsManager_Fullscreen_DoesNotAlterPreferredWindowSize()
    {
        ResetSettingsManager();
        SettingsManager settings = SettingsManager.Instance;
        settings.SetWindowSize(960, 540);

        settings.SetFullscreen(true);
        Assert.That(settings.WindowSize, Is.EqualTo(new Vector2Int(960, 540)));
        Assert.That(PlayerPrefs.GetInt(SettingsManager.WindowWidthKey), Is.EqualTo(960));

        RestartSettingsManager();
        Assert.That(SettingsManager.Instance.Fullscreen, Is.True);
        Assert.That(SettingsManager.Instance.WindowSize, Is.EqualTo(new Vector2Int(960, 540)));
    }

    [Test]
    public void SettingsManager_MissingOrInvalidWindowSize_FallsBackToDefault()
    {
        ResetSettingsManager();
        Assert.That(SettingsManager.Instance.WindowSize, Is.EqualTo(SettingsManager.DefaultWindowSize));

        PlayerPrefs.SetInt(SettingsManager.WindowWidthKey, 999);
        PlayerPrefs.SetInt(SettingsManager.WindowHeightKey, 777);
        RestartSettingsManager();
        Assert.That(SettingsManager.Instance.WindowSize, Is.EqualTo(SettingsManager.DefaultWindowSize));

        PlayerPrefs.SetInt(SettingsManager.WindowWidthKey, 1280);
        PlayerPrefs.DeleteKey(SettingsManager.WindowHeightKey);
        RestartSettingsManager();
        Assert.That(SettingsManager.Instance.WindowSize, Is.EqualTo(SettingsManager.DefaultWindowSize));
    }

    [Test]
    public void SettingsManager_WindowSizeChanges_DoNotAlterAudioSettings()
    {
        ResetSettingsManager();
        SettingsManager settings = SettingsManager.Instance;
        settings.SetMusicVolume(0.3f);
        settings.SetSfxVolume(0.6f);

        settings.SetWindowSize(1366, 768);

        Assert.That(settings.MusicVolume, Is.EqualTo(0.3f));
        Assert.That(settings.SfxVolume, Is.EqualTo(0.6f));
        Assert.That(PlayerPrefs.GetFloat(SettingsManager.MusicVolumeKey), Is.EqualTo(0.3f));
        Assert.That(PlayerPrefs.GetFloat(SettingsManager.SfxVolumeKey), Is.EqualTo(0.6f));
    }

    [Test]
    public void SettingsManager_SetMusicVolume_ClampsAndPersists()
    {
        ResetSettingsManager();
        SettingsManager settings = SettingsManager.Instance;

        settings.SetMusicVolume(0.2f);
        Assert.That(settings.MusicVolume, Is.EqualTo(0.2f));
        Assert.That(PlayerPrefs.GetFloat(SettingsManager.MusicVolumeKey), Is.EqualTo(0.2f));

        settings.SetMusicVolume(3f);
        Assert.That(settings.MusicVolume, Is.EqualTo(1f));
        Assert.That(PlayerPrefs.GetFloat(SettingsManager.MusicVolumeKey), Is.EqualTo(1f));
    }

    [Test]
    public void SettingsManager_SetSfxVolume_ClampsAndPersists()
    {
        ResetSettingsManager();
        SettingsManager settings = SettingsManager.Instance;

        settings.SetSfxVolume(-1f);
        Assert.That(settings.SfxVolume, Is.EqualTo(0f));
        Assert.That(PlayerPrefs.GetFloat(SettingsManager.SfxVolumeKey), Is.EqualTo(0f));

        settings.SetSfxVolume(0.75f);
        Assert.That(settings.SfxVolume, Is.EqualTo(0.75f));
        Assert.That(PlayerPrefs.GetFloat(SettingsManager.SfxVolumeKey), Is.EqualTo(0.75f));
    }

    [Test]
    public void SettingsManager_SetFullscreen_TogglesAndPersists()
    {
        ResetSettingsManager();
        SettingsManager settings = SettingsManager.Instance;

        settings.SetFullscreen(false);
        Assert.That(settings.Fullscreen, Is.False);
        Assert.That(PlayerPrefs.GetInt(SettingsManager.FullscreenKey), Is.EqualTo(0));

        settings.SetFullscreen(true);
        Assert.That(settings.Fullscreen, Is.True);
        Assert.That(PlayerPrefs.GetInt(SettingsManager.FullscreenKey), Is.EqualTo(1));
    }

    [Test]
    public void SettingsManager_LinearToDecibels_UsesExpectedScaling()
    {
        Assert.That(SettingsManager.LinearToDecibels(0f), Is.EqualTo(-80f));
        Assert.That(SettingsManager.LinearToDecibels(1f), Is.EqualTo(0f));
        Assert.That(SettingsManager.LinearToDecibels(0.5f), Is.LessThan(0f));
    }

    [Test]
    public void GameProgressManager_MarkProgress_TracksCompletionFlags()
    {
        ResetGameProgressManager();
        GameProgressManager manager = GameProgressManager.Instance;

        manager.MarkLevel3Completed();
        manager.MarkLongestPathCompleted();
        manager.MarkConstraintPuzzleCompleted();

        Assert.That(manager.Level3Completed, Is.True);
        Assert.That(manager.LongestPathCompleted, Is.True);
        Assert.That(manager.ConstraintPuzzleCompleted, Is.True);
        Assert.That(manager.SlidingPuzzleCompleted, Is.False);
    }

    [Test]
    public void GameProgressManager_StartNewGame_ResetsAllProgressFlags()
    {
        ResetGameProgressManager();
        GameProgressManager manager = GameProgressManager.Instance;

        manager.MarkLevel4Completed();
        manager.MarkLevel5Completed();
        manager.MarkSlidingPuzzleCompleted();
        manager.StartNewGame();

        Assert.That(manager.Level4Completed, Is.False);
        Assert.That(manager.Level5Completed, Is.False);
        Assert.That(manager.SlidingPuzzleCompleted, Is.False);
        Assert.That(PlayerPrefs.GetInt(GameProgressManager.Level4CompletedKey), Is.EqualTo(0));
    }

    [Test]
    public void GameProgressManager_Load_UsesPersistedValues()
    {
        ResetGameProgressManager();
        PlayerPrefs.SetInt(GameProgressManager.Level3CompletedKey, 1);
        PlayerPrefs.SetInt(GameProgressManager.LongestPathCompletedKey, 1);
        PlayerPrefs.Save();

        GameProgressManager manager = new GameObject("GameProgressManager_Load").AddComponent<GameProgressManager>();
        typeof(GameProgressManager)
            .GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(manager, null);

        Assert.That(manager.Level3Completed, Is.True);
        Assert.That(manager.LongestPathCompleted, Is.True);
    }

    [Test]
    public void PauseManager_DuplicatePauseReason_IsTrackedOnce()
    {
        PauseManager pauseManager = PauseManager.Instance;

        pauseManager.Pause(PauseManager.PauseReason.Debug);
        pauseManager.Pause(PauseManager.PauseReason.Debug);

        Assert.That(pauseManager.IsPaused, Is.True);
        Assert.That(pauseManager.ActiveReasons.Count, Is.EqualTo(1));
        Assert.That(pauseManager.IsPausedFor(PauseManager.PauseReason.Debug), Is.True);

        pauseManager.Unpause(PauseManager.PauseReason.Debug);
        Assert.That(pauseManager.IsPaused, Is.False);
        Assert.That(pauseManager.ActiveReasons.Count, Is.EqualTo(0));
    }

    [Test]
    public void SettingsManager_ResetToDefaults_RestoresDefaultValuesAndPersistsThem()
    {
        ResetSettingsManager();
        SettingsManager settings = SettingsManager.Instance;

        settings.SetMusicVolume(0.2f);
        settings.SetSfxVolume(0.6f);
        settings.SetFullscreen(false);

        settings.ResetToDefaults();

        Assert.That(settings.MusicVolume, Is.EqualTo(SettingsManager.DefaultMusicVolume));
        Assert.That(settings.SfxVolume, Is.EqualTo(SettingsManager.DefaultSfxVolume));
        Assert.That(settings.Fullscreen, Is.EqualTo(SettingsManager.DefaultFullscreen));
        Assert.That(PlayerPrefs.GetFloat(SettingsManager.MusicVolumeKey), Is.EqualTo(SettingsManager.DefaultMusicVolume));
        Assert.That(PlayerPrefs.GetFloat(SettingsManager.SfxVolumeKey), Is.EqualTo(SettingsManager.DefaultSfxVolume));
        Assert.That(PlayerPrefs.GetInt(SettingsManager.FullscreenKey), Is.EqualTo(SettingsManager.DefaultFullscreen ? 1 : 0));
    }

    [Test]
    public void GameProgressManager_RepeatedMarks_AreIdempotent()
    {
        ResetGameProgressManager();
        GameProgressManager manager = GameProgressManager.Instance;

        manager.MarkLevel3Completed();
        manager.MarkLevel3Completed();
        manager.MarkLongestPathCompleted();
        manager.MarkLongestPathCompleted();

        Assert.That(manager.Level3Completed, Is.True);
        Assert.That(manager.LongestPathCompleted, Is.True);
        Assert.That(PlayerPrefs.GetInt(GameProgressManager.Level3CompletedKey), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(GameProgressManager.LongestPathCompletedKey), Is.EqualTo(1));
    }

    [Test]
    public void Combatant_TakeDamage_OnlyAppliesLivingDamage()
    {
        Combatant combatant = CreateCombatant("TestCombatant", false, 20, 8, 2);

        Assert.That(combatant.TakeDamage(5), Is.EqualTo(5));
        Assert.That(combatant.CurrentHP, Is.EqualTo(15));

        Assert.That(combatant.TakeDamage(100), Is.EqualTo(15));
        Assert.That(combatant.IsAlive, Is.False);
        Assert.That(combatant.TakeDamage(1), Is.EqualTo(0));
    }

    [Test]
    public void Combatant_Heal_StopsAtMaxHP()
    {
        Combatant combatant = CreateCombatant("Healer", false, 10, 5, 1);
        combatant.TakeDamage(6);

        Assert.That(combatant.Heal(5), Is.EqualTo(5));
        Assert.That(combatant.CurrentHP, Is.EqualTo(9));

        Assert.That(combatant.Heal(1), Is.EqualTo(1));
        Assert.That(combatant.CurrentHP, Is.EqualTo(10));

        Assert.That(combatant.Heal(1), Is.EqualTo(0));
        Assert.That(combatant.CurrentHP, Is.EqualTo(10));
    }

    [Test]
    public void Combatant_DefendBonus_ReplacesPreviousValue()
    {
        Combatant combatant = CreateCombatant("Defender", false, 20, 5, 3);

        combatant.ApplyDefendBonus(5);
        Assert.That(combatant.EffectiveDefense, Is.EqualTo(8));

        combatant.ApplyDefendBonus(12);
        Assert.That(combatant.EffectiveDefense, Is.EqualTo(15));

        combatant.ClearDefendBonus();
        Assert.That(combatant.EffectiveDefense, Is.EqualTo(3));
    }

    [Test]
    public void Level5BattleController_PerformAttack_UsesDefenseAndCanKillTarget()
    {
        Combatant warrior = CreateCombatant("Warrior", false, 50, 25, 2);
        Combatant enemy = CreateCombatant("Enemy", true, 20, 8, 5);
        Level5BattleController controller = CreateBattleController(warrior, enemy);

        bool missed;
        bool critical;
        int damage = controller.PerformAttack(warrior, enemy, out missed, out critical, canMiss: false);

        Assert.That(missed, Is.False);
        Assert.That(critical, Is.False);
        Assert.That(damage, Is.EqualTo(20));
        Assert.That(enemy.CurrentHP, Is.EqualTo(0));
        Assert.That(controller.State, Is.EqualTo(Level5BattleController.BattleState.Victory));
    }

    [Test]
    public void Level5BattleController_EndCurrentTurn_AdvancesToNextAliveCombatant()
    {
        Combatant warrior = CreateCombatant("Warrior", false, 50, 10, 0);
        Combatant enemy = CreateCombatant("Enemy", true, 20, 0, 0);
        Combatant supporter = CreateCombatant("Support", false, 50, 10, 0);
        Level5BattleController controller = CreateBattleController(warrior, enemy, supporter);

        var turnSequence = new List<Combatant>();
        controller.OnTurnChanged += turnSequence.Add;

        SetPrivateField(controller, "currentIndex", 0);

        // The Final Enemy's turn runs automatically, so it ends straight into the Support turn.
        controller.EndCurrentTurn();
        Assert.That(turnSequence, Is.EqualTo(new List<Combatant> { enemy, supporter }));
        Assert.That(controller.CurrentCombatant, Is.EqualTo(supporter));

        controller.EndCurrentTurn();
        Assert.That(controller.CurrentCombatant, Is.EqualTo(warrior));
    }

    [Test]
    public void Level5BattleController_EndCurrentTurn_SkipsDeadCombatants()
    {
        Combatant warrior = CreateCombatant("Warrior", false, 50, 10, 0);
        Combatant tank = CreateCombatant("Tank", false, 0, 0, 0);
        Combatant supporter = CreateCombatant("Support", false, 50, 10, 0);
        Level5BattleController controller = CreateBattleController(warrior, tank, supporter);

        controller.EndCurrentTurn();

        Assert.That(controller.CurrentCombatant, Is.EqualTo(supporter));
        Assert.That(controller.CurrentTurnIndex, Is.EqualTo(2));
    }

    [Test]
    public void Level5BattleController_EnemyTurn_CanDefeatPlayerWhenAllPlayersDie()
    {
        Combatant warrior = CreateCombatant("Warrior", false, 10, 1, 0);
        Combatant support = CreateCombatant("Support", false, 10, 1, 0);
        Combatant enemy = CreateCombatant("Enemy", true, 50, 30, 0);
        Level5BattleController controller = CreateBattleController(warrior, support, enemy);

        // Keep only one living player at 1 hp. The enemy's targeting and second attack are random, so with two
        // living players the outcome would depend on the roll. With one, the first hit always ends the battle.
        support.TakeDamage(support.MaxHP);
        warrior.TakeDamage(warrior.MaxHP - 1);
        SetPrivateField(controller, "currentIndex", 2);

        controller.TriggerEnemyAttack();
        Assert.That(controller.State, Is.EqualTo(Level5BattleController.BattleState.Defeat));
    }

    [Test]
    public void Level5BattleController_Start_UsesFirstAliveCombatant()
    {
        // Turn order matches the game: the Final Enemy acts last.
        Combatant warrior = CreateCombatant("Warrior", false, 0, 0, 0);
        Combatant support = CreateCombatant("Support", false, 30, 0, 0);
        Combatant enemy = CreateCombatant("Enemy", true, 20, 0, 0);

        Level5BattleController controller = CreateBattleController(warrior, support, enemy);

        // Warrior is dead, so Start should move the battle to Support.
        InvokeStart(controller);

        Assert.That(controller.CurrentTurnIndex, Is.EqualTo(1));
        Assert.That(controller.CurrentCombatant, Is.EqualTo(support));
    }

    [Test]
    public void GameProgressManager_LoadsAndResetsProgressForMultipleKeys()
    {
        ResetGameProgressManager();
        PlayerPrefs.SetInt(GameProgressManager.Level3CompletedKey, 1);
        PlayerPrefs.SetInt(GameProgressManager.Level4CompletedKey, 1);
        PlayerPrefs.SetInt(GameProgressManager.Level5CompletedKey, 1);
        PlayerPrefs.Save();

        GameProgressManager manager = new GameObject("GameProgressManager_MultiLoad").AddComponent<GameProgressManager>();
        typeof(GameProgressManager)
            .GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(manager, null);

        Assert.That(manager.Level3Completed, Is.True);
        Assert.That(manager.Level4Completed, Is.True);
        Assert.That(manager.Level5Completed, Is.True);

        manager.StartNewGame();
        Assert.That(manager.Level3Completed, Is.False);
        Assert.That(manager.Level4Completed, Is.False);
        Assert.That(manager.Level5Completed, Is.False);
    }

    private static T CreateComponent<T>(string objectName) where T : Component
    {
        GameObject go = new GameObject(objectName);
        return go.AddComponent<T>();
    }

    private static Health CreateFreshHealth(string objectName)
    {
        Health health = CreateComponent<Health>(objectName);
        SetPrivateField(health, "currentHealth", 10);
        SetPrivateField(health, "hasInvokedZero", false);
        return health;
    }

    private static PlayerExperience CreateFreshPlayerExperience(string objectName)
    {
        PlayerExperience experience = CreateComponent<PlayerExperience>(objectName);
        experience.ResetExperience();
        return experience;
    }

    private static Combatant CreateCombatant(string name, bool isEnemy, int maxHp, int attack, int defense)
    {
        GameObject go = new GameObject(name);
        Combatant combatant = go.AddComponent<Combatant>();
        SetPrivateField(combatant, "combatantName", name);
        SetPrivateField(combatant, "isEnemy", isEnemy);
        SetPrivateField(combatant, "maxHP", maxHp);
        SetPrivateField(combatant, "attack", attack);
        SetPrivateField(combatant, "defense", defense);
        combatant.ResetToFullHP();
        return combatant;
    }

    private static Level5BattleController CreateBattleController(params Combatant[] combatants)
    {
        GameObject go = new GameObject("BattleController");
        Level5BattleController controller = go.AddComponent<Level5BattleController>();
        SetPrivateField(controller, "turnOrder", combatants);
        SetPrivateField(controller, "currentIndex", 0);
        return controller;
    }

    private static void InvokeStart(Level5BattleController controller)
    {
        MethodInfo start = typeof(Level5BattleController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        start.Invoke(controller, null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
            return;

        field.SetValue(target, value);
    }

    private static void ResetSettingsManager()
    {
        var instanceField = typeof(SettingsManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        instanceField?.SetValue(null, null);

        var mixerField = typeof(SettingsManager).GetField("mixer", BindingFlags.Static | BindingFlags.NonPublic);
        mixerField?.SetValue(null, null);

        var musicGroupField = typeof(SettingsManager).GetField("musicGroup", BindingFlags.Static | BindingFlags.NonPublic);
        musicGroupField?.SetValue(null, null);

        var sfxGroupField = typeof(SettingsManager).GetField("sfxGroup", BindingFlags.Static | BindingFlags.NonPublic);
        sfxGroupField?.SetValue(null, null);

        foreach (string key in new[]
        {
            SettingsManager.MusicVolumeKey,
            SettingsManager.SfxVolumeKey,
            SettingsManager.FullscreenKey,
            SettingsManager.WindowWidthKey,
            SettingsManager.WindowHeightKey
        })
        {
            PlayerPrefs.DeleteKey(key);
        }

        SettingsManager existing = Object.FindAnyObjectByType<SettingsManager>();
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
    }

    /// <summary>Simulates an application restart: drops the live manager but keeps PlayerPrefs so it reloads from them.</summary>
    private static void RestartSettingsManager()
    {
        var instanceField = typeof(SettingsManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        instanceField?.SetValue(null, null);

        SettingsManager existing = Object.FindAnyObjectByType<SettingsManager>();
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        // Awake does not run for AddComponent in EditMode, so invoke the load step explicitly.
        SettingsManager restarted = SettingsManager.Instance;
        var load = typeof(SettingsManager).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic);
        load?.Invoke(restarted, null);
    }

    private static void ResetGameProgressManager()
    {
        var instanceField = typeof(GameProgressManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        instanceField?.SetValue(null, null);

        foreach (string key in new[]
        {
            GameProgressManager.SlidingPuzzleCompletedKey,
            GameProgressManager.LongestPathCompletedKey,
            GameProgressManager.ConstraintPuzzleCompletedKey,
            GameProgressManager.Level3CompletedKey,
            GameProgressManager.Level4CompletedKey,
            GameProgressManager.Level5CompletedKey
        })
        {
            PlayerPrefs.DeleteKey(key);
        }

        GameProgressManager existing = Object.FindAnyObjectByType<GameProgressManager>();
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
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

