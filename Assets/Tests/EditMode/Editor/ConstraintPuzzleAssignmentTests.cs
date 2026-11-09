using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConstraintPuzzleAssignmentTests
{
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private readonly Dictionary<char, DraggableLetter> letters = new Dictionary<char, DraggableLetter>();
    private readonly SlotDropZone[] slots = new SlotDropZone[ConstraintPuzzleManager.PositionCount + 1];

    private ConstraintPuzzleManager manager;
    private ConstraintPuzzleStateManager stateManager;
    private ConstraintPuzzleIntegration integration;
    private RectTransform lettersArea;

    [SetUp]
    public void SetUp()
    {
        GameObject canvasGO = Track(new GameObject("ConstraintPuzzleCanvas", typeof(Canvas)));

        manager = Track(new GameObject("ConstraintPuzzleManager")).AddComponent<ConstraintPuzzleManager>();
        stateManager = Track(new GameObject("ConstraintPuzzleStateManager")).AddComponent<ConstraintPuzzleStateManager>();
        integration = Track(new GameObject("ConstraintPuzzleIntegration")).AddComponent<ConstraintPuzzleIntegration>();

        lettersArea = CreateRect("LettersArea", canvasGO.transform);
        lettersArea.sizeDelta = new Vector2(400f, 400f);

        RectTransform slotsArea = CreateRect("SlotsArea", canvasGO.transform);

        for (int i = 1; i <= ConstraintPuzzleManager.PositionCount; i++)
        {
            GameObject slotGO = Track(new GameObject("Slot_" + i, typeof(RectTransform)));
            slotGO.transform.SetParent(slotsArea, false);
            slotGO.AddComponent<Image>();
            slots[i] = slotGO.AddComponent<SlotDropZone>();
        }

        foreach (string id in ConstraintPuzzleManager.ItemIds)
        {
            GameObject letterGO = Track(new GameObject("Letter_" + id, typeof(RectTransform)));
            letterGO.transform.SetParent(lettersArea, false);
            letterGO.AddComponent<Image>();
            letters[id[0]] = letterGO.AddComponent<DraggableLetter>();
        }
    }

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
        letters.Clear();
    }

    [Test]
    public void LettersArea_ToEmptySlot_PlacesLetterAndSyncsBothStates()
    {
        bool accepted = slots[1].TryAssignLetter(letters['A']);

        Assert.IsTrue(accepted);
        Assert.AreEqual(slots[1].transform, letters['A'].transform.parent);
        Assert.AreEqual('A', stateManager.GetLetterInSlot(1));
        Assert.AreEqual("A", manager.GetItemAtPosition(1));
        AssertConsistent();
    }

    [Test]
    public void LettersArea_ToOccupiedSlot_DisplacedLetterReturnsToLettersArea()
    {
        slots[1].TryAssignLetter(letters['A']);

        bool accepted = slots[1].TryAssignLetter(letters['B']);

        Assert.IsTrue(accepted);
        Assert.AreEqual(slots[1].transform, letters['B'].transform.parent);
        Assert.AreEqual(lettersArea, letters['A'].transform.parent);
        Assert.AreEqual('B', stateManager.GetLetterInSlot(1));
        Assert.AreEqual(-1, stateManager.GetSlotContainingLetter('A'));
        AssertConsistent();
    }

    [Test]
    public void SlotToEmptySlot_MovesLetterAndClearsSource()
    {
        slots[1].TryAssignLetter(letters['A']);

        bool accepted = slots[2].TryAssignLetter(letters['A']);

        Assert.IsTrue(accepted);
        Assert.AreEqual(slots[2].transform, letters['A'].transform.parent);
        Assert.AreEqual(' ', stateManager.GetLetterInSlot(1));
        Assert.AreEqual('A', stateManager.GetLetterInSlot(2));
        Assert.IsNull(manager.GetItemAtPosition(1));
        Assert.AreEqual("A", manager.GetItemAtPosition(2));
        AssertConsistent();
    }

    [Test]
    public void SlotToOccupiedSlot_SwapsBothLetters()
    {
        slots[1].TryAssignLetter(letters['A']);
        slots[2].TryAssignLetter(letters['B']);

        bool accepted = slots[2].TryAssignLetter(letters['A']);

        Assert.IsTrue(accepted);
        Assert.AreEqual(slots[2].transform, letters['A'].transform.parent);
        Assert.AreEqual(slots[1].transform, letters['B'].transform.parent);
        Assert.AreEqual('B', stateManager.GetLetterInSlot(1));
        Assert.AreEqual('A', stateManager.GetLetterInSlot(2));
        Assert.AreEqual("B", manager.GetItemAtPosition(1));
        Assert.AreEqual("A", manager.GetItemAtPosition(2));
        AssertConsistent();
    }

    [Test]
    public void InvalidLetter_IsRejectedWithoutChangingState()
    {
        bool accepted = integration.TryAssignLetter('Z', 1, 0, out char displaced);

        Assert.IsFalse(accepted);
        Assert.AreEqual(' ', displaced);
        Assert.AreEqual(' ', stateManager.GetLetterInSlot(1));
        AssertConsistent();
    }

    [Test]
    public void DropOutsideSlotAndLettersArea_RestoresPreviousSlot()
    {
        slots[3].TryAssignLetter(letters['A']);
        DraggableLetter letter = letters['A'];

        PointerEventData outside = new PointerEventData(null) { position = new Vector2(-10000f, -10000f) };
        letter.OnBeginDrag(outside);
        letter.OnEndDrag(outside);

        Assert.AreEqual(slots[3].transform, letter.transform.parent);
        Assert.AreEqual('A', stateManager.GetLetterInSlot(3));
        AssertConsistent();
    }

    [Test]
    public void DropOnLettersArea_ReturnsLetterAndFreesSlot()
    {
        slots[3].TryAssignLetter(letters['A']);
        DraggableLetter letter = letters['A'];

        PointerEventData overLettersArea = new PointerEventData(null) { position = Vector2.zero };
        letter.OnBeginDrag(overLettersArea);
        letter.OnEndDrag(overLettersArea);

        Assert.AreEqual(lettersArea, letter.transform.parent);
        Assert.AreEqual(' ', stateManager.GetLetterInSlot(3));
        Assert.IsNull(manager.GetItemAtPosition(3));
        AssertConsistent();
    }

    [Test]
    public void MouseDrop_OnSlot_UsesSharedAssignment()
    {
        PointerEventData drop = new PointerEventData(null) { pointerDrag = letters['C'].gameObject };

        slots[4].OnDrop(drop);

        Assert.AreEqual(slots[4].transform, letters['C'].transform.parent);
        Assert.AreEqual('C', stateManager.GetLetterInSlot(4));
        AssertConsistent();
    }

    [Test]
    public void MouseDrop_OnOwnSlot_KeepsStateUnchanged()
    {
        slots[2].TryAssignLetter(letters['D']);
        PointerEventData drop = new PointerEventData(null) { pointerDrag = letters['D'].gameObject };

        slots[2].OnDrop(drop);

        Assert.AreEqual(slots[2].transform, letters['D'].transform.parent);
        Assert.AreEqual('D', stateManager.GetLetterInSlot(2));
        AssertConsistent();
    }

    [Test]
    public void ResetPuzzle_ReturnsAllLettersAndClearsBothStates()
    {
        slots[1].TryAssignLetter(letters['A']);
        slots[2].TryAssignLetter(letters['B']);
        slots[5].TryAssignLetter(letters['E']);

        integration.ResetPuzzle();

        for (int i = 1; i <= ConstraintPuzzleManager.PositionCount; i++)
        {
            Assert.AreEqual(' ', stateManager.GetLetterInSlot(i));
            Assert.IsNull(manager.GetItemAtPosition(i));
            Assert.AreEqual(0, slots[i].transform.childCount, $"Slot {i} still has children after reset");
        }

        foreach (DraggableLetter letter in letters.Values)
        {
            Assert.AreEqual(lettersArea, letter.transform.parent);
        }

        Assert.IsFalse(manager.IsSolved);
        AssertConsistent();
    }

    [Test]
    public void ResetPuzzle_AllowsAssignmentsAgain()
    {
        slots[1].TryAssignLetter(letters['A']);
        integration.ResetPuzzle();

        bool accepted = slots[1].TryAssignLetter(letters['B']);

        Assert.IsTrue(accepted);
        Assert.AreEqual('B', stateManager.GetLetterInSlot(1));
        AssertConsistent();
    }

    private void AssertConsistent()
    {
        HashSet<char> seenInSlots = new HashSet<char>();

        for (int slot = 1; slot <= ConstraintPuzzleManager.PositionCount; slot++)
        {
            char stateLetter = stateManager.GetLetterInSlot(slot);
            string managerItem = manager.GetItemAtPosition(slot);
            DraggableLetter[] physical = slots[slot].GetComponentsInChildren<DraggableLetter>(true);

            if (stateLetter == ' ')
            {
                Assert.IsTrue(string.IsNullOrEmpty(managerItem), $"Slot {slot}: manager has '{managerItem}' but state is empty");
                Assert.AreEqual(0, physical.Length, $"Slot {slot}: physical letter present but state is empty");
            }
            else
            {
                Assert.AreEqual(stateLetter.ToString(), managerItem, $"Slot {slot}: manager and state differ");
                Assert.AreEqual(1, physical.Length, $"Slot {slot}: expected exactly one physical letter");
                Assert.AreEqual(stateLetter, physical[0].Letter, $"Slot {slot}: physical letter differs from state");
                Assert.IsTrue(seenInSlots.Add(stateLetter), $"Letter {stateLetter} is registered in more than one slot");
            }
        }

        foreach (DraggableLetter letter in letters.Values)
        {
            bool inSlotState = stateManager.GetSlotContainingLetter(letter.Letter) > 0;
            bool inLettersArea = letter.transform.parent == lettersArea;
            Assert.AreNotEqual(inSlotState, inLettersArea, $"Letter {letter.Letter} must be in exactly one place");
        }
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = Track(new GameObject(objectName, typeof(RectTransform)));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private GameObject Track(GameObject go)
    {
        spawnedObjects.Add(go);
        return go;
    }
}
