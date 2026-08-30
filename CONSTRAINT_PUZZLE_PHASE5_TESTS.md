# MANUAL TEST INSTRUCTIONS — ConstraintPuzzle_New Phase 5

## PHASE 5: BUTTON WIRING + ENGLISH CONSTRAINTS

Phase 5 completes the ConstraintPuzzle_New implementation by:
1. ✅ Wiring CheckButton to validation logic
2. ✅ Wiring BackButton to return to puzzle menu
3. ✅ Displaying puzzle constraints in English
4. ✅ Translating all player-facing messages to English

---

## SETUP (SAME AS PHASE 4B)

### Prerequisites
The scene should already have from Phase 4B:
- ✅ ConstraintPuzzleIntegration (auto-creates manager/state)
- ✅ ConstraintPuzzleCheckHandler on CheckButton
- ✅ SlotDropZone on all Slot_1...6
- ✅ DraggableLetter on all Letter_A...F
- ✅ ConstraintPuzzleSceneSetup (auto-wires everything)

### Phase 5 Changes (Automatic)
ConstraintPuzzleSceneSetup now ALSO:
- ✅ Adds ConstraintPuzzleCheckHandler to CheckButton (if missing)
- ✅ Adds ConstraintPuzzleBackHandler to BackButton (if missing)
- ✅ Adds ConstraintPuzzleCluesDisplay to Canvas (if missing)

**No manual wiring needed — all happens in Awake!**

### To Test
1. Open `Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity`
2. Save scene (Ctrl+S)
3. Click Play
4. Proceed with tests below

---

## MANUAL TESTS (Run in Play Mode)

### TEST 1 — Clues Visible in English
**Objective**: Verify all puzzle constraints are displayed

**Expected Clues**:
```
• Place letters A through F in slots 1 through 6.
• A must be in position 3 or 4.
• B must be at an end (position 1 or 6).
• D must be left of E (lower position number).
• E must be left of F (lower position number).
• B and D must be exactly 3 positions apart.
```

**Steps**:
1. Click Play
2. Look for a "CluesArea" at the bottom of the screen
3. Verify all 6 clues are visible and readable
4. Verify text is in ENGLISH
5. Verify clues do NOT block dragging or clicking

**Expected Result**: ✓
- 6 clues visible in English
- Text is readable, not overlapping with letters/slots
- No errors in Console related to CluesArea

---

### TEST 2 — Drag & Drop Still Works
**Objective**: Verify clues display doesn't interfere with gameplay

**Steps**:
1. From Play Mode
2. Try to drag Letter_A
3. Verify it drags smoothly
4. Drop it on Slot_1
5. Verify drop works

**Expected Result**: ✓
- Dragging works normally
- Drop works normally
- Clues stay in place, don't interfere

---

### TEST 3 — Invalid Check (English Message)
**Objective**: Verify Check rejection message is in English

**Setup**:
- Place letters randomly: Slot_1=A, 2=B, 3=C, 4=D, 5=E, 6=F

**Steps**:
1. Click CheckButton
2. Look at feedback text

**Expected Result**: ✓
- Feedback shows: **"Incorrect arrangement. Try again."** (RED text)
- Console shows validation failed
- Puzzle stays playable

---

### TEST 4 — Valid Check (English Message)
**Objective**: Verify Check success message is in English

**Setup** (THE ONLY VALID SOLUTION):
- Slot_1 = B, 2 = C, 3 = A, 4 = D, 5 = E, 6 = F

**Steps**:
1. Place all letters correctly (use clues to guide you!)
2. Click CheckButton
3. Look at feedback text

**Expected Result**: ✓
- Feedback shows: **"Correct! Puzzle completed."** (GREEN text)
- Console shows puzzle solved
- Puzzle state is locked (no more moves allowed if integrated)

---

### TEST 5 — Repeated Check (No Spam Victory)
**Objective**: Verify multiple Check clicks don't repeat victory

**Setup**:
- Puzzle already solved from TEST 4

**Steps**:
1. Click CheckButton 5+ times rapidly
2. Watch feedback text and Console

**Expected Result**: ✓
- First click → "Correct! Puzzle completed." (GREEN)
- Subsequent clicks → Same message, no repetition
- Console shows: "Puzzle already solved, ignoring repeated Check"
- Victory event fires only once

---

### TEST 6 — Back Button Works
**Objective**: Verify BackButton returns to PuzzleMenu

**Steps**:
1. Click BackButton
2. Wait for scene to load

**Expected Result**: ✓
- Scene changes to PuzzleMenu
- No errors in Console
- Can see puzzle selector menu

---

