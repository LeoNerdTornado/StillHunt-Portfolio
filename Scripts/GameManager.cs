using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    CharacterController cc;
    IEnumerator StartNextRound()
    {
        
        yield return new WaitForSeconds(3f);
        currentGuard.gameObject.SetActive(true);
        currentGuard.transform.position = guardSpawnPoint.position;
        //currentGuard.health = 200;
        currentGuard.ResetAI();
        //Debug.Log("ROUND GUARD AI RESPAWN!");
        //cc = currentGuard.GetComponent<CharacterController>();
        //cc.enabled = false;


        //cc.enabled = true;



    }
    public Transform guardSpawnPoint;
    public GuardAI guardPrefab;
    public GuardAI currentGuard;
    void Start()
    {
        //Debug.Log("Still Hunt Started");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GuardDefeated() {
        //Debug.Log("Round Won!");
        StartCoroutine(StartNextRound());
        //Debug.Log("ROUND Guard AI Respawn!");
    }
    
    

}
