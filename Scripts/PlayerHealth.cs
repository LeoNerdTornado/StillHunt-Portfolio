using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Net;
using Unity.VisualScripting;

public class PlayerHealth : MonoBehaviour
{
    public enum ZoneType
    {
        Head,
        Chest,
        Abdomen,
        LeftLeg,
        RightLeg

    }
    // Start is called before the first frame update
    [Header("BodyHealth")]
    [SerializeField] private int headHealth = 100;
    [SerializeField] private int chestHealth = 100;
    [SerializeField] private int abdomenHealth = 100;
    [SerializeField] private int leftLegHealth = 100;
    [SerializeField] private int rightLegHealth = 100;

    private bool headDepleted = false;
    private bool chestDepleted = false;
    private bool abdomenDepleted = false;
    private bool leftLegDepleted = false;
    private bool rightLegDepleted = false;
    private bool isDead = false;

    

    private SpawnManager spawnManager;
    private GuardMovement guardMovement;
    private Player player;

    public void TakeDamage(ZoneType zoneType,int damage)
    {
        switch (zoneType)
        {
            case ZoneType.Head:
                headHealth = Mathf.Max(0,headHealth - damage);
                //Debug.Log("GUARD SHOT | PLAYER HEALTH | HEAD | Health: " + headHealth);
            break;
            case ZoneType.Chest:
                chestHealth = Mathf.Max(0, chestHealth - damage);
                //Debug.Log("GUARD SHOT | PLAYER HEALTH | CHEST | Health: " + chestHealth);
            break;
            case ZoneType.Abdomen:
                abdomenHealth = Mathf.Max(0, abdomenHealth - damage);
                //Debug.Log("GUARD SHOT | PLAYER HEALTH | ABDOMEN | Health: " + abdomenHealth);
            break;
            case ZoneType.LeftLeg:
                leftLegHealth = Mathf.Max(0, leftLegHealth - damage);
                //Debug.Log("GUARD SHOT | PLAYER HEALTH | LEFT LEG | Health: " + leftLegHealth);
            break;
            case ZoneType.RightLeg:
                rightLegHealth = Mathf.Max(0, rightLegHealth - damage);
                //Debug.Log("GUARD SHOT | PLAYER HEALTH | RIGHT LEG | Health: " + rightLegHealth);
             break;

        }
        CheckZoneDepletion(zoneType);
        //CheckDeath();



        //Debug.Log("PLAYER HIT | Zone: " +
        //        zoneType +
        //        " | Damage: " +
        //        damage);

    }
    
    public void CheckZoneDepletion(ZoneType zoneType)
    {

        switch (zoneType)
        {
            case ZoneType.Head:
                if ((headHealth <= 0) && (!headDepleted))
                {
                    headDepleted = true;
                   
                    //Debug.Log("GUARD SHOT | PLAYER BODY STATE | HEAD DEPLETED");
                    CheckDeath();
                }
            break;
            case ZoneType.Chest:
                if ((chestHealth<=0f) && (!chestDepleted))
                {
                    chestDepleted = true;
                    
                    //Debug.Log("GUARD SHOT | PLAYER BODY STATE | CHEST DEPLETED");
                    CheckDeath();
                }
            break;
            case ZoneType.Abdomen:
                if ((abdomenHealth<=0f) && (!abdomenDepleted))
                {
                    abdomenDepleted = true;
                    
                    //Debug.Log("GUARD SHOT | PLAYER BODY STATE | ABDOMEN DEPLETED");
                    CheckDeath();
                }
            break;
            case ZoneType.LeftLeg:
                if ((leftLegHealth<=0f) && (!leftLegDepleted))
                {
                    leftLegDepleted = true;
                    
                    //Debug.Log("GUARD SHOT | PLAYER BODY STATE | left Leg DEPLETED");
                    CheckDeath();

                }
            break;
            case ZoneType.RightLeg:
                if ((rightLegHealth<=0f) && (!rightLegDepleted))
                {
                    rightLegDepleted = true;
                   
                    //Debug.Log(" GUARD SHOT | PLAYER BODY STATE | right Leg DEPLETED");
                    CheckDeath();
                }
            break;
                

        }
       
        //Debug.Log("PLAYER HIT | Zone: "+
        //            zoneType+
        //            " | Damage: "+
        //            damage);
    }
    private void CheckDeath()
    {

        if (isDead)
        {
            return;
        }
        if ((headHealth <= 0) ||
            (chestHealth <= 0) ||
            (abdomenHealth <= 0) ||
            (leftLegHealth <= 0) ||
            (rightLegHealth <= 0))
        {
            isDead = true;
            //Debug.Log("GUARD SHOT | PLAYER DIED!");
            if (spawnManager != null)
            {
                ResetHealth();
                //spawnManager.RespawnPlayerAtA();
                spawnManager.PlayerRandomRespawnReset();
            }
        }

    }
    private void ResetHealth()
    {

        //player.guardAI.currentState = GuardAI.GuardState.Patrol;
        //player.guardAI.Start();

        //player.guardVision.Start();
        //player.guardMovement.Start();

        headHealth = 100;
        chestHealth = 100;
        abdomenHealth = 100;
        leftLegHealth = 100;
        rightLegHealth = 100;


        headDepleted = false;
        chestDepleted = false;
        abdomenDepleted = false;
        leftLegDepleted = false;
        rightLegDepleted = false;
        //Debug.Log("ROUND PLAYER HEALTH RESET | NEW ROUND!");
        isDead = false;
        if (player != null)
        {
            //Debug.Log("ROUND GUARD HEALTH RESET | NEW ROUND!");
            //player.guardMovement.Die();
        }
        

    }
    [ContextMenu("TEST Head To Zero")]
    private void TestHeadToZero()
    {
        headHealth = 0;
        CheckZoneDepletion(ZoneType.Head);

    }
    [ContextMenu("Test Chest To Zero")]
    private void TestChestToZero()
    {
        chestHealth = 0;
        CheckZoneDepletion(ZoneType.Chest);

    }
    [ContextMenu("Test Abdomen To Zero")]
    private void TestAbdomenToZero()
    {
        abdomenHealth = 0;
        CheckZoneDepletion(ZoneType.Abdomen);
    }
    [ContextMenu("Test Left Leg To Zero")]
    private void TestLeftLegDamage()
    {
        leftLegHealth = 0;
        CheckZoneDepletion(ZoneType.LeftLeg);
    }
    [ContextMenu("Test Right Leg To Zero")]
    private void TestRightLegDamage()
    {
        rightLegHealth = 0;
        CheckZoneDepletion(ZoneType.RightLeg);
    }
    void Start()
    {
        spawnManager = FindObjectOfType<SpawnManager>();
        player = GetComponent<Player>();


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
