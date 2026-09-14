using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Parser))]
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager manager { get; set; }

    [Header("NPC UI")]
    public TextMeshProUGUI dialogueText;
    public Image sprite;
    public TextMeshProUGUI characterName;
    [Tooltip("Optional background panel(s) shown only while a conversation is active.")]
    public GameObject dialogueBackground;
    [Space(10)]
    [Header("Type Writer"), Space(5)]
    public float typeWriterSpeed = 0.025f;
    [Space(10)]
    public bool autoNextLine = false;
    public float waitForNextLine;
    [Space(10)]
    public bool pauseOnPunctuation = false;
    public float pauseLength = 0.6f;
    [Space(10)]
    public bool playTextAudio = false;
    public AudioClip audioClip;
    public AudioSource audioSource;
    [Space(10)]
    [Header("Player Controls")]
    [Tooltip("Root GameObject of the player. EasyDS will find and disable the correct movement controller automatically.")]
    public GameObject playerObject;
    [Tooltip("Interaction key used to start/advance dialogue. Defaults to E.")]
    public KeyCode interactKey = KeyCode.E;

    private void Awake()
    {
        if (manager == null) 
        { 
            manager = this; 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource != null && audioSource.outputAudioMixerGroup == null)
            audioSource.outputAudioMixerGroup = SettingsManager.SfxGroup;
    }

    public void OnCharReveal(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
        audioSource.pitch = Random.Range(1f, 1.2f);
    }
}