### TEST 7 — Back → Return → Check Still Works
**Objective**: Verify scene reloading doesn't break anything

**Steps**:
1. From PuzzleMenu, click on Constraint Puzzle again
2. Verify it loads
3. Do a full test cycle:
   - Drag letters to solution
   - Click Check
   - Verify "Correct!" message

**Expected Result**: ✓
- Scene loads cleanly
- All 6 clues visible
- Drag & drop works
- Check validates correctly
- Message is in English

---

### TEST 8 — Other Puzzles Still Work
**Objective**: Verify Constraint Puzzle changes don't break other puzzles

**Steps**:
1. From PuzzleMenu, click Sliding Puzzle 4x4
2. Verify it loads and works normally
3. Go back to PuzzleMenu
4. Click Longest Path Puzzle
5. Verify it loads and works normally
6. Back to PuzzleMenu
7. Re-enter Constraint Puzzle

**Expected Result**: ✓
- All puzzles load correctly
- No shared state corruption
- Each puzzle works independently
- Return to Constraint Puzzle shows clues again

---

## VERIFICATION CHECKLIST

### Buttons
- [ ] CheckButton responds to click
- [ ] CheckButton validates puzzle
- [ ] BackButton responds to click
- [ ] BackButton returns to PuzzleMenu
- [ ] Both buttons have correct handlers (ConstraintPuzzleCheckHandler, ConstraintPuzzleBackHandler)

### English Localization
- [ ] Clues displayed in English (6 clues visible)
- [ ] Invalid check message: "Incorrect arrangement. Try again."
- [ ] Valid check message: "Correct! Puzzle completed."
- [ ] No Spanish messages in Constraint Puzzle UI

### Game Mechanics
- [ ] Drag & drop works (not blocked by clues)
- [ ] Slots accept drops correctly
- [ ] Swap/rectification works
- [ ] Invalid arrangements rejected
- [ ] Valid solution accepted
- [ ] Victory doesn't repeat on multiple Check clicks
- [ ] Puzzle stays playable until solved

### Stability
- [ ] No console errors (except info/debug logs)
- [ ] No missing references
- [ ] Scene reloads without corruption
- [ ] Other puzzles unaffected

---

## CRITICAL CHECKS AFTER TESTING

### Save and Reload
1. Stop Play Mode
2. File → Save Scene (Ctrl+S)
3. Close ConstraintPuzzle_New.unity
4. Wait 3-5 seconds
5. Re-open ConstraintPuzzle_New.unity

### Verify No Corruption
Look for ANY of these errors:
- `Broken text PPtr`
- `Creating missing RectTransform`
- `Component has a broken GameObject reference`
- `Component could not be loaded`
- `Removing it`
- `does not reference component`

**If ANY appear**: STOP — do not continue. Scene is corrupted.

---

## ROLLBACK INSTRUCTIONS (If tests fail)

If tests fail catastrophically:

1. Close scene WITHOUT saving
2. Revert changes from Git (if available)
3. Or manually remove:
   - ConstraintPuzzleBackHandler from BackButton
   - ConstraintPuzzleCluesDisplay from Canvas
4. Restore scene to Phase 4B state

**DO NOT** edit YAML manually.

---

## TEST SUMMARY TABLE

| Test | Pass | Evidence |
|------|------|----------|
| 1 — Clues in English | ✓/✗ | 6 clues visible, English text, no errors |
| 2 — Drag Works | ✓/✗ | Clues don't block letter dragging |
| 3 — Invalid Check | ✓/✗ | Red message "Incorrect arrangement" |
| 4 — Valid Check | ✓/✗ | Green message "Correct! Puzzle completed." |
| 5 — No Repeat | ✓/✗ | Victory fires once only |
| 6 — Back Button | ✓/✗ | Returns to PuzzleMenu |
| 7 — Reload Cycle | ✓/✗ | Clues reload, Check works |
| 8 — Other Puzzles | ✓/✗ | Sliding/Longest Path unaffected |
| Scene Reload | ✓/✗ | No PPtr/corruption errors |

---

## SUCCESS CRITERIA

Phase 5 is complete when:

✅ All buttons are wired and functional
✅ All clues are visible in English
✅ All messages are in English
✅ All 8 tests pass
✅ Scene reloads without corruption
✅ No other puzzles affected
✅ Puzzle logic unchanged (still uses ConstraintPuzzleManager)

---

## KNOWN CONSTRAINTS

- CluesArea uses VerticalLayoutGroup (auto-sizing)
- Clues use raycastTarget=false (won't block drops)
- BackButton connects to existing SceneLoader
- Messages are UI text elements (not console output)
- Phase 5 does NOT add animations/effects (defer to future phase)
