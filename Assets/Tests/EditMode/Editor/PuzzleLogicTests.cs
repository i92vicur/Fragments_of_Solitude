using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PuzzleLogicTests
{
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject spawned in spawnedObjects)
        {
            if (spawned != null)
            {
                Object.DestroyImmediate(spawned);
            }
        }

        spawnedObjects.Clear();
    }

    [Test]
    public void LongestPathPuzzleManager_ValidAndInvalidMoves_BehaveAsExpected()
    {
        LongestPathPuzzleManager manager = CreateComponent<LongestPathPuzzleManager>("LongestPathTest");

        Assert.That(manager.IsPuzzleFinished, Is.False);
        Assert.That(manager.CurrentNodeId, Is.EqualTo("A"));
        Assert.That(manager.IsValidMove("A_N1"), Is.True);
        Assert.That(manager.IsValidMove("N1_N2"), Is.False);

        Assert.That(manager.TryMoveAlongConnection("N1_N2"), Is.False);
        Assert.That(manager.CurrentNodeId, Is.EqualTo("A"));
        Assert.That(manager.TotalDistance, Is.EqualTo(0f));

        List<LongestPathPuzzleCompletionState> completionStates = new List<LongestPathPuzzleCompletionState>();
        float completedDistance = -1f;
        manager.RouteCompleted += (state, distance) =>
        {
            completionStates.Add(state);
            completedDistance = distance;
        };

        Assert.That(manager.TryMoveAlongConnection("A_N1"), Is.True);
        Assert.That(manager.TryMoveAlongConnection("N1_N3"), Is.True);
        Assert.That(manager.TryMoveAlongConnection("N3_N4"), Is.True);
        Assert.That(manager.TryMoveAlongConnection("N4_N5"), Is.True);
        Assert.That(manager.TryMoveAlongConnection("N5_B"), Is.True);

        Assert.That(manager.IsPuzzleFinished, Is.True);
        Assert.That(manager.CurrentNodeId, Is.EqualTo("B"));
        CollectionAssert.AreEqual(new[] { LongestPathPuzzleCompletionState.ValidRoute }, completionStates);
        Assert.That(completedDistance, Is.GreaterThan(0f));
        Assert.That(manager.TryMoveAlongConnection("N5_B"), Is.False);
    }

    [Test]
    public void ConstraintPuzzleManager_IncompleteInvalidAndValidConfigurations_AreDetected()
    {
        ConstraintPuzzleManager manager = CreateComponent<ConstraintPuzzleManager>("ConstraintPuzzleTest");

        Assert.That(manager.IsSolved, Is.False);
        Assert.That(manager.HasAllItemsPlaced(), Is.False);
        Assert.That(manager.IsValidArrangement(out string incompleteMessage), Is.False);
        Assert.That(incompleteMessage, Is.EqualTo("The configuration is incomplete."));

        PlaceArrangement(manager, new[] { "A", "B", "C", "D", "E", "F" });
        Assert.That(manager.HasAllItemsPlaced(), Is.True);
        Assert.That(manager.IsValidArrangement(out string invalidMessage), Is.False);
        Assert.That(invalidMessage, Is.EqualTo("The configuration does not satisfy the clues."));
        Assert.That(manager.TryCompleteIfValid(), Is.False);
        Assert.That(manager.IsSolved, Is.False);

        manager.ResetBoard();
        PlaceArrangement(manager, new[] { "B", "C", "A", "D", "E", "F" });
        Assert.That(manager.IsValidArrangement(out string validMessage), Is.True);
        Assert.That(validMessage, Is.EqualTo("Puzzle Complete!"));
        Assert.That(manager.TryCompleteIfValid(), Is.True);
        Assert.That(manager.IsSolved, Is.True);

        manager.ResetBoard();
        PlaceArrangement(manager, new[] { "A", "C", "B", "D", "E", "F" });
        Assert.That(manager.IsValidArrangement(out string violatedMessage), Is.False);
        Assert.That(violatedMessage, Is.EqualTo("The configuration does not satisfy the clues."));
    }

    [Test]
    public void ConstraintPuzzleManager_UsesDefinedSolution()
    {
        ConstraintPuzzleManager manager = CreateComponent<ConstraintPuzzleManager>("ConstraintPuzzleSolutionTest");

        string[] expectedSolution = manager.GetExpectedSolution();
        CollectionAssert.AreEqual(new[] { "B", "C", "A", "D", "E", "F" }, expectedSolution);

        PlaceArrangement(manager, expectedSolution);
        Assert.That(manager.TryCompleteIfValid(), Is.True);
        Assert.That(manager.IsSolved, Is.True);
    }

    private static T CreateComponent<T>(string objectName) where T : Component
    {
        GameObject go = new GameObject(objectName);
        return go.AddComponent<T>();
    }

    private static void PlaceArrangement(ConstraintPuzzleManager manager, IReadOnlyList<string> arrangement)
    {
        for (int i = 0; i < arrangement.Count; i++)
        {
            manager.PlaceItemAtPosition(arrangement[i], i + 1);
        }
    }
}