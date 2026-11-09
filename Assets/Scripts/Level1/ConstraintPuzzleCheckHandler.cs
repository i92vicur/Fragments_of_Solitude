/*
ConstraintPuzzleCheckHandler.cs

Purpose:
- Controls the constraint puzzle check handler flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ConstraintPuzzleCheckHandler : MonoBehaviour
{
    [SerializeField] private ConstraintPuzzleIntegration integration;
    [SerializeField] private TextMeshProUGUI feedbackText;
    
    private Button checkButton;

    private void Awake()
    {
        checkButton = GetComponent<Button>();
        
        if (checkButton != null)
        {
            checkButton.onClick.AddListener(HandleCheckClick);
        }

        if (integration == null)
        {
            integration = FindFirstObjectByType<ConstraintPuzzleIntegration>();
        }

        if (integration == null)
        {
            Debug.LogWarning("[ConstraintPuzzleCheckHandler] ConstraintPuzzleIntegration not found in scene");
        }
    }

    private void HandleCheckClick()
    {
        if (integration == null)
        {
            Debug.LogError("[ConstraintPuzzleCheckHandler] No integration available");
            return;
        }

        if (integration.IsSolved)
        {
            Debug.Log("[ConstraintPuzzleCheckHandler] Puzzle already solved, ignoring repeated Check");
            return;
        }

        bool isValid = integration.ValidateAndComplete();

        if (isValid)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "Correct! Puzzle completed.";
                feedbackText.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
            }
            Debug.Log("[ConstraintPuzzleCheckHandler] Puzzle solved!");
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.text = "Incorrect arrangement. Try again.";
                feedbackText.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
            }
            Debug.Log("[ConstraintPuzzleCheckHandler] Invalid arrangement");
        }
    }

    private void OnDestroy()
    {
        if (checkButton != null)
        {
            checkButton.onClick.RemoveListener(HandleCheckClick);
        }
    }
}
