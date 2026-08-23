using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DetectPlayer : MonoBehaviour
{
    
    [HideInInspector]
    public bool inRange = false;

    //public GameObject alertObject;
    public float alertSpeed = 0.025f;
    public float alertHeight = 0.25f;
    
    private Vector3 velocity = Vector3.zero;
    private Vector3 startingPos;
    private Collider2D detectionCollider;
    private readonly Collider2D[] overlapResults = new Collider2D[8];

    private void Awake()
    {
        detectionCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (detectionCollider == null || !detectionCollider.enabled)
            return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        int overlapCount = detectionCollider.Overlap(filter, overlapResults);

        inRange = false;
        for (int i = 0; i < overlapCount; i++)
        {
            if (overlapResults[i] == detectionCollider)
                continue;

            if (IsPlayerCollider(overlapResults[i]))
            {
                inRange = true;
                break;
            }
        }
    }

    //Detects if Player is in range to the NPC
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayerCollider(collision))
        {
            inRange = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayerCollider(collision))
        {
            inRange = false;
        }
    }

    private static bool IsPlayerCollider(Collider2D collision)
    {
        if (collision == null)
            return false;

        if (collision.CompareTag("Player"))
            return true;

        if (collision.attachedRigidbody != null && collision.attachedRigidbody.CompareTag("Player"))
            return true;

        Transform root = collision.transform.root;
        return root != null && root.CompareTag("Player");
    }


    //EVERYTHING UNDER HERE IS OPTIONAL AND CAN BE COMMENTED OUT IF NOT WANTED
    private void Start()
    {
       // startingPos = new Vector3(alertObject.transform.position.x, alertObject.transform.position.y, 0);
    }
/*
    private void Update()
    {
        CheckRange();
    }

    void CheckRange()
    {
        if (inRange)
        {
            if (alertObject.transform.localPosition.y <= alertHeight)
            {
                Display(alertObject);
            }
        }
        else
        {
            if (alertObject.transform.localPosition.y != 0)
            {
                Hide(alertObject);
            }
        }
    }*/

    void Display(GameObject displayObject)
    {
        Vector3 targetPos = new Vector3(displayObject.transform.position.x, displayObject.transform.position.y + alertHeight, 0);
        displayObject.transform.position = Vector3.SmoothDamp(displayObject.transform.position, targetPos, ref velocity, alertSpeed);
    }

    void Hide(GameObject displayObject)
    {
        Vector3 targetPos = startingPos;
        displayObject.transform.position = Vector3.SmoothDamp(displayObject.transform.position, targetPos, ref velocity, alertSpeed);
    }
}
