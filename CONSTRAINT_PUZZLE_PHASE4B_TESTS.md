# MANUAL TEST INSTRUCTIONS — ConstraintPuzzle_New Phase 4B

## SETUP INSTRUCTIONS (BEFORE TESTING)

### Step 1: Add ConstraintPuzzleIntegration to Scene
1. Open `Assets/Scenes/Puzzles/ConstraintPuzzle_New.unity` in Unity Editor
2. Right-click in Hierarchy → Create Empty → Name it "ConstraintPuzzleIntegration"
3. Add Component → Search "ConstraintPuzzleIntegration" → Add it
4. Leave the Manager and StateManager fields empty (script auto-creates them)

### Step 2: Add ConstraintPuzzleCheckHandler to CheckButton
1. Select "CheckButton" in the Hierarchy
2. Add Component → Search "ConstraintPuzzleCheckHandler" → Add it
3. Leave Integration field empty (script auto-finds it)
4. (Optional) Assign a Text component to "Feedback Text" field if you want visual feedback

### Step 3: Compile and Load
1. Save the scene
2. Go to Scripting → Recompile All
3. Wait for compilation to complete

---

## MANUAL TESTS (Run in Play Mode)

### TEST 1 — State Empty
**Objective**: Verify system starts clean

**Steps**:
1. Click Play
2. Open Console (Ctrl+`)
3. Look for log: `[ConstraintPuzzleIntegration] Initialized successfully`
4. All 6 slots should be visually empty

**Expected Result**: ✓ No letters in any slot, console shows init success

---

### TEST 2 — Placement
**Objective**: Verify drag & drop still works and state updates

**Steps**:
1. From Play Mode (or reload scene if stopped)
2. Drag Letter_A to Slot_1
3. Check Console for: `[Drop] Letter_A placed in Slot_1`
4. Also look for: `[ConstraintPuzzleIntegration] Placing A at position 1`
5. Drag Letter_B to Slot_2
6. Repeat checks

**Expected Result**: ✓ Letters appear in slots, console shows both drop and integration messages

---

### TEST 3 — Rectification (Simple Movement)
**Objective**: Verify letter can move from one slot to another

**Steps**:
1. Ensure Letter_A is in Slot_1
2. Drag Letter_A to Slot_3
3. Check Console for movement message

**Expected Result**: ✓ Letter_A moves to Slot_3, Slot_1 becomes empty

---

### TEST 4 — Swap (Critical Test)
**Objective**: Verify swap/interchange works

**Setup**:
- Slot_1 = Letter_A
- Slot_2 = Letter_B

**Steps**:
1. Drag Letter_A over Slot_2
2. Check Console for swap message

**Expected Result**: ✓
- Slot_1 now contains Letter_B
- Slot_2 now contains Letter_A
- Console shows: `[Swap] Letter_A ↔ Letter_B in slot 2`

---

### TEST 5 — Invalid Configuration
**Objective**: Verify Check rejects wrong answer

**Setup**:
- Place letters randomly/incorrectly:
  - Slot_1 = A, Slot_2 = B, Slot_3 = C, Slot_4 = D, Slot_5 = E, Slot_6 = F

**Steps**:
1. Click CheckButton
2. Look at Console

**Expected Result**: ✓
- Console shows: `[ConstraintPuzzleIntegration] ✗ Arrangement is invalid: The configuration does not satisfy the clues.`
- Puzzle does NOT become solved (no victory)
- Letters remain in place (can continue playing)
- Puzzle is still playable

---

### TEST 6 — Correct Solution
**Objective**: Verify Check accepts the correct solution

**Setup** (THE ONLY VALID SOLUTION):
- Slot_1 = Letter_B
- Slot_2 = Letter_C
- Slot_3 = Letter_A
- Slot_4 = Letter_D
- Slot_5 = Letter_E
- Slot_6 = Letter_F

**Steps**:
1. Place all letters according to solution (drag each one to correct slot)
2. Click CheckButton
3. Check Console

**Expected Result**: ✓
- Console shows: `[ConstraintPuzzleIntegration] ✓ Puzzle solved!`
- Console shows: `[ConstraintPuzzleCheckHandler] Puzzle solved!`
- Puzzle state is now `IsSolved = true`
- If feedback text exists, shows green message: "¡CORRECTO! Puzzle completado."

---

### TEST 7 — Repeated Check After Victory
**Objective**: Verify Check button doesn't repeat victory

**Setup**:
- Puzzle already solved (from TEST 6)

**Steps**:
1. Click CheckButton multiple times
2. Check Console

**Expected Result**: ✓
- First click in TEST 6 caused victory
- Subsequent clicks log: `[ConstraintPuzzleCheckHandler] Puzzle already solved, ignoring repeated Check`
- Victory does NOT fire multiple times
- No duplicate messages

---

### TEST 8 — Persistence During Play
**Objective**: Verify state stays consistent during extended play

**Steps**:
1. Place Letter_A in Slot_1
2. Move Letter_B to Slot_2
3. Swap Letter_A and Letter_B
4. Move Letter_C to Slot_3
5. Check Console for all movement logs
6. Verify UI matches internal state (visual letter positions = console state)

**Expected Result**: ✓
- All movements logged correctly
- No letters disappear or teleport
- Visual state matches console state
- No error messages about missing references

---

## CRITICAL CHECKS AFTER TESTING

### Save and Reload
1. Stop Play Mode
2. File → Save Scene (Ctrl+S)
3. Close ConstraintPuzzle_New.unity
4. Wait a few seconds
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

## TEST SUMMARY TABLE

| Test | Pass | Evidence |
|------|------|----------|
| 1 — State Empty | ✓/✗ | Console init msg + empty slots |
| 2 — Placement | ✓/✗ | Drag works + console logs placement |
| 3 — Simple Move | ✓/✗ | Letter moves between slots |
| 4 — Swap | ✓/✗ | A↔B exchange works, console shows swap |
| 5 — Invalid Check | ✓/✗ | Invalid arrangement rejected, puzzle playable |
| 6 — Valid Check | ✓/✗ | Correct solution accepted, solved state set |
| 7 — No Repeat Victory | ✓/✗ | Check doesn't fire victory twice |
| 8 — Persistence | ✓/✗ | All movements consistent, no data corruption |
| Scene Reload | ✓/✗ | No PPtr/corruption errors |

---

## ROLLBACK INSTRUCTIONS (If tests fail)

If tests fail and you need to revert:

1. Close scene WITHOUT saving
2. Revert changes from Git (if available)
3. Or manually remove:
   - ConstraintPuzzleIntegration component from scene
   - ConstraintPuzzleCheckHandler from CheckButton
4. Restore scene to known-good state

**DO NOT** edit YAML manually.
**DO NOT** try to auto-repair the scene.
