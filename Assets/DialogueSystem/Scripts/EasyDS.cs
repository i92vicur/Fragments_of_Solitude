using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

[RequireComponent(typeof(DetectPlayer))]
public class EasyDS : MonoBehaviour
{
    [Header("NPC Name")]
    public string Name;

    [Header("Dialogue Objects"), Tooltip("Dialogue Scriptable Objects"), Space(5)]
    public List<Dialogue> dialogue;

    DialogueManager manager;
    DetectPlayer player;
 
    private string rawLine;

    //lines
    public int lineNumber;

    //nodes
    public int nodeNumber;

    //start and end of dialogue
    private bool dialogueStarted = false;
    private bool dialogueFinished = false;

    private readonly float time = 1.5f;
    private float timer;

    private bool dialogueGateOpen = false;

    private void Start()
    {
        nodeNumber = 0;
        lineNumber = 0;

        manager = DialogueManager.manager;
        player = gameObject.GetComponent<DetectPlayer>();
        manager.dialogueText.text = "";
    }

    private void Update()
    {
        if (dialogue.Count != 0) //checks if there's any dialogue
        {
            if (!dialogueStarted)
            {
                StartDialogue();
            }
            else if (dialogueStarted)
            {
                RunDialogue();
            }
            if (dialogueFinished)
            {
                StartCoroutine(EndDialogue());
            }
        }
        else
        {
            return;
        }
    }

    //starts dialogue if player is in range 
    void StartDialogue()
    {
        if (player.inRange && WasInteractPressed())
        {
            manager.dialogueText.text = "";
            SetSprite(true);
            dialogueStarted = true;
            if (!dialogueGateOpen)
            {
                DialogueGate.Begin();
                dialogueGateOpen = true;
            }
            StartCoroutine(DisplayChar());
        }
    }

    //runs dialogue through to it's completion
    void RunDialogue()
    {
        timer += Time.deltaTime;
        if (timer >= time)
        {
            if (!Parser.parser.nodeFinished && !manager.autoNextLine)
            {
                if (Parser.parser.lineFinished && WasInteractPressed())
                {
                    NextLine();
                    Parser.parser.lineFinished = false;
                }
            }
            else if (!Parser.parser.nodeFinished && manager.autoNextLine)
            {
                if (Parser.parser.lineFinished)
                {
                    AutoNextLine();
                    Parser.parser.lineFinished = false;
                }
            }
        }
        if (WasInteractPressed() && Parser.parser.nodeFinished)
        {
            NextNode();
            SetSprite(false);
            SetPlayerControlEnabled(true);
            Parser.parser.nodeFinished = false;
        }    
    }

    //displays the characters from each line one by one
    //when all characters are displayed, check the line's tags for what comes next
    public IEnumerator DisplayChar()
    {
        float typingSpeed = manager.typeWriterSpeed;
        SetPlayerControlEnabled(false);

        rawLine = dialogue[nodeNumber].dialogue[lineNumber];
        
        foreach (char c in Parser.parser.Line(rawLine))
        {
            manager.dialogueText.text += c;
            if (manager.playTextAudio)
            {
                manager.OnCharReveal(manager.audioClip);
            }
            if (manager.pauseOnPunctuation)
            {
                typingSpeed = manager.typeWriterSpeed;
                char[] punctuation = { '.', ',', '!', '?' };
                //check for puncuation
                foreach (char punc in punctuation)
                {
                    if (c == punc)
                    {
                        typingSpeed = manager.typeWriterSpeed + manager.pauseLength;
                    }
                }
                if (c == ' ')
                {
                    typingSpeed = 0;
                }
            }
            yield return new WaitForSeconds(typingSpeed);
            typingSpeed = manager.typeWriterSpeed;
        }
        //this checks the tags at the end of the lines
        Parser.parser.CheckLine(rawLine);
    }

    //progresses to next line, if available
    void NextLine()
    {
        timer = 0;
        lineNumber++;
        manager.dialogueText.text = "";
        StartCoroutine(DisplayChar());
    }

    //when all lines in a node are finished, progresses to next node if available
    void NextNode()
    {
        //reset text
        manager.dialogueText.text = "";

        //reset numbers
        timer = 0;
        lineNumber = 0;

        //get next node
        nodeNumber++;

        //reset bools
        dialogueStarted = false;
        dialogueFinished = false;
        if (nodeNumber == dialogue.Count)
        {
            dialogueFinished = true;
        }      
    }

    //ends dialogue if no more nodes are available
    IEnumerator EndDialogue()
    {
        yield return new WaitForSeconds(manager.waitForNextLine);
        //reset text
        manager.dialogueText.text = "";
        
        //reset numbers
        lineNumber = 0;
        nodeNumber = 0;

        //reset bools
        Parser.parser.lineFinished = false;
        Parser.parser.nodeFinished = false;
        dialogueStarted = false;
        dialogueFinished = false;

        if (dialogueGateOpen)
        {
            DialogueGate.End();
            dialogueGateOpen = false;
        }
    }

    //when active, progresses to the next line after a set amount of time
    void AutoNextLine()
    {
        if (manager.autoNextLine)
        {
            Invoke("NextLine", manager.waitForNextLine);
        }
        else
        {
            return;
        }
    }

    private void OnDisable()
    {
        // safety net in case this NPC is disabled/destroyed mid-conversation
        if (dialogueGateOpen)
        {
            DialogueGate.End();
            dialogueGateOpen = false;
        }
    }

    //activates sprite image next to the dialogue
    void SetSprite(bool value)
    {
        if (manager.dialogueBackground != null)
            manager.dialogueBackground.SetActive(value);

        if (manager.sprite != null)
            manager.sprite.gameObject.SetActive(value);

        if (manager.characterName != null)
        {
            manager.characterName.text = Name;
            manager.characterName.gameObject.SetActive(value);
        }

        if (!value || manager.sprite == null)
            return;

        // fall back to a parent's SpriteRenderer for NPCs where EasyDS sits on a child trigger object
        var npcSprite = gameObject.GetComponentInChildren<SpriteRenderer>() ?? gameObject.GetComponentInParent<SpriteRenderer>();
        if (npcSprite == null)
            return;

        manager.sprite.sprite = npcSprite.sprite;
        manager.sprite.color = npcSprite.color;
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        if (manager == null || manager.playerObject == null)
            return;

        GameObject playerRoot = manager.playerObject;
        ToggleController(playerRoot.GetComponentInChildren<PlayerMovement>(true), enabled);
        ToggleController(playerRoot.GetComponentInChildren<PlatformerPlayerController>(true), enabled);
        ToggleController(playerRoot.GetComponentInChildren<PlatformerJumpController>(true), enabled);
        ToggleController(playerRoot.GetComponentInChildren<PlatformerCharacterAnimationController>(true), enabled);

        Rigidbody2D playerBody = playerRoot.GetComponentInChildren<Rigidbody2D>(true);
        if (playerBody == null)
            return;

        if (enabled)
        {
            playerBody.simulated = true;
            return;
        }

        playerBody.linearVelocity = Vector2.zero;
        playerBody.angularVelocity = 0f;
        playerBody.simulated = false;
    }

    private bool WasInteractPressed()
    {
        if (manager == null)
            return false;

        return manager.interactKey != KeyCode.None && Input.GetKeyDown(manager.interactKey);
    }

    private static void ToggleController(Behaviour controller, bool enabled)
    {
        if (controller != null)
            controller.enabled = enabled;
    }
}
