using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassArea : MonoBehaviour
{
    // Start is called before the first frame update
    PlayerMovement playerMovement;
    public bool isMoving = false;
    public bool hasGrass = false;

    public float grassMoveDuration = 1f;

    private float grassMoveTimer = 0f;


    void Start()
    {
        
    }
    private void OnTriggerExit(Collider other)
    {

        PlayerMovement playerMovement =
        other.GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.isInGrass = false;
            //isMoving = false;
            DevLog.Log("GRASS TRIGGER EXIT |  " + gameObject.name);
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement playerMovement =
         other.GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.isInGrass = true;

            isMoving = true;
            grassMoveTimer = grassMoveDuration;

            GuardMovement[] guards = FindObjectsOfType<GuardMovement>();

            foreach (GuardMovement guard in guards)
            {
                guard.HearGrassNoise(transform.position, 10f, gameObject.name);

            }
            DevLog.Log("GRASS TRIGGER ENTER | "+gameObject.name);
        }
    }
   

    // Update is called once per frame
    void Update()
    {
        if (isMoving) {
            grassMoveTimer -= Time.deltaTime;
            if (grassMoveTimer <= 0f) {
                isMoving = false;
                grassMoveTimer = 0f;
                DevLog.Log("GRASS STOP : "+ gameObject.name);
            }
            
        }
    }
}
