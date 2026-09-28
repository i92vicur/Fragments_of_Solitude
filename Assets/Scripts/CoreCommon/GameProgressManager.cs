/*
GameProgressManager.cs

Purpose:
- Controls the game progress manager flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class GameProgressManager : MonoBehaviour
{
    public const string SlidingPuzzleCompletedKey = "Progress.SlidingPuzzleCompleted";
    public const string LongestPathCompletedKey = "Progress.LongestPathCompleted";
    public const string ConstraintPuzzleCompletedKey = "Progress.ConstraintPuzzleCompleted";
    public const string Level3CompletedKey = "Progress.Level3Completed";
    public const string Level4CompletedKey = "Progress.Level4Completed";
    public const string Level5CompletedKey = "Progress.Level5Completed";

    private static GameProgressManager instance;

    public static GameProgressManager Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = FindFirstObjectByType<GameProgressManager>();
            if (instance == null)
            {
                var go = new GameObject(nameof(GameProgressManager));
                instance = go.AddComponent<GameProgressManager>();
            }

            return instance;
        }
    }

    private bool slidingPuzzleCompleted;
    private bool longestPathCompleted;
    private bool constraintPuzzleCompleted;
    private bool level3Completed;
    private bool level4Completed;
    private bool level5Completed;

    public bool SlidingPuzzleCompleted => slidingPuzzleCompleted;
    public bool LongestPathCompleted => longestPathCompleted;
    public bool ConstraintPuzzleCompleted => constraintPuzzleCompleted;
    public bool Level3Completed => level3Completed;
    public bool Level4Completed => level4Completed;
    public bool Level5Completed => level5Completed;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public bool GetSlidingPuzzleCompleted() => slidingPuzzleCompleted;

    public bool GetLongestPathCompleted() => longestPathCompleted;

    public bool GetConstraintPuzzleCompleted() => constraintPuzzleCompleted;

    public bool GetLevel3Completed() => level3Completed;

    public bool GetLevel4Completed() => level4Completed;

    public bool GetLevel5Completed() => level5Completed;

    public void MarkSlidingPuzzleCompleted()
    {
        if (slidingPuzzleCompleted) return;

        slidingPuzzleCompleted = true;
        Save();
    }

    public void MarkLongestPathCompleted()
    {
        if (longestPathCompleted) return;

        longestPathCompleted = true;
        Save();
    }

    public void MarkConstraintPuzzleCompleted()
    {
        if (constraintPuzzleCompleted) return;

        constraintPuzzleCompleted = true;
        Save();
    }

    public void MarkLevel3Completed()
    {
        if (level3Completed) return;

        level3Completed = true;
        Save();
    }

    public void MarkLevel4Completed()
    {
        if (level4Completed) return;

        level4Completed = true;
        Save();
    }

    public void MarkLevel5Completed()
    {
        if (level5Completed) return;

        level5Completed = true;
        Save();
    }

    public void StartNewGame()
    {
        slidingPuzzleCompleted = false;
        longestPathCompleted = false;
        constraintPuzzleCompleted = false;
        level3Completed = false;
        level4Completed = false;
        level5Completed = false;
        Save();
    }

    private void Load()
    {
        slidingPuzzleCompleted = LoadBool(SlidingPuzzleCompletedKey);
        longestPathCompleted = LoadBool(LongestPathCompletedKey);
        constraintPuzzleCompleted = LoadBool(ConstraintPuzzleCompletedKey);
        level3Completed = LoadBool(Level3CompletedKey);
        level4Completed = LoadBool(Level4CompletedKey);
        level5Completed = LoadBool(Level5CompletedKey);
    }

    private static bool LoadBool(string key)
    {
        return PlayerPrefs.GetInt(key, 0) != 0;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(SlidingPuzzleCompletedKey, slidingPuzzleCompleted ? 1 : 0);
        PlayerPrefs.SetInt(LongestPathCompletedKey, longestPathCompleted ? 1 : 0);
        PlayerPrefs.SetInt(ConstraintPuzzleCompletedKey, constraintPuzzleCompleted ? 1 : 0);
        PlayerPrefs.SetInt(Level3CompletedKey, level3Completed ? 1 : 0);
        PlayerPrefs.SetInt(Level4CompletedKey, level4Completed ? 1 : 0);
        PlayerPrefs.SetInt(Level5CompletedKey, level5Completed ? 1 : 0);
        PlayerPrefs.Save();
    }
}