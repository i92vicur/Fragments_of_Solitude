/*
ConstraintPuzzleCheckHandler.cs

Handles the Check button click in ConstraintPuzzle_New.
Connects the UI button to the validation logic via ConstraintPuzzleIntegration.

Attached to: CheckButton
*/

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ConstraintPuzzleCheckHandler : MonoBehaviour
{
    [SerializeField] private ConstraintPuzzleIntegration integration;
    [SerializeField] private Text feedbackText;
    
    private Button checkButton;
    private bool alreadySolved;

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

        if (alreadySolved || integration.IsSolved)
        {
            Debug.Log("[ConstraintPuzzleCheckHandler] Puzzle already solved, ignoring repeated Check");
            alreadySolved = true;
            return;
        }

        bool isValid = integration.ValidateAndComplete();

        if (isValid)
        {
            alreadySolved = true;
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
