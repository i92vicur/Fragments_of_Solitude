using UnityEngine;

// Mirrors the dialogue text's visibility: shown only while there is text on screen,
// exactly like the text itself, without depending on any extra wiring.
// Uses a CanvasGroup (not SetActive) so this GameObject's own Update keeps running
// and can re-show the panel later; SetActive(false) would freeze this script forever.
[RequireComponent(typeof(CanvasGroup))]
public class DialoguePanelVisibility : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        var manager = DialogueManager.manager;
        bool shouldShow = manager != null
            && manager.dialogueText != null
            && !string.IsNullOrEmpty(manager.dialogueText.text);

        canvasGroup.alpha = shouldShow ? 1f : 0f;
        canvasGroup.blocksRaycasts = shouldShow;
    }
}
