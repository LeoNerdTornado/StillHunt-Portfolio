using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform spawnA;
    public Transform spawnB;

    public GameObject playerPrefab;
    public GameObject guardPrefab;

    PlayerMovement movement;
    CharacterController cc;
    private SpawnArea[] spawnAreas;
    private PatrolPoint[] patrolPoints;
    private GuardVision guardVision;
    private GuardAI guardAI;
    private GuardMovement guardMovement;

    [SerializeField] private float minimumRespawnDistance = 50f;


    // Start is called before the first frame update
    void Start()
    {
        spawnAreas = FindObjectsOfType<SpawnArea>();
        patrolPoints = FindObjectsOfType<PatrolPoint>();
        DevLog.Log(
            "SPAWN MANAGER | PATROL POINTS FOUND: " +
            patrolPoints.Length
        );
        guardVision = guardPrefab.GetComponentInChildren<GuardVision>();
        guardMovement = guardPrefab.GetComponentInChildren<GuardMovement>();
        guardAI = guardPrefab.GetComponentInChildren<GuardAI>();

        if (guardVision != null)
        {
            DevLog.Log(
                "SPAWN MANAGER | GUARD VISION FOUND"
            );
        }
        else
        {
            DevLog.Log(
                "SPAWN MANAGER | GUARD VISION NOT FOUND"
            );
        }

        //Debug.Log("SPAWN MANAGER | SPAWN AREAS FOUND: " + spawnAreas.Length);
        //playerPrefab.transform.position = spawnA.position;

        //Player player = playerPrefab.GetComponent<Player>();
        //if (player != null)
        //{
        //    List<SpawnArea> validSpawnAreas = GetValidSpawnAreas(
        //        player.transform.position
        //        );
        //    //foreach(SpawnArea spawnArea in validSpawnAreas)
        //    //{
        //    //    GetSpawnTacticalScore(
        //    //                    spawnArea
        //    //                );
        //    //}
        //    SpawnArea bestGuardSpawn = GetBestGuardSpawnArea(validSpawnAreas);
        //    if (bestGuardSpawn != null)
        //    {
        //        Debug.Log(
        //            "SPAWN MANAGER | SELECTED GUARD SPAWN POSITION | " +
        //            bestGuardSpawn.name +
        //            " | Position: " +
        //            bestGuardSpawn.transform.position
        //        );
        //    }
        //}
        SpawnArea randomPlayerSpawn = GetRandomPlayerSpawnArea();

        if (randomPlayerSpawn != null)
        {
            Player player = playerPrefab.GetComponent<Player>();
            if (player != null)
            {
                player.RespawnAt(randomPlayerSpawn.transform.position);
               // Debug.Log(
               //    "SPAWN MANAGER | PLAYER STARTED RANDOMLY | " +
               //    randomPlayerSpawn.name +
               //    " | Position: " +
               //    randomPlayerSpawn.transform.position
               //);

                RespawnGuard();
            }
        }
    }
    private List<SpawnArea> GetValidSpawnAreas(Vector3 otherPlayerPosition)
    {
        List<SpawnArea> validSpawnAreas = new List<SpawnArea>();

        foreach (SpawnArea spawnArea in spawnAreas)
        {
            if (spawnArea == null) { continue; }
            float distance = Vector3.Distance(
                spawnArea.transform.position,
                otherPlayerPosition
                );
            if (distance < minimumRespawnDistance)
            {
           //     DebugDebug.Log(
           //    "SPAWN MANAGER | SPAWN AREA | REJECTED | " +
           //    spawnArea.name +
           //    " | Distance: " +
           //    distance
           //);

                continue;
            }
            validSpawnAreas.Add(spawnArea);

            DevLog.Log(
                "SPAWN MANAGER | SPAWN AREA | VALID | " +
                spawnArea.name +
                " | Distance: " +
                distance
            );
        }
        DevLog.Log(
        "SPAWN MANAGER | SPAWN AREA | VALID COUNT: " +
        validSpawnAreas.Count
    );

        return validSpawnAreas;
    }
    public void RespawnPlayerAtA()
    {
        //CharacterController cc = playerPrefab.GetComponent<CharacterController>();
        //cc.enabled = false;
        //playerPrefab.transform.position = spawnA.position;
        //cc.enabled = true;
        Player player = playerPrefab.GetComponent<Player>();
        
        player.RespawnAt(spawnA.position);
    }
    
    private PatrolPoint GetNearestPatrolPoint(SpawnArea spawnArea)
    {
        if (spawnArea == null) { return null; }

        PatrolPoint nearestPoint = null;
        float nearestDistance = Mathf.Infinity;

        foreach (PatrolPoint point in patrolPoints)
        {
            if (point == null) { continue; }

            float distance = Vector3.Distance(
                spawnArea.transform.position,
                point.transform.position
                );
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = point;
            }

        }
        if (nearestPoint != null)
        {
            DevLog.Log(
                "SPAWN MANAGER | SPAWN AREA | NEAREST PATROL POINT | " +
                spawnArea.name +
                " | " +
                nearestPoint.name +
                " | Distance: " +
                nearestDistance
            );
        }

        return nearestPoint;
    }
    private float GetSpawnTacticalScore(SpawnArea spawnArea)
    {
        if (spawnArea == null)
        {
            return float.MinValue;
        }

        if (guardVision == null)
        {
            return float.MinValue;
        }
        PatrolPoint nearestPoint = GetNearestPatrolPoint(spawnArea);
        if (nearestPoint == null) { return float.MinValue; }

        float tacticalScore = guardVision.GetTacticalScore(nearestPoint);
        DevLog.Log(
        "SPAWN MANAGER | SPAWN AREA | TACTICAL SCORE | " +
        spawnArea.name +
        " | PatrolPoint: " +
        nearestPoint.name +
        " | Score: " +
        tacticalScore
    );

        return tacticalScore;
    }
    private SpawnArea GetBestGuardSpawnArea(
        List<SpawnArea> validSpawnAreas
        )
    {
        if (
        validSpawnAreas == null ||
        validSpawnAreas.Count == 0
         )
        {
            DevLog.Log(
                "SPAWN MANAGER | NO VALID GUARD SPAWN AREAS"
            );

            return null;
        }
        SpawnArea bestSpawnArea = null;
        float bestScore = float.MinValue;

        foreach (SpawnArea spawnArea in validSpawnAreas)
        {
            float tacticalScore = GetSpawnTacticalScore(spawnArea);
            if (tacticalScore <= float.MinValue)
            {
                //Debug.Log(
                //    "SPAWN MANAGER | SKIP DANGEROUS SPAWN | " +
                //    spawnArea.name
                //);

                continue;
            }
            if (tacticalScore > bestScore)
            {
                bestScore = tacticalScore;
                bestSpawnArea = spawnArea;
            }
        }
        //if (bestSpawnArea != null)
        //{
        //    Debug.Log(
        //        "SPAWN MANAGER | BEST GUARD SPAWN | " +
        //        bestSpawnArea.name +
        //        " | SCORE: " +
        //        bestScore
        //    );
        //}
        //else
        //{
        //    Debug.Log(
        //        "SPAWN MANAGER | NO SAFE GUARD SPAWN FOUND"
        //    );
        //}

        return bestSpawnArea;
    }
    public void RespawnGuard()
    {
        Player player = playerPrefab.GetComponent<Player>();
        if (player == null)
        {
            DevLog.Log("SPAWN MANAGER | PLAYER NOT FOUND");
            return;
        }
        List<SpawnArea> validSpawnAreas =
            GetValidSpawnAreas(player.transform.position);

        SpawnArea bestGuardSpawn =
            GetBestGuardSpawnArea(validSpawnAreas);

        if (bestGuardSpawn == null)
        {
            DevLog.Log(
                "SPAWN MANAGER | GUARD RESPAWN FAILED | NO SPAWN"
            );

            return;
        }

        GuardAI guardAI =
        guardPrefab.GetComponent<GuardAI>();
        //if (!guardAI.gameObject.activeInHierarchy) { return; }
        if (guardAI == null)
        {
            DevLog.Log(
                "SPAWN MANAGER | GUARD AI NOT FOUND"
            );

            return;
        }
        guardAI.transform.position =
        bestGuardSpawn.transform.position;

        guardAI.ResetAI();
        guardPrefab.SetActive(true);

        DevLog.Log(
            "SPAWN MANAGER | GUARD RESPAWNED | " +
            bestGuardSpawn.name +
            " | Position: " +
            bestGuardSpawn.transform.position
        );
    }
    private SpawnArea GetRandomPlayerSpawnArea()
    {
        if (spawnAreas == null || spawnAreas.Length == 0)
        {
            DevLog.Log(
                "SPAWN MANAGER | NO SPAWN AREAS FOUND"
            );

            return null;
        }
        int randomIndex = Random.Range(0, spawnAreas.Length);

        SpawnArea randomSpawnArea = spawnAreas[randomIndex];
        DevLog.Log(
       "SPAWN MANAGER | RANDOM PLAYER START | " +
       randomSpawnArea.name +
       " | Position: " +
       randomSpawnArea.transform.position
   );

        return randomSpawnArea;
    }
    public void PlayerRandomRespawnReset()
    {
        Player player = playerPrefab.GetComponent<Player>();
        if (player == null)
        {
            DevLog.Log(
                "SPAWN MANAGER | PLAYER NOT FOUND"
            );

            return;
        }
        if (guardAI == null)
        {
            DevLog.Log(
                "SPAWN MANAGER | GUARD AI NOT FOUND"
            );

            return;
        }
        List<SpawnArea> validSpawnAreas =
            GetValidSpawnAreas(
                guardAI.transform.position
                );
                if (
                validSpawnAreas == null ||
                validSpawnAreas.Count == 0
            )
        {
            DevLog.Log(
                "SPAWN MANAGER | PLAYER RESPAWN FAILED | " +
                "NO VALID SPAWN AREAS"
            );

            return;
        }
        int randomIndex =
            Random.Range
            (
                0,
                validSpawnAreas.Count
                );
        SpawnArea randomSpawnArea =
            validSpawnAreas[randomIndex];
            
        player.RespawnAt(
        randomSpawnArea.transform.position
        );

        DevLog.Log(
            "SPAWN MANAGER | PLAYER RANDOM RESPAWN | " +
            randomSpawnArea.name +
            " | Distance From Guard: " +
            Vector3.Distance(
                randomSpawnArea.transform.position,
                guardAI.transform.position
            )
        );
        guardMovement.guardResetKnown();
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    Player player = playerPrefab.GetComponent<Player>();

        //    player.RespawnAt(spawnA.position);

        //    DevLog.Log("Alpha 1 spawnA: " + spawnA.position);
        //}
        //else if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    //Player player = playerPrefab.GetComponent<Player>();

        //    //player.RespawnAt(spawnB.position);

        //    //Debug.Log("Alpha 2 spawnB: " + spawnB.position);
        //    PlayerRandomRespawnReset();
        //}
        //else if (Input.GetKeyDown(KeyCode.Alpha3))
        //{
        //    RespawnGuard();
        //}
    }
}

