using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;

using static GuardAI;
using static UnityEngine.GraphicsBuffer;

public class GuardVision : MonoBehaviour
{
    

    public enum PredictionState { 
        Uncertain,
        Searching,
        Tracking,
        Ambushing
    
    }
    public enum TacticalVisibility
    {
        Dangerous,
        Advantageous,
        Hidden,
        Exposed
    }
    [System.Serializable]
    public class TacticalVisibilityRecord
    {
        public PatrolPoint patrolPoint;

        public bool guardSeesPlayer;
        public bool playerSeesGuard;

        public TacticalVisibility visibility;

        public float distanceFromLastKnownPosition;
    }
    public List<TacticalVisibilityRecord> tacticalVisibilityRecords = new List<TacticalVisibilityRecord>();

    public PredictionState predictionState = PredictionState.Uncertain;

    public PatrolPoint bestAmbushTile;
    public PatrolPoint failedAmbushTile = null;
    
    public List<PatrolPoint> predictionTiles = new List<PatrolPoint>();
    public List<PatrolPoint> blockedPredictionTiles = new List<PatrolPoint>();
    public List<PatrolPoint> safePredictionTiles = new List<PatrolPoint>();

    [Header("Strike Route Memory")]
    public List<PatrolPoint> lastStrikeRoute =new List<PatrolPoint>();
    public bool hasStrikeRouteRecord = false;


    public bool playerCanSeeGuard;
    public bool iFeelPlayer = false;
    public bool heardGunshot;
    public bool repeatedShots;
    public bool ambushPredictionChanged = false;


    [SerializeField] private LineRenderer visionLaser;

    public Transform playerTarget;
    public Transform playerEyeTarget;

    public float forgetTime = 3f;
    public float viewDistance = 30f;
    public float crouchViewDistance = 8f;
    public float hearingDistance = 20f;
    public float forgetTimer = 0f;
    public float locationCertainty = 0f;
    public float exposureCertainty = 0f;
    public float predictionCertainty = 0f;
    public float viewAngle = 90f;
    public float concealScore = 0f;
    public float pressureScore = 0f;
    public float baitScore = 0f;
    public float relocateScore = 0f;
    public float huntScore = 0f;
    public float playerDetectedTimer = 0f;
    public float currentViewDistance = 0;

    [Header("Player Aim Threat")]
    public float playerAimSuppressionLevel = 0f; 

    [SerializeField]
    private float locationDecayRate = 5f;
    [SerializeField]
    private float tacticalEvidenceRadius = 20f;

    public LayerMask obstacleMask;

    [SerializeField] public  LayerMask visibilityMask;

    [SerializeField] private LayerMask visionBlockerMask;

    [Header("Player Visibility Points")]
    public Transform playerHeadPoint;
    public Transform playerChestPoint;
    public Transform playerPelvisPoint;

    public Transform eyePoint;
    public Transform headPoint;
    public Transform chestPoint;
    public Transform bodyPoint;
    public Transform leftLeg;
    public Transform rightLeg;

    
    

    public bool playerDetected = false;
    public bool isObserving = false;
    public bool isInvestigating = false;
    public bool playerCanSeeMe = false;
    public bool lastSeenCertainty = false;
    public bool finalAmbushSetUp = false;
    public bool playerInsideVisionCone = false;
    public bool playerWasNoticed = false;
    public bool guardCanSeeMe = false;

    private bool isSearching = false;
    private bool rotationFinished = false;
    private bool tacticalHyphothesisRecorded = false;
    private bool isGuardVisionCone = false;
    public void ResetTacticalHypothesis()
    {
        tacticalHyphothesisRecorded = false;
        //Debug.Log("TACTICAL HYPOTHESIS | RESET | NEW LKP EVIDENCE");
    }
    
    public Vector3 lastKnownPosition;

    public Vector3 previousShotPosition;
    public Vector3 lastShotPosition;

    private Vector3 visionDirection;
    private Vector3 previousSeenPosition;

    public Queue<Vector3> shotHistory = new Queue<Vector3>();
    public int maxShotHistory = 6;

    

    [SerializeField]
    private float aimConfirmTime = 3f;
    private float aimTimer = 0f;

    [SerializeField]
    private float aimMemoryTime = 0.5f;
    private float aimMemoryTimer = 0f;
    private float ambushWaitTime = 3f;
    private float ambushTimer = 0f;

    [SerializeField]
    private float killZoneRadius = 3f;


    [SerializeField]
    private float aimSettleTime = 0.4f;
    private float aimSettleTimer = 0f;

    [SerializeField]
    private int predictionExpansionDepth = 2;

    Vector3 directionToPlayer;
    public Transform hunterAI;
    GuardCover guardCover;
    GuardMovement guardMovement;
    PlayerMovement playerMovement;
    GuardAI guardAI;
            float distanceToPlayer = 0;
    

    public PatrolPoint bestPredictionTile;

    private void RecordEvidence(
         InvestigationPoint.EvidenceType type,
         Vector3 position,
         float evidence

         )
    { }

    // Start is called before the first frame update
    public void Start()
    {
        playerMovement = playerTarget.GetComponent<PlayerMovement>();
        guardCover = gameObject.GetComponent<GuardCover>();
        guardAI = gameObject.GetComponent<GuardAI>();
        guardMovement = gameObject.GetComponent<GuardMovement>();
    }
   
    private float GetMaximumTravelDistance()
    {
        //Debug.Log("Maximum Prediction Distance :"+ guardMovement.timeSinceLastShot);
        return playerMovement.moveSpeeds * guardMovement.timeSinceLastShot;

    }
    private void VerifyPrediction() {

        //Debug.Log("Can Do A!");
        PatrolPoint actualTile = guardMovement.GetNearestPatrolPoint(playerTarget.position);
        if (bestPredictionTile == null) return;
        //Debug.Log("Can Do B!");
        if (actualTile == null) return;
        //Debug.Log("Can Do C!");
        if (actualTile == bestPredictionTile)
        {
            predictionCertainty += 15f ;
            DevLog.Log("pCertainty N Prediction Correct: "+ predictionCertainty+" actual tile: "+ actualTile.name);
        }
        else if (bestPredictionTile.connectedPoints.Contains(actualTile))
        {
            predictionCertainty += 5f ;
            DevLog.Log("pCertainty N  Nearby Prediction: "+ predictionCertainty + " actual tile: " + actualTile.name);

        }
        else {
            predictionCertainty -= 5f ;
            DevLog.Log("pCertainty N Wrong Prediction!"+ predictionCertainty + " actual tile: " + actualTile.name);
        
        }
            predictionCertainty = Mathf.Clamp(predictionCertainty, 0, 100);
        //Debug.Log("Actual Tile : "+actualTile.name);
        UpdatePredictionState();
    }
    public void UpdatePredictionState()
    {

        if (predictionCertainty < 30f)
        {
            predictionState = PredictionState.Uncertain;
        }
        else if (predictionCertainty < 60f)
        {
            predictionState = PredictionState.Searching;
        }
        else if (predictionCertainty < 80f)
        {
            predictionState = PredictionState.Tracking;
        }
        else {
            predictionState = PredictionState.Ambushing;
        }
        //Debug.Log("prediction State : "+ predictionState);
        
        if (predictionState == PredictionState.Ambushing)
        {
            
            //CODE NA LANG MAMAYA

            //guardAI.currentState = GuardAI.GuardState.AimFire;
            //playerDetected = false;
            
            //if (bestAmbushTile == null) return;
            //Debug.Log("pCertainty N AMBUSH SET UP ! bestAmbushTile: "+ bestAmbushTile.name);
            //bool arrived = guardMovement.MoveTo(bestAmbushTile.transform.position);
            //if (arrived)
            //{
               
            //    ambushTimer += Time.deltaTime;
            //    //guardMovement.RotationTo(lastKnownPosition);
            //    if(ambushTimer>= ambushWaitTime)
            //    {
            //        //finalAmbushSetUp = false;
            //        Debug.Log("pCertainty AMBUSH READY !");
            //        if (IsPlayerInsideKillZone())
            //        {
                        
            //            Debug.Log("pCertainty AMBUSH KILL ZONE !");
                        
            //            guardAI.currentState = GuardAI.GuardState.AimFire;
                        
            //        }
            //    }
            //}
        }
        //if(predictionState == PredictionState.Tracking)
        //{
        //    if (bestPredictionTile != null) {
        //        guardMovement.MoveToPredictionTile(bestPredictionTile);
        //    }

        //}

    }
    public void UpdateSafePredictionTiles()
    {
        safePredictionTiles.Clear();
        foreach (Transform pointTransform in guardMovement.patrolPoints)
        {
            PatrolPoint point = pointTransform.GetComponent<PatrolPoint>();
            if (point == null) continue;
            if (blockedPredictionTiles.Contains(point)) continue;
            safePredictionTiles.Add(point);


        }
        //Debug.Log("Safe Tiles : "+ safePredictionTiles.Count);

    }
    private void ExpandPredictionArea() {
        if (bestPredictionTile == null) return;
        
        Queue<PatrolPoint> open = new Queue<PatrolPoint>();
        HashSet<PatrolPoint> visited = new HashSet<PatrolPoint>(); 
        open.Enqueue(bestPredictionTile);
        visited.Add(bestPredictionTile);
        int depth = 0;
        while((open.Count > 0)&&(depth < predictionExpansionDepth))
        {
            int count = open.Count;
            for(int i=0; i<count; i++)
            {
                PatrolPoint current = open.Dequeue();
                foreach (PatrolPoint next in current.connectedPoints)
                {
                    if (visited.Contains(next)) continue;

                    visited.Add(next);
                    open.Enqueue(next);
                    blockedPredictionTiles.Add(next);
                }
                

            }
            depth++;

        }
        
    }
    public bool IsPlayerInsideKillZone() {
        
        if (bestAmbushTile == null) return false;

        float distance = Vector3.Distance(playerTarget.position, bestAmbushTile.transform.position);
        DevLog.Log("pCertaintyAMBUSH Distance: " + distance);

        return distance<killZoneRadius;
    } 
    public void ResetAmbush()
    {
        finalAmbushSetUp = false;
        bestAmbushTile = null;
        ambushTimer = 0f;

    }
    public void ClearBlockedPredictionTiles()
    {
        blockedPredictionTiles.Clear();
        if(bestPredictionTile!= null)
        {
            blockedPredictionTiles.Add(bestPredictionTile);
            //Debug.Log("Blocked :" + bestPredictionTile.name);

            foreach(PatrolPoint next in bestPredictionTile.connectedPoints)
            {
                if (!blockedPredictionTiles.Contains(next))
                {
                    blockedPredictionTiles.Add(next);

                }

            }
            foreach(PatrolPoint tile in blockedPredictionTiles)
            {
                //Debug.DrawLine(
                //    transform.position,
                //    tile.transform.position,
                //    Color.black
                //    );

            }
            DevLog.Log("pCertainty  pre blockedPredictionTiles: "+ blockedPredictionTiles.Count);
            ExpandPredictionArea();

            DevLog.Log("pCertainty post blockedPredictionTiles: " + blockedPredictionTiles.Count);
            ChooseBestAmbushTile();
            UpdateSafePredictionTiles();
        }
    }
    private void UpdateTerritoryPressure() {
        int safeCount = safePredictionTiles.Count;
        if (safeCount <= 2f) {
            //Debug.Log("uCertainty CHECK MATE POSITION!");
        }
        else if (safeCount > 8f)
        {
            //Debug.Log("uCertainty Player has many escape Routes!");
        }
        else if (safeCount > 4f)
        {
            //Debug.Log("uCertainty Player Movement restriction!");

        }
        else
        {
            //Debug.Log("uCertainty Player nearly trapped!");

        }
    }
    private void ChooseBestAmbushTile() {
        if (!finalAmbushSetUp)
        {
            if (blockedPredictionTiles.Count == 0) return;

            bestAmbushTile = blockedPredictionTiles[0];
            //Debug.Log("bCertainty pre BestAmbush Tile: " + bestAmbushTile.name);
            float bestScore = float.MinValue;
            foreach (PatrolPoint tile in blockedPredictionTiles)
            {
                if (tile == failedAmbushTile) {
                    DevLog.Log("pCertainty AMBUSH AMBUSH MEMORY :Skipping previously failed tile:" +tile.name);
                    continue; }
                float score = 0f;

                score -= Vector3.Distance(
                    transform.position,
                    tile.transform.position
                    );
                score += guardCover.GetCoverScores(tile.transform);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAmbushTile = tile;

                }

            }

            if (bestAmbushTile != null) { 

                finalAmbushSetUp = true;
                if (ambushPredictionChanged)
                {
                    DevLog.Log("pCertainty AMBUSH NEW PREDICTION: "+bestAmbushTile.name);
                    ambushPredictionChanged = false;
                }
            }
        }

        DevLog.Log("pCertainty N AMBUSH post BestAmbush Tile: "+ bestAmbushTile.name);
    }
    public void ClearPredictionTiles()
    {
        
        bestPredictionTile = null;
        
        if (lastKnownPosition == Vector3.zero) return;
        DevLog.Log("pCertainty AMBUSH START ");
        predictionTiles.Clear();
        PatrolPoint start = guardMovement.GetNearestPatrolPoint(lastKnownPosition); //LastKnownPosition
        if (start != null)
        {
                        predictionTiles.Add(start);
                        foreach (PatrolPoint next in start.connectedPoints)
                        {
                            predictionTiles.Add(next);
                        }
                        //Debug.Log("NearestPredictionTiles : " + start.name);

        
                    GetMaximumTravelDistance();
                    float maxTravelDistance = playerMovement.moveSpeeds * guardMovement.timeSinceLastShot;
                    List<PatrolPoint> validTiles = new List<PatrolPoint>();
                    float bestScores = -999f;
                    foreach (PatrolPoint tile in predictionTiles) {
                        float distance = Vector3.Distance(lastKnownPosition, tile.transform.position);
                        if (distance <= maxTravelDistance)
                        {
                            validTiles.Add(tile);

                        }
                    }
                    predictionTiles = validTiles;
                    foreach (PatrolPoint tile in predictionTiles)
                    {
                        Vector3 tileDirection = (tile.transform.position - lastKnownPosition).normalized;
                        float alignment = Vector3.Dot(guardAI.movementDirection.normalized, tileDirection);
                        //Debug.Log("Reachable Prediction : "+tile.name);
                        if (alignment > bestScores) {
                            bestScores = alignment;
                            bestPredictionTile = tile;
                        }
                    }
                    if (bestPredictionTile != null) {
                        DevLog.Log("pCertainty N AMBUSH Best Prediction Tile :" + bestPredictionTile.name);
                        ClearBlockedPredictionTiles();

                        
                    }
      }
    }
  
    public bool CanHearPlayer() { 
       return true;
    }
    
    public bool IsVisibleToPlayer()
    {
        Vector3 origin = playerEyeTarget.position;

        Transform[] visibilityPoint = 
            {
                headPoint,
                chestPoint,
                bodyPoint,
                leftLeg,
                rightLeg
            }; 
        foreach(Transform point in visibilityPoint)
        {
            if (point == null) continue;

            Vector3 direction = point.position - origin;
            float distance = direction.magnitude;

            if(distance <= 0.01f) { continue; }
            direction.Normalize();

            float angle = Vector3.Angle(
                playerEyeTarget.forward,
                direction
                );
            if (angle > 55f) {
                DevLog.Log("CAN DO visible PLAYER CANNOT SEE GUARD : ANGLE TO LARGE");
                continue; }
            //Debug.DrawRay(
            //    origin,
            //    direction * distance,
            //    Color.green
            //    );
            if (!Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                distance,
                visionBlockerMask
                )) 
            {
                DevLog.Log("CAN DO visible PLAYER CAN SEE GUARD GUARD THROUGH : "+point.name);
                return true;
            }
            DevLog.Log("CAN DO visible vision blocked by "+hit.transform.name+" when checking "+point.name);
            
        }
        return false;
        //Vector3 target = eyePoint.position;

        //Vector3 direction = target - origin;
        //float distance = direction.magnitude;
        //direction.Normalize();

        //float angle = Vector3.Angle(
        //    playerEyeTarget.forward,
        //    direction
        //    );
        
        //if(angle > 55f)
        //{

        //    Debug.Log("CAN DO visible PLAYER CANNOT SEE GUARD : ANGLE TO LARGE");
        //    return false;    
        //}
        //Debug.DrawRay(
        //    origin,
        //    direction * distance,
        //    Color.green
        //    );
        //if (Physics.Raycast(
        //    origin,
        //    direction,
        //    out RaycastHit hit,
        //    distance,
        //    visionBlockerMask
        //    )) 
        //{
        //    Debug.Log("CAN DO visible PLAYER VISION BLOCKED BY: "+hit.transform.name);
        //    return false;
        //}
        //   Debug.Log("CAN DO visible PLAYER CAN SEE GUARD!");
        //return true;
    }
    //bool IsVisibleToPlayer()
    //{
        
    //    Vector3 directionToGuard = (eyePoint.transform.position - playerEyeTarget.position).normalized;
    //    float angle = Vector3.Angle(playerEyeTarget.forward, directionToGuard);
    //    Debug.Log("Can Do PLayerCanSeeMe ANGLE : " + angle);
    //    if (angle > (55f)) { 
    //        //Debug.Log("Can Do visible return: " + angle); 
            
    //        return false; }
    //    //Vector3 directionToGuard = (transform.position - playerTarget.position).normalized;
    //    float distance = Vector3.Distance(playerEyeTarget.position, eyePoint.position);
    //    Ray ray = new Ray(
    //        playerEyeTarget.position,
    //        directionToGuard
    //        );
    //    Debug.DrawRay(
    //        playerEyeTarget.position,
    //        directionToGuard * distance,
    //        Color.green
    //        );

    //    RaycastHit hit;
    //    if (!Physics.Raycast(ray, out hit, distance, visibilityMask, QueryTriggerInteraction.Collide))
    //    {
    //        Debug.Log(" CAN DO PLAYER RAY HIT NOTHING!");
    //        return false;
    //    }
    //        //      if (Physics.Raycast(
    //        //    playerEyeTarget.position,
    //        //    directionToGuard,
    //        //    out hit,
    //        //    distance
    //        //))
    //        //      {
    //        //if (hit.transform == eyePoint) {
    //        //    return true;
    //        //}
    //        Debug.Log(" Can do player ray hit : "+hit.transform.name);
    //        GuardAI guard = hit.transform.GetComponentInParent<GuardAI>();
    //        if(guard!=null)
    //        {
    //            return true;
    //        }
    //        Debug.Log(" Can do player ray hit something else : " + hit.transform.name);
    //        return false;
    //        //if (hit.transform.IsChildOf(transform))
    //        //{

    //        //    return true;
    //        //}
    //        //if (hit.transform == transform)
    //        //{
    //        //    return true;
    //        //}

        
    //}
    public bool DetectRotationPattern(out Vector3 predictedFromPattern)
    {
        predictedFromPattern = Vector3.zero;

        if (shotHistory.Count < 3) {
            return false;
        }
        Vector3[] shots = shotHistory.ToArray();

        int n = shots.Length;

        Vector3 dirA = (shots[n - 1] - shots[n-2]).normalized;

        Vector3 dirB = (shots[n-2] - shots[n-3]).normalized;

        float angle = Vector3.Angle(dirA, dirB);

        //Debug.Log("PATTERN DETECTED MEMORY angle: " + angle);

        if (angle > 30f && angle < 150f) {
            predictedFromPattern = shots[n - 1] + (shots[n-1] - shots[n-2]);
            //Debug.Log("PATTERN DETECTED ! Predicted position: "+ predictedFromPattern);

            return true;
        }

        return false;
    }
    public void PlayerRepeatedShot(Vector3 position)
    {

        //guardAI.currentState = GuardState.Investigate;
        

       

        guardAI.previousShotPosition = guardAI.lastShotPosition;
        guardAI.lastShotPosition = position;

        if (Vector3.Distance(guardAI.lastShotPosition, guardAI.previousShotPosition) < 4f)
        {
            guardAI.repeatedShotCount++;
        }
        else
        {
            
            guardAI.repeatedShotCount = 1;
            
            predictionCertainty -= 5f;
            
        }
        if (guardAI.repeatedShotCount >= 2)
        {
            predictionCertainty += 20;
            
            if (predictionCertainty > 70) {
                huntScore += predictionCertainty;
            }

            predictionCertainty = Mathf.Clamp(
                predictionCertainty,
                0f,
                100f
                );
            DevLog.Log("iSuppression predictionCertainty:" + predictionCertainty);
            guardAI.currentIntent = GuardAI.HunterIntent.Pressure;

            guardAI.movementDirection = guardAI.lastShotPosition - guardAI.previousShotPosition;

            //guardAI.predictedPosition = guardAI.lastShotPosition + guardAI.movementDirection;
            //Debug.Log("prev prediction Certainty:"+ predictionCertainty + " lastShotPosition:" + guardAI.lastShotPosition);
            //Debug.Log("prev previousShotPosition:" + guardAI.previousShotPosition);
            //Debug.Log("prev movementDirection:" + guardAI.movementDirection);


            lastKnownPosition = guardAI.predictedPosition;
            //Debug.DrawLine(
            //    guardAI.lastShotPosition,
            //    guardAI.predictedPosition,
            //    Color.magenta,
            //    1f
            //    );
            guardAI.pressureShotsRemaining = Random.Range(2, 5);
        }
        else
        {
            lastKnownPosition = position;
        }
        
        shotHistory.Enqueue(position);
        while (shotHistory.Count > maxShotHistory)
        {
            shotHistory.Dequeue();
        }
        Vector3 patternPrediction;
        Vector3 shortTermPrediction;
        bool patternDetected = DetectRotationPattern(out patternPrediction);
        
        //Debug.Log("PATTERN DETECTED : " + patternDetected);
        //Debug.Log("PATTERN DETECTED Shot History Count:" + shotHistory.Count);

        if (patternDetected)
        {
            guardAI.predictedPosition = patternPrediction;
            predictionCertainty += 15f;
            predictionCertainty = Mathf.Clamp(
                predictionCertainty,
                0f,
                100f
                );
            //Debug.Log("PATTERN DETECTED ! Prediction: " +
            //          guardAI.predictedPosition +
            //          " Certainty: " +
            //          predictionCertainty
            //    );
        }
        else
        {
            shortTermPrediction = guardAI.lastShotPosition + guardAI.movementDirection;
            guardAI.predictedPosition = shortTermPrediction;
            predictionCertainty += 10f;
            //Debug.Log("PATTERN DETECTED SHORT TERM: "+ shortTermPrediction+" predictionCertainty: "+ predictionCertainty);

        }

        lastKnownPosition = guardAI.predictedPosition;
        DevLog.Log("For fire Last Known Position From PlayerRepeatedShot:"+ lastKnownPosition);
        //Debug.Log("prev Repeated Shots:" + guardAI.repeatedShotCount);
        if (lastKnownPosition != null)
        {
              lastSeenCertainty = true;
            guardMovement.isWaitingAtPatrolNode = false;
                //guardAI.currentState = GuardAI.GuardState.Observe;
        }
        //ClearPredictionTiles();
        UpdatePredictionState();


    }
   
    public void HearGunshot(Vector3 shotPosition)
    {
        if (shotPosition == Vector3.zero) { return; }

        previousShotPosition = lastShotPosition;
        lastShotPosition = shotPosition;


        if (Vector3.Distance(lastShotPosition, previousShotPosition) > 4f)
        {
            predictionCertainty -= 10f;
            predictionCertainty = Mathf.Clamp(
                predictionCertainty,
                0f,
                100f
                );
            heardGunshot = false;
            //Debug.Log("New Set into 1 prev prediction Certainty:" + predictionCertainty);
        }
        else { heardGunshot = true; }
        if (!heardGunshot)
        {
            predictionCertainty -= 8f * Time.deltaTime;
            predictionCertainty = Mathf.Clamp(
                predictionCertainty,
                0f,
                100f
                );
            //Debug.Log("PredictionCertainty is decreasing over time");
        }
        guardMovement.timeSinceLastShot = 0f;
        lastKnownPosition = shotPosition;
        ResetTacticalHypothesis();
        //Debug.Log("Last Known Position From Heard GunShot:"+ lastKnownPosition);
        playerDetected = false;
        //guardAI.currentCover = guardCover.ChooseBestCover(coverPoints);
        //`guardAI.currentCover = guardCover.ChooseBestCover(guardAI.coverPoints);
        //`guardAI.currentIntent = HunterIntent.Pressure;
        //`guardAI.currentState = GuardState.MoveToCover;
        //`guardAI.currentCover = guardAI.coverPoints[0];
        //guardAI.UpdateTacticalGoal();
        //Debug.Log("TACTICAL GOAL WINNER :" + guardMovement.currentTacticalGoal);
        if (lastKnownPosition != null) {
            //guardCover.ResetFailedPeeks();
            lastSeenCertainty = true;
            locationCertainty += 20f;
            exposureCertainty += locationCertainty * 0.2f;
            //guardAI.currentState = GuardAI.GuardState.Observe;
        }
        //ClearPredictionTiles();
        UpdatePredictionState();
        if (guardAI.currentCover == null)
        {
            return;
        }
    }
    public void guardCertaintyConfidence(bool isPlayerCanSeeGuard)
    {
        if (isPlayerCanSeeGuard)
        {
            //guardAI.currentIntent = HunterIntent.Conceal;
            return;
        }
        else {
            if (locationCertainty <= 30)
            {
                //guardAI.currentIntent = HunterIntent.Bait;
                return;
            }
            else if (locationCertainty <= 70)
            {
                //guardAI.currentIntent = HunterIntent.Relocate;
                return;
            } //locationCertainty = 80
            else if (locationCertainty <= 100) {
                if (exposureCertainty <= 30)
                { //exposureCertainty =25
                    if (predictionCertainty >= 70) {
                        //guardAI.currentIntent = HunterIntent.Hunt;
                        DevLog.Log("PredictionCertainty: HUNT");
                        return;
                    }
                    else
                    {
                        //guardAI.currentIntent = HunterIntent.Relocate;
                        DevLog.Log("PredictionCertainty: RELOCATE");
                        return;

                    }
                }
                else if (exposureCertainty <= 70) {
                    //guardAI.currentIntent = HunterIntent.Relocate;
                    return;
                }
                else if(exposureCertainty <= 100)
                {
                    //guardAI.currentIntent = HunterIntent.Pressure;
                    return;
                }
            }

        }
    }
    // Update is called once per frame
    private void OnPlayerSeen()
    {

        
        playerDetected = true;
        if ((guardMovement.currentState == GuardMovement.GuardState.AimFire)) { playerDetected = false; }
        //playerDetectedTimer += Time.deltaTime;


        isObserving = true;
        forgetTimer = 0f;
        isInvestigating = true;
        lastSeenCertainty = true;
        if (predictionCertainty <= 0)
        {

            //Debug.Log("CAN DO currentGuardState open prediction");
            //lastSeenCertainty = true;
            guardAI.repeatedShotCount = 1;
            previousSeenPosition = lastKnownPosition;
            lastKnownPosition = playerTarget.transform.position;
            ResetTacticalHypothesis();
            guardAI.movementDirection = lastKnownPosition - previousSeenPosition;
            predictionCertainty += 5f;
            guardMovement.currentState = GuardMovement.GuardState.Patrol;
            //Debug.Log("Can Do lastKnownPosition : " + lastKnownPosition);
            DevLog.Log("pCertainty N HIMALA "+ lastKnownPosition);
        }
        //Debug.Log("Seen Movement Direction :" + guardAI.movementDirection);
        
        
        VerifyPrediction();
    }
    private void OnPlayerHeard() {
        guardAI.lastKnownPosition = playerTarget.position;
        ResetTacticalHypothesis();
        //Debug.Log("Can Do lastKnownLocation OnPlayerHeard "+ lastKnownPosition);
        locationCertainty += 20f;
        exposureCertainty += locationCertainty * 0.2f;
        //guardCover.ResetFailedPeeks();
        lastSeenCertainty = true;
        //guardVision.isInvestigating = true;
        //guardAI.currentState = GuardState.Investigate;
        exposureCertainty = Mathf.Clamp(
            exposureCertainty, 0, 100
            );
        locationCertainty = Mathf.Clamp(
            locationCertainty, 0, 100
            );
        DevLog.Log("Noise is Heard");
    }
    public void visionLasers(float viewDistance) {
        if (visionLaser == null || eyePoint == null) return;
        //visionDirection = transform.forward;
        visionDirection = eyePoint.forward;
        //Debug.DrawLine(eyePoint.position, eyePoint.position + eyePoint.forward * 5f,
        //                        Color.blue
        //                        ); 
        float laserDistance = viewDistance;
            visionLaser.SetPosition(0, eyePoint.position);
            visionLaser.SetPosition(1, eyePoint.position + visionDirection * laserDistance);
    }
    private void OnGunShotHeard() {
        //HearGunshot();
    }
    private void updateLocationCertainty() {
        float hearingDistanceToPlayer = Vector3.Distance(
                transform.position,
                playerTarget.position
                );


        if (playerMovement.madeNoise && hearingDistanceToPlayer <= hearingDistance)
        {
            OnPlayerHeard();
        }
       
        if (lastSeenCertainty)
        {
            locationCertainty = 100f;
            //guardAI.currentState = GuardAI.GuardState.Observe;
            DevLog.Log("Observe2 lastSeenCertainty And Then Investigate: " + locationCertainty);
            
        }
        else
        {
            //float decayMultiplier = 1f + (100f - exposureCertainty) / 100f;
            //locationCertainty -= locationDecayRate *
            //                     decayMultiplier *
            //                     Time.deltaTime;
            //locationCertainty -= locationDecayRate * Time.deltaTime;
            locationCertainty -=  Time.deltaTime;
            locationCertainty = Mathf.Max(0f, locationCertainty);

            
        }
        if ((lastKnownPosition != Vector3.zero) && (locationCertainty <= 0))
        {
            //Debug.Log("Can Do lastKnownPosition lastKnownPosition");
            lastKnownPosition = Vector3.zero;
        }
        //Debug.Log("live locationCertainty: " + locationCertainty);

    }
    private void updateExposureCertainty() {
        if (lastSeenCertainty) {
            exposureCertainty += 20f;
            exposureCertainty = Mathf.Clamp(exposureCertainty, 0, 100);
        }
        else
        {
            exposureCertainty -= 5f;
            exposureCertainty = Mathf.Clamp(exposureCertainty, 0, 100);
        }
        //Debug.Log("live exposureCertainty: "+ exposureCertainty);
            
    }
    private bool IsAimedCentered()
    {

        float angle = Vector3.Angle(eyePoint.forward, directionToPlayer);

        return angle < 5f;
    }

    public bool CanGuardSeePlayer()
    {
        if ((playerTarget == null) || (eyePoint == null)) return false;

        // ---------------------------------------------------------
        // 1. Player visibility points
        // ---------------------------------------------------------

        Transform[] playerPoints = { 
        playerHeadPoint,
        playerChestPoint,
        playerPelvisPoint
        };
        string[] pointNames = 
        { 
        "HEAD",
        "CHEST",
        "PELVIS"
        };
        // ---------------------------------------------------------
        // 2. Check each point
        // ---------------------------------------------------------
        for (int i = 0; i < playerPoints.Length; i++)
        {
            Transform point = playerPoints[i];
            if (point == null) continue;

            Vector3 toPoint = point.position - eyePoint.position;
            float distance = toPoint.magnitude;
            

            Vector3 direction = toPoint.normalized;
            // -----------------------------------------------------
            // 3. Vision cone check
            // -----------------------------------------------------
            float angle = Vector3.Angle(
                eyePoint.forward,
                direction
                );
            
            // 60 degree total cone = 30 degrees each side
            if(angle>viewAngle * 0.5f)
            {
                //Debug.DrawRay(
                //    eyePoint.position,
                //    direction * distance,
                //    Color.gray,
                //    0.05f
                //    );
                isGuardVisionCone = false;
                continue;
            }
            isGuardVisionCone = true;


            if (distance > currentViewDistance) continue;
            // -----------------------------------------------------
            // 4. Draw the actual visibility ray
            // -----------------------------------------------------

            //Debug.DrawRay(
            //    eyePoint.position,
            //    direction * distance,
            //    Color.yellow,
            //    0.05f
            //    );
            // -----------------------------------------------------
            // 5. Raycast
            // -----------------------------------------------------
            RaycastHit hit;
            bool didHit = Physics.Raycast(
                eyePoint.position,
                direction,
                out hit,
                distance,
                visibilityMask
                );
            if (!didHit)
            {
                //Debug.DrawRay(
                //    eyePoint.position,
                //    direction * distance,
                //    Color.red,
                //    0.05f
                //    );
                continue;
            }
            // -----------------------------------------------------
            // 6. Did the ray hit the player?
            // -----------------------------------------------------
            
            Player targetPlayer = playerTarget.GetComponentInParent<Player>();
            Player hitPlayer = hit.collider.GetComponentInParent<Player>();
            PlayerHitZone hitBox = hit.collider.GetComponentInParent<PlayerHitZone>();
            if (((hitPlayer != null) && (hitPlayer == targetPlayer)) ||
                (hit.transform == playerTarget) ||
                (hit.transform.IsChildOf(playerTarget)))
            {
                //Debug.DrawRay(
                //    eyePoint.position,
                //    direction * hit.distance,
                //    Color.black,
                //    0.15f
                //    );
                DevLog.Log(
                    "CAN DO GUARD SEE PLAYER : "+pointNames[i]
                    +" | visible | HIT: "+hit.collider.name
                    );
                return true;
            }
            else
            {
                DevLog.Log(
                   "CAN DO GUARD SEE PLAYER : " 
                   + " | visible | HIT: " + hit.collider.name
                   );
            }

            //if((hit.transform == playerTarget) || (hit.transform.IsChildOf(playerTarget)))
            //{
            //    Debug.DrawRay(
            //        eyePoint.position ,
            //        direction * hit.distance,
            //        Color.black,
            //        0.15f
            //        );
            //    Debug.Log("CAN DO GUARD SEE PLAYER: "+
            //        pointNames[i]+
            //        " | VISIBLE | HIT: "+hit.collider.name);
            //    return true;
            //}

            // -----------------------------------------------------
            // 7. Something blocked this point
            // -----------------------------------------------------
            //Debug.DrawRay(
            //eyePoint.position,
            //direction * hit.distance,
            //Color.red,
            //0.15f
            //);
            DevLog.Log(
                "CAN DO GUARD SEE PLAYER : VISION BLOCKED: " +
                pointNames[i] +
                " | Hit: "+ hit.collider.name+
                " | Layer: "+LayerMask.LayerToName(hit.collider.gameObject.layer)
                );
        }
        return false;
        
    }
    public bool CanGuardSeePlayerFromPoint(PatrolPoint point)
    {
        if((point == null)||(playerTarget == null))
        {
            return false;
        }
        Vector3 origin = point.transform.position + Vector3.up * 1.5f;

        Transform[] playerPoints =
        {
            playerPelvisPoint,
            playerChestPoint,
            playerHeadPoint
        };
        string[] pointNames =
        {
            "PELVIS",
            "CHEST",
            "HEAD"
        };
        
        for(int i =0; i < playerPoints.Length;i++)
        {
            Transform playerPoint = playerPoints[i];

            if(playerPoint == null)
            {
                continue;
            }
            Vector3 toPlayer = playerPoint.position - origin;
            float distance = toPlayer.magnitude;

            //if(distance > viewDistance)
            //{
            //    Debug.Log("TACTICAL VISION | "
            //                +point.name+" | "
            //                + pointNames[i]+
            //                " | FAILED: TOO FAR | Distance: "+
            //                distance);
            //    continue;
            //}
            Vector3 direction = toPlayer.normalized;

            // TEMPORARY:
            // Still using current guard facing.

            Vector3 virtualForward = (playerTarget.position - point.transform.position);
            virtualForward.y = 0f;

            if(virtualForward.sqrMagnitude < 0.01)
            {
                continue;
            }
            virtualForward.Normalize();

            float angle = Vector3.Angle(
                virtualForward,
                direction
                );
            //if(angle > viewAngle * 0.5f)
            //{
            //    Debug.Log(
            //    "TACTICAL VISION | " +
            //    point.name +
            //    " | " +
            //    pointNames[i] +
            //    " | FAILED: OUTSIDE VISION CONE | Angle: " +
            //    angle
            //);

            //    continue;
            //}
            //Debug.DrawRay(
            //    origin,
            //    virtualForward * 5f,
            //    Color.blue,
            //    1f
            //    );

            RaycastHit hit;

            bool didHit = Physics.Raycast(
                origin,
                direction,
                out hit,
                distance,
                visibilityMask
                );
            if (!didHit)
            {
                DevLog.Log(
               "TACTICAL VISION | " +
               point.name +
               " | " +
               pointNames[i] +
               " | FAILED: RAY HIT NOTHING"
           );

                continue;
            }
            Player hitPlayer = hit.collider.GetComponentInParent<Player>();

            Player targetPlayer = playerTarget.GetComponentInParent<Player>();

            if(hitPlayer == playerTarget.GetComponentInParent<Player>())
            {
                //Debug.DrawRay(
                //    origin,
                //    direction * hit.distance,
                //    Color.green,
                //    1f
                //    );

                DevLog.Log("TACTICAL VISION | " + point.name
                            + " CAN SEE PLAYER");
                DevLog.Log(
               "TACTICAL VISION | " +
               point.name +
               " | " +
               pointNames[i] +
               " | PLAYER WINS | HIT: " +
               hit.collider.name
           );
                return true;
            }
            //     Debug.DrawRay(
            //    origin,
            //    direction * hit.distance,
            //    Color.red,
            //    1f
            //);

            DevLog.Log(
                "TACTICAL VISION | " +
                point.name +
                " | " +
                pointNames[i] +
                " | OBSTACLE WINS | " +
                "HIT: " + hit.collider.name +
                " | LAYER: " +
                LayerMask.LayerToName(hit.collider.gameObject.layer)
            );
        }

        DevLog.Log("TACTICAL VISION | "+point.name
                            +" CANNOT SEE PLAYER");
        return false;
    }
    public bool CanPlayerSeeGuardFromPoint(PatrolPoint Point)
    {
        if((Point == null) || (playerEyeTarget == null))
        {
            return false;
        }
        Vector3 origin = playerEyeTarget.position;
        Vector3 virtualGuardPosition = 
            Point.transform.position + Vector3.up * 1.5f;

        Vector3 toGuard = virtualGuardPosition - origin;

        float distance = toGuard.magnitude;

        if(distance < 0.01f)
        {
            return false;
        }
        Vector3 direction = toGuard.normalized;

        //Debug.DrawRay(
        //    origin,
        //    direction * distance,
        //    Color.blue,
        //    1f
        //    );
        RaycastHit hit;
        bool didhit = Physics.Raycast(
            origin,
            direction,
            out hit,
            distance,
            visionBlockerMask
            );
        if (!didhit)
        {
            DevLog.Log("TACTICAL VISION | PLAYER -> "+Point.name
                +" | GUARD POSITION VISIBLE ");
            return true;
        }
        //Debug.DrawRay(
        //    origin,
        //    direction * hit.distance,
        //    Color.red,
        //    1f
        //    );
        //Debug.DrawRay(
        //    origin,
        //    direction * hit.distance,
        //    Color.red,
        //    1f
        //    );
        DevLog.Log("TACTICAL VISION | PLAYER -> "+Point.name
                    +" | BLOCKED BY: "+hit.collider.name
                    +" | LAYER: "+LayerMask.LayerToName(hit.collider.gameObject.layer));

        return false;
    }
    public TacticalVisibility EvaluateTacticalVisibility(PatrolPoint point)
    {

        if(point == null)
        {
            return TacticalVisibility.Hidden;
        }
        if (playerCanSeeMe)
        {

        }
      
        bool guardSeesPlayer = CanGuardSeePlayerFromPoint(point);
        bool playerSeesGuard = CanPlayerSeeGuardFromPoint(point);
        if ((guardSeesPlayer) && (playerSeesGuard))
        {
            DevLog.Log("TACTICAL VISIBILITY | "+point.name+" | DANGEROUS");
            return TacticalVisibility.Dangerous;
        }
        if(guardSeesPlayer && (!playerSeesGuard))
        {
            DevLog.Log("TACTICAL VISIBILITY | " + point.name + " | ADVANTAGEOUS");
            return TacticalVisibility.Advantageous;
        }
        if ((!guardSeesPlayer) && (!playerSeesGuard))
        {
            DevLog.Log("TACTICAL VISIBILITY | " + point.name + " | HIDDEN");
            return TacticalVisibility.Hidden;
        }
        DevLog.Log("TACTICAL VISIBILITY | " + point.name + " | EXPOSED");
        return TacticalVisibility.Exposed;
    }
    public void RecordTacticalVisibility(PatrolPoint point)
    {
        if(point == null) { return; }

        bool guardSeesPlayer = CanGuardSeePlayerFromPoint(point);
        bool playerSeesGuard = CanPlayerSeeGuardFromPoint(point);

        TacticalVisibility visibility = EvaluateTacticalVisibility(point);

        TacticalVisibilityRecord record = new TacticalVisibilityRecord();

        record.patrolPoint = point;
        record.guardSeesPlayer = guardSeesPlayer;
        record.playerSeesGuard = playerSeesGuard;
        record.visibility = visibility;
        record.distanceFromLastKnownPosition =
            Vector3.Distance(lastKnownPosition, point.transform.position);
        //tacticalVisibilityRecords.Add(record);
        TacticalVisibilityRecord existingRecord =
            tacticalVisibilityRecords.Find(r => r.patrolPoint == point);
        if(existingRecord != null)
        {
            existingRecord.guardSeesPlayer = guardSeesPlayer;
            existingRecord.playerSeesGuard = playerSeesGuard;
            existingRecord.visibility = visibility;
            existingRecord.distanceFromLastKnownPosition =
            Vector3.Distance(lastKnownPosition, point.transform.position);
            DevLog.Log("TACTICAL RECORD UPDATED | " + point.name
               + " | " + visibility
               + " | DistanceFrom LKP: " + record.distanceFromLastKnownPosition);
        }
        else
        {
            tacticalVisibilityRecords.Add(record);
            DevLog.Log("TACTICAL RECORD ADDED | "+ point.name
                        +" | "+visibility);
        }
       
        
    }
    public void RecordEscapeRouteNode(PatrolPoint point)
    {
        if(point == null) { return; }
        RecordTacticalVisibility(point);
        //Debug.Log("ESCAPE EVIDENCE RECORDED | "+point.name);

    }
    public void RecordTacticalPossibilitiesFromEscapeNode(PatrolPoint escapeNode)
    {
        if(escapeNode == null)
        {
            DevLog.Log("TACTICAL POSSIBILITIES | ESCAPE NODE NULL");
            return;
        }
        if(lastKnownPosition == Vector3.zero)
        {
            DevLog.Log("TACTICAL POSSIBILITIES | ESCAPE NODE -> NO LAST KNOWN POSITION");
            return;
        }
        PatrolPoint centerPoint =
            guardMovement.GetNearestPatrolPoint(lastKnownPosition);
        if(centerPoint == null)
        {
            DevLog.Log("TACTICAL POSSIBILITIES | ESCAPE NODE -> NO LKP CENTER PATROL POINT");
            return;
        }
        DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES | ESCAPE NODE -> LKP CENTER | "
            + " ESCAPE: "+ escapeNode.name
            +" | CENTER: "+centerPoint.name);

        //List<PatrolPoint> route =
        //    guardMovement.BuildPatrolRoute(
        //        escapeNode,
        //        centerPoint
        //        );
        
        List<PatrolPoint> route;
       
        route = BuildTacticalPatrolPoint
            (
            escapeNode,
            centerPoint
            );
        
        if ((route == null) || (route.Count == 0))
        {
            DevLog.Log(
            "TACTICAL POSSIBILITIES | ESCAPE NODE -> LKP | NO ROUTE"
        );
            return;
        }
        List<PatrolPoint> tacticalRoute = OptimizeTacticalRoute(route); //KEY

        DevLog.Log(
        "TACTICAL ROUTE | OPTIMIZED ROUTE COUNT: " +
        tacticalRoute.Count
    );

        guardMovement.SetIndirectionInvestigationRoute(tacticalRoute);
        //guardMovement.SetIndirectionStrikeRoute(tacticalRoute);
        IsSameStrikeRoute(lastStrikeRoute);
        RememberStrikeRoute(tacticalRoute);
        //guardMovement.SetPostEscapeRoute(route);
        foreach (PatrolPoint point in tacticalRoute)
        {

            if(point == null)
            {
                continue;
            }
            RecordEscapeRouteNode(point);
        }
        DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES | ESCAPE NODE -> LKP ROUTE RECORDED | " +
            "NODES: "+route.Count);
        //RecordTacticalPossibilititiesFromPatrolGraph();
        
    }
    public void RecordTacticalPossibilitiesAroundLastKnownPosition()
    {
        if(lastKnownPosition == Vector3.zero)
        {
            DevLog.Log("TACTICAL EVIDENCE | NO LAST KNOWN POSITION ");
            return;
        }
        DevLog.Log("TACTICAL POSSIBILITIES YEAH");
        int recordedCount = 0;
        foreach (Transform pointTransform in guardMovement.patrolPoints)
        {
            if(pointTransform == null)
            {
                continue;
            }
            PatrolPoint point = pointTransform.GetComponent<PatrolPoint>();
            if(point == null) { continue; }
            float distance = Vector3.Distance(
                lastKnownPosition,
                point.transform.position
                );
            if(distance > tacticalEvidenceRadius)
            {
                continue;
            }
            RecordTacticalVisibility(point);
            recordedCount++;
        }
        DevLog.Log("TACTICAL POSSIBILITIES | TACTICAL HYPOTHESIS AREA RECORDED | " +
                " CENTER: "+lastKnownPosition
                +" | POINTS: "+recordedCount);
    }
    public void RecordTacticalPossibilititiesFromPatrolGraph()
    {
        if (tacticalHyphothesisRecorded)
        {
            return;
        }
       if(lastKnownPosition == Vector3.zero)
        {
            DevLog.Log("TACTICAL POSSIBILITIES | TACTICAL GRAPH | NO LAST KNOWN POSITION");
        }
        PatrolPoint centerPoint = guardMovement.GetNearestPatrolPoint(lastKnownPosition);
        if(centerPoint == null)
        {
            DevLog.Log("TACTICAL POSSIBILITIES | TACTICAL GRAPH | NO CENTER PATROL POINT");
            return;
        }
        Queue<PatrolPoint> open = new Queue<PatrolPoint>();

        HashSet<PatrolPoint> visited = new HashSet<PatrolPoint>();

        open.Enqueue(centerPoint);
        visited.Add(centerPoint);

        int recordedCount = 0;

        while (open.Count > 0)
        {
            PatrolPoint current = open.Dequeue();
            if(current == null)
            {
                continue;
            }
            float distance = Vector3.Distance(
                lastKnownPosition,
                current.transform.position
                );
            if(distance<= tacticalEvidenceRadius)
            {
                RecordTacticalVisibility(current);
                recordedCount++;
            }
            foreach (PatrolPoint next in current.connectedPoints)
            {
                 if(next == null) { continue; }
                if (visited.Contains(next)) { continue; }
                float nextDistance = Vector3.Distance(
                    lastKnownPosition,
                    next.transform.position); 
                if(nextDistance > tacticalEvidenceRadius)
                {
                    continue;
                }
                visited.Add(next);
                open.Enqueue(next);
            }
        }
        DevLog.Log(
       "ESCAPE ROUTE | TACTICAL POSSIBILITIES | TACTICAL GRAPH | HYPOTHESIS AREA LINKED | " +
       "CENTER: " + centerPoint.name +
       " | POINTS: " + recordedCount
   );
        tacticalHyphothesisRecorded = true;

    }
    public List<TacticalVisibilityRecord> GetTacticalPossibilities()
    {

        List<TacticalVisibilityRecord> possibilities = new List<TacticalVisibilityRecord>();

        foreach (TacticalVisibilityRecord record in tacticalVisibilityRecords)
        {
            if(record == null) { continue; }
            if(record.patrolPoint == null) { continue; }
            if(record.visibility == TacticalVisibility.Dangerous) { continue; }

            float currentDistance = Vector3.Distance(
                lastKnownPosition,
                record.patrolPoint.transform.position
                );
            if(currentDistance > tacticalEvidenceRadius) { continue; }
            possibilities.Add(record);
            
        }
        DevLog.Log("TACTICAL POSSIBILITIES | "+
            "TOTAL RECORDS: " + tacticalVisibilityRecords.Count
            +" | USABLE: "+ possibilities.Count);
        return possibilities;
    }
    public TacticalVisibilityRecord GetBestTacticalPossibility()
    {
        List<TacticalVisibilityRecord> possibilities = GetTacticalPossibilities();

        TacticalVisibilityRecord bestRecord = null;
        float bestScore = float.MinValue;

        foreach (TacticalVisibilityRecord record in possibilities)
        {
            float score = 0f;

            // Tactical advantage is the strongest factor.
            if(record.visibility == TacticalVisibility.Advantageous)
            {
                score += 100f;
            }
            else if(record.visibility == TacticalVisibility.Hidden)
            {
                score += 70f;
            }
            else if (record.visibility == TacticalVisibility.Exposed)
            {
                score += 20f;
            }
            // Slight preference for possibilities closer to the hypothesis center.
            score -= record.distanceFromLastKnownPosition;
            if(score > bestScore)
            {
                bestScore = score;
                bestRecord = record;
            }

        }
        if (bestRecord != null)
        {
            DevLog.Log("TACTICAL POSSIBILITIES | BEST TACTICAL POSSIBILITY: "+bestRecord.patrolPoint.name
                        +" | "+bestRecord.visibility
                        +" | SCORE: "+bestScore
                        +" | DISTANCE FROM LKP: "+bestRecord.distanceFromLastKnownPosition);
        }
        else
        {
            DevLog.Log("TACTICAL POSSIBILITIES | BEST TACTICAL POSSIBILITY | NONE");
        }
        return bestRecord;
    }
    public bool isInCover()
    {
        if ((guardMovement.currentState == GuardMovement.GuardState.MoveToCover)|| 
            (guardMovement.currentState == GuardMovement.GuardState.WaitInCover)||
            (guardMovement.currentState == GuardMovement.GuardState.LeanLeft)||
            (guardMovement.currentState == GuardMovement.GuardState.LeanRight)||
            (guardMovement.currentState == GuardMovement.GuardState.Aim)||
            (guardMovement.currentState == GuardMovement.GuardState.Fire)||
            (guardMovement.currentState == GuardMovement.GuardState.ReturnToCover))
        {
            return true;
        }

            return false;
    }
    private bool CanSeePlayer()
    {

        Transform[] visibilityPoints =
        {
            playerHeadPoint,
            playerChestPoint,
            playerPelvisPoint
        };
        foreach (Transform point in visibilityPoints)
        {
            if (point == null) continue;

            Vector3 direction = (point.position - eyePoint.position).normalized;

            float distance = Vector3.Distance(point.position, eyePoint.position);

            Ray ray = new Ray(
                eyePoint.position,
                direction
                );
            //Debug.DrawRay(
            //    eyePoint.position,
            //    point.position * distance,
            //    Color.black
            //    );
            RaycastHit hit;

            if(Physics.Raycast(
                ray,
                out hit,
                distance,
                visibilityMask
                )) 
            {
                //Debug.Log("CAN DO inside2 AI visibility ray hit: "+
                //    hit.collider.name+
                //    " | Target Point: "+
                //    point.name);
                // Did the ray hit any part of the player?
                if (hit.transform.root == playerTarget.root)
                {
                    //Debug.Log(
                    //    "CAN DO inside2 PLAYER DETECTED THROUGH: "+point.name+
                    //    " | HIT: "+hit.collider.name);
                    return true;
                }
                //Debug.Log("CAN DO inside2 PLAYER2 RAY BLOCKED BY: " + hit.collider.name);
            }
        }
        //Debug.Log("CAN DO inside2 PLAYER NOT VISIBLE FROM ANY VISIBILITY POINT");
        return false;
    }
    public Transform GetBestVisiblePlayerPoint()
    {
        Transform[] points =
        {
            playerPelvisPoint,
            playerChestPoint,
            playerHeadPoint
        };
        for(int i = 0; i < points.Length; i++)
        {
            Transform point = points[i];
            if (point == null) continue;

            Vector3 direction = point.position - eyePoint.position;

            float distance = direction.magnitude;

            if (distance > currentViewDistance) continue;
            direction.Normalize();

            float angle = Vector3.Angle(
                eyePoint.forward,
                direction);
            if (angle > viewAngle * 0.5f) continue;

            if (Physics.Raycast(
                eyePoint.position,
                direction,
                out RaycastHit hit,
                distance,
                visibilityMask
                )) 
            {
                Player hitPlayer =
                    hit.collider.GetComponentInParent<Player>();

                Player targetPlayer =
                    playerTarget.GetComponentInParent<Player>();

                PlayerHitZone hitZone =
                    hit.collider.GetComponentInParent<PlayerHitZone>();

                if ((hitPlayer!=null && hitPlayer == targetPlayer)||
                    (hitZone!=null))
                {
                    return point;
                }

            }
        }
        return null;
    }
    private List<PatrolPoint> OptimizeTacticalRoute(List<PatrolPoint> route)
    {
        

        if ((route==null) )
        {
            return new List<PatrolPoint>();
        }
        if((route.Count < 3))
        {
            return new List<PatrolPoint>(route);
        }
        List<PatrolPoint> optimizeRoute = new List<PatrolPoint>(route);
        for (int i = 1; i < route.Count - 1;i++)
        {
            PatrolPoint previous = optimizeRoute[i - 1];
            PatrolPoint current = optimizeRoute[i];
            PatrolPoint next = optimizeRoute[i + 1];

            PatrolPoint betterPoint =
                GetBetterTacticalRoutePoint(
                    previous,
                    current,
                    next
                    );
            DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES | CURRENT INV PATROL ROUTE: "+current.name);
            if ((betterPoint!=null) && (betterPoint != current))
            {
                DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES | CURRENT INV TACTICAL ROUTE | REPLACED: "
                            + current.name
                            +" -> "+betterPoint.name);
                optimizeRoute[i] = betterPoint;

            }
        }
        return optimizeRoute;
    }
    private PatrolPoint GetBetterTacticalRoutePoint(
        PatrolPoint previous,
        PatrolPoint current,
        PatrolPoint next
        )
    {
        if (current == null) { return null; }
        PatrolPoint bestPoint = current;
        float bestScore = GetTacticalScore(current); 
        foreach (PatrolPoint candidate in current.connectedPoints)
        {
            if (candidate == null) { continue; }
            // Candidate must still connect the route.
            if((previous!=null)&&
                (!previous.connectedPoints.Contains(candidate))) {
                //Debug.Log("TACTICAL ROUTE CHECK | " +
                //    "Current: " +current.name
                //            +" of Candidate: "+candidate.name
                //            +" is not connected to Previous: "+previous.name);
                continue;
             }
            // Candidate must connect forward.
            if ((next != null) && 
                (!candidate.connectedPoints.Contains(next)))
            {
                //Debug.Log("TACTICAL ROUTE CHECK | "+
                //    "Current: " + current.name
                //            + " of Candidate: " + candidate.name
                //            + " is not connected to next: " + next.name);
                continue;
            }
            float candidateScore = GetTacticalScore(candidate);
        //    Debug.Log(
        //    "TACTICAL ROUTE CHECK | " +
        //    current.name +
        //    " -> " +
        //    candidate.name +
        //    " | SCORE: " +
        //    candidateScore
        //);
            if(candidateScore > bestScore)
            {
                bestScore = candidateScore;
                bestPoint = candidate;
            }
        }
        return bestPoint;

    }
    public float GetTacticalScore(PatrolPoint point)
    {
        if(point == null)
        {
            return float.MinValue;
        }
        TacticalVisibility visibility = EvaluateTacticalVisibility(point);
        if(visibility == TacticalVisibility.Dangerous)
        {
            return float.MinValue;
        }
        float score = 0f;
        if(visibility == TacticalVisibility.Advantageous)
        {
            score += 100f;
        }
        else if(visibility == TacticalVisibility.Hidden)
        {
            score += 70f;
        }
        else if (visibility == TacticalVisibility.Exposed)
        {
            score += 20f;
        }
        float distance = Vector3.Distance(
            lastKnownPosition,
            point.transform.position
            );
        score -= distance;
        return score;
    }
    public PatrolPoint GetBestTacticalPatrolPoint
        (List<PatrolPoint> possiblePoints)
    {
        if((possiblePoints == null)||
            (possiblePoints.Count == 0))
        {
            return null;
        }
        PatrolPoint bestPoint = null;
        float bestScore = float.MinValue;
       
        foreach(PatrolPoint point in possiblePoints)
        {
            if(point == null)
            {
                continue;
            }
            float score = GetTacticalScore(point);
           // Debug.Log(
           //"PATROL INSTINCT | " +
           //point.name +
           //" | TACTICAL SCORE: " +
           //score
           // );
            // -----------------------------------------
            // RECENT TACTICAL MEMORY
            // -----------------------------------------
            if (guardMovement.IsRecentTacticalPatrolNode(point))
            {
                //Debug.Log(
                //"PATROL INSTINCT | " +
                //point.name +
                //" | RECENTLY USED | SKIPPING"
                // );
                continue;
            }
            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = point;
            }
            
        }
        //if (bestPoint != null)
        //{
        //    Debug.Log(
        //        "PATROL INSTINCT | BEST POINT: " +
        //        bestPoint.name +
        //        " | SCORE: " +
        //        bestScore
        //    );
        //}
        //else
        //{
        //    Debug.Log(
        //        "PATROL INSTINCT | " +
        //        "NO NON-RECENT TACTICAL POINT"
        //    );
        //}
        return bestPoint;
    }
   
    private List<PatrolPoint> BuildTacticalPatrolPoint
        (
        PatrolPoint start,
        PatrolPoint target
        )
    {
        
        List<PatrolPoint> result =
            new List<PatrolPoint>();
        if ((start == null) ||
            (target==null))
        {
            return result;
        }
        Dictionary<PatrolPoint, float> scores = new Dictionary<PatrolPoint, float>();
        Dictionary<PatrolPoint, PatrolPoint> cameFrom = new Dictionary<PatrolPoint, PatrolPoint>();

        List<PatrolPoint> open = new List<PatrolPoint>();

        HashSet<PatrolPoint> visited = new HashSet<PatrolPoint>();

        open.Add(start);

        scores[start] = 0f;
        cameFrom[start] = null;

        while(open.Count > 0)
        {
            // -------------------------------------------------
            // FIND THE BEST CURRENT PATROL POINT
            // -------------------------------------------------
            PatrolPoint current = open[0];

            float bestCurrentScore = scores[current];

            for(int i=1;i< open.Count; i++)
            {
                PatrolPoint candidate = open[i];
                if (scores[candidate] > bestCurrentScore)
                {
                    bestCurrentScore = scores[candidate];
                    current = candidate;
                }
            }
            open.Remove(current);

            if (visited.Contains(current))
            {
                continue;
            }
            visited.Add(current);

            // -------------------------------------------------
            // TARGET REACHED
            // -------------------------------------------------
            if (current == target)
            {
                break;
            }
            // -------------------------------------------------
            // EXPLORE CONNECTED PATROL POINTS
            // -------------------------------------------------
            foreach (PatrolPoint next in current.connectedPoints)
            {
                if(next == null) { continue; }
                if (visited.Contains(next)) { continue; }

                float tacticalScore = GetTacticalScore(next);

                // ---------------------------------------------
                // DANGEROUS POINT
                // ---------------------------------------------
                float newScore = scores[current] + tacticalScore;


                // ---------------------------------------------
                // FIRST TIME DISCOVERED
                // ---------------------------------------------
                if (!scores.ContainsKey(next))
                {
                    scores[next] = newScore;
                    cameFrom[next] = current;
                    open.Add(next);
                }
                // ---------------------------------------------
                // BETTER TACTICAL PATH FOUND
                // ---------------------------------------------
                else if(newScore > scores[next])
                {
                    scores[next] = newScore;
                    cameFrom[next] = current;
                }

            }
        }
        // -----------------------------------------------------
        // TARGET COULD NOT BE REACHED
        // -----------------------------------------------------
        if (!cameFrom.ContainsKey(target))
        {
            DevLog.Log("TACTICAL ROUTE | "+
                        "NO SAFE TACTICAL ROUTE FOUND");
            return result;
        }
        // -----------------------------------------------------
        // RECONSTRUCT ROUTE
        // -----------------------------------------------------
        PatrolPoint routeNode = target;

        while (routeNode!=null)
        {
            result.Add(routeNode);
            routeNode = cameFrom[routeNode];

        }
        result.Reverse();
        DevLog.Log("TACTICAL ROUTE | "+
                    "ROUTE FOUND | "+
                    " NODES: "+result.Count);
        foreach(PatrolPoint point in result)
        {
            if(point == null) { continue; }

            DevLog.Log("TACTICAL ROUTE NODE | "+point.name
                        +"| SCORE: " + GetTacticalScore(point));

        }
        return result;
    }
    public float EvaluatePlayerAimThreat( Vector3 muzzleOrigin,Vector3 muzzleDirection)
    {
        if(guardAI == null) 
        {
            playerAimSuppressionLevel = 0f;
            return 0f;
        }
        float aimThreat =
            guardAI.CalculateBulletSuppression
            ( muzzleOrigin,
                muzzleDirection
                );
        playerAimSuppressionLevel = aimThreat;

        
        return playerAimSuppressionLevel;
    }
    public void RememberStrikeRoute(List<PatrolPoint> route)
    {
        if ((route == null) ||
            (route.Count == 0))
        {
            return;
        }
        lastStrikeRoute.Clear();
        foreach (PatrolPoint point in route)
        {
            if (point == null) { continue; }
            lastStrikeRoute.Add(point);
        }
        if (lastStrikeRoute.Count > 0)
        {
            hasStrikeRouteRecord = true;
            DevLog.Log("STRIKE MEMORY | ROUTE RECORDED | NODES: " + lastStrikeRoute.Count);
            foreach (PatrolPoint point in lastStrikeRoute)
            {
                DevLog.Log(
              "STRIKE MEMORY | NODE: " +
              point.name
          );
            }
        }
    }
    public List<PatrolPoint> CalculateStrikeRoute()
    {
        List<PatrolPoint> result = new List<PatrolPoint>();
        if ((guardMovement == null) ||
        (lastKnownPosition == Vector3.zero))
        {
            DevLog.Log(
                "STRIKE ROUTE | CANNOT CALCULATE | " +
                "GUARD MOVEMENT OR LKP NULL"
            );

            return result;
        }
        PatrolPoint start =
            guardMovement.GetNearestPatrolPoint(transform.position);
        PatrolPoint target =
            guardMovement.GetNearestPatrolPoint(lastKnownPosition);

        if ((start == null) || (target == null))
        {
            DevLog.Log(
                "STRIKE MEMORY | STRIKE ROUTE | CANNOT CALCULATE | " +
                "START OR TARGET NULL"
            );

            return result;
        }
        DevLog.Log(
           "STRIKE MEMORY | STRIKE ROUTE | CALCULATING | " +
           "START: " + start.name +
           " | TARGET: " + target.name
            );
        List<PatrolPoint> route =
            BuildTacticalPatrolPoint
            (
                start,
                target
                );
            if ((route == null) ||
           (route.Count == 0))
            {
            DevLog.Log(
                    "STRIKE MEMORY | STRIKE ROUTE | NO ROUTE FOUND"
                );

            return result;
            }
        result = OptimizeTacticalRoute(route);
        DevLog.Log(
            "STRIKE MEMORY | STRIKE ROUTE | CALCULATED | " +
            "NODES: " + result.Count
            );
        foreach(PatrolPoint point in result)
        {
            if(point == null) { continue; }
            DevLog.Log(
           "STRIKE MEMORY | STRIKE ROUTE | NODE: " +
           point.name
            );
        }
        return result;
    }
    public bool TestStrikeRouteMemory()
    {
        if((guardMovement == null)||(lastKnownPosition==Vector3.zero)) 
        {
            DevLog.Log("STRIKE MEMORY TEST | GuardMovement: "+ guardMovement
                +" | lastKnownPosition: "+ lastKnownPosition);
            return false;
        }
        PatrolPoint start = guardMovement.GetNearestPatrolPoint(transform.position);
        PatrolPoint target = guardMovement.GetNearestPatrolPoint(lastKnownPosition);
        
        if ((start == null) ||
            (target == null))
        {
            DevLog.Log(
           "STRIKE MEMORY TEST | " +
           "START OR TARGET NULL"
       );

            return false;
        }

        DevLog.Log(
        "STRIKE MEMORY TEST | " +
        "START: " + start.name +
        " | TARGET: " + target.name
    );
        // -------------------------------------------------
        // FIRST STRIKE ROUTE
        // -------------------------------------------------
        if (!hasStrikeRouteRecord)
        {

            DevLog.Log(
            "STRIKE MEMORY TEST | " +
            "NO PREVIOUS STRIKE ROUTE | RECORDING FIRST ROUTE"
        );

            RecordTacticalPossibilitiesFromEscapeNode(start);
            return false ;
        }
        
        // -------------------------------------------------
        // NEW STRIKE ROUTE
        // -------------------------------------------------

        List<PatrolPoint> route =
            BuildTacticalPatrolPoint
            (start,
            target
                );

        if ((route == null) || (route.Count == 0))
        {
            DevLog.Log(
            "STRIKE MEMORY TEST | " +
            "NO NEW STRIKE ROUTE"
        );

            return false;
        }
        List<PatrolPoint> tacticalRoute =
        OptimizeTacticalRoute(route);
        if ((tacticalRoute == null) ||
        (tacticalRoute.Count == 0))
        {
            DevLog.Log(
                "STRIKE MEMORY TEST | " +
                "NO OPTIMIZED STRIKE ROUTE"
            );

            return false;
        }
        DevLog.Log(
              "STRIKE MEMORY TEST | " +
              "NEW ROUTE CALCULATED | NODES: " +
              tacticalRoute.Count
          );
        bool sameRoute =
            IsSameStrikeRoute(tacticalRoute);

        DevLog.Log(
            "STRIKE MEMORY TEST | " +
            "SAME ROUTE: " + sameRoute
        );
        return sameRoute;
    }
    public bool IsSameStrikeRoute(List<PatrolPoint> newRoute)
    {
        if (!hasStrikeRouteRecord)
        {
            DevLog.Log(
            "STRIKE MEMORY | NO PREVIOUS ROUTE"
        );

            return false;
        }
        if ((newRoute == null) || (newRoute.Count == 0) ||(lastKnownPosition == Vector3.zero))
        {
            DevLog.Log(
                "STRIKE MEMORY | NEW ROUTE NULL OR EMPTY"
            );

            return false;
        }
        if (newRoute.Count != lastStrikeRoute.Count)
        {
            DevLog.Log(
                "STRIKE MEMORY | ROUTE DIFFERENT | " +
                "NODE COUNT CHANGED | OLD: " +
                lastStrikeRoute.Count +
                " | NEW: " +
                newRoute.Count
            );

            return false;
        }

        for (int i=0; i< newRoute.Count;i++)
        {
            if (newRoute[i] != lastStrikeRoute[i])
            {
                DevLog.Log(
               "STRIKE MEMORY | ROUTE DIFFERENT | " +
               "NODE INDEX: " + i +
               " | OLD: " + lastStrikeRoute[i].name +
               " | NEW: " + newRoute[i].name
                );

                return false;
            }

        }
        DevLog.Log(
       "STRIKE MEMORY | ROUTE MATCH | " +
       "SAME TACTICAL ROUTE"
   );
        return true;
    }
    public bool isInterrupt()
    {
        if ((guardMovement.currentTacticalGoal != GuardMovement.TacticalGoal.Patrol) && (lastKnownPosition != Vector3.zero))
        {

            if((guardMovement.currentState == GuardMovement.GuardState.Investigate) 
                &&(guardMovement.currentTacticalGoal == GuardMovement.TacticalGoal.HuntPlayer)
                ) { return false; }
            if ((guardMovement.currentState == GuardMovement.GuardState.EscapeRoute)
                && (guardMovement.currentTacticalGoal == GuardMovement.TacticalGoal.EscapeRoute)
                ) { return false; }

            guardMovement.observeTacticalGoal = guardMovement.currentTacticalGoal;
            DevLog.Log("Observe2 currentTacticalGoal PASOK new tactical goal: " + guardMovement.observeTacticalGoal);
            //observerTimer = 0f;
            guardAI.observeTimer = 0f;

            //GuardVision.TacticalVisibilityRecord bestPossibility =
            //    guardVision.GetBestTacticalPossibility();

            //if(bestPossibility!= null)
            //{
            //    Debug.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES | TACTICAL DECISION | BEST POSSIBILITY: "+bestPossibility.patrolPoint.name
            //                +" | VISIBILITY: "+bestPossibility.visibility
            //                +" | DISTANCE FROM LKP: "+bestPossibility.distanceFromLastKnownPosition);
            //}
            //else
            //{

            //}
            guardMovement.currentState = GuardMovement.GuardState.Observe;
            //guardVision.RecordTacticalPossibilitiesAroundLastKnownPosition();
            //guardVision.RecordTacticalPossibilititiesFromPatrolGraph();
            //guardVision.GetTacticalPossibilities();
            //guardVision.GetBestTacticalPossibility();
            return true;
        }
        return false;
    }
    
    void Update()
    {
        //TestStrikeRouteMemory();







        visionLasers(viewDistance);
        ClearPredictionTiles();
        DevLog.Log("CurrentGuardState :" + guardMovement.currentState + " playerDetected : " + playerDetected);
        //guardCover.GetCoverOcclusionScore(guardAI.coverPoints[6]);
        //Debug.Log("GuardAI Distance:" +distance + " <= viewDistance:"+viewDistance);

        distanceToPlayer = Vector3.Distance(playerTarget.position, eyePoint.position);
        directionToPlayer = (playerTarget.position - eyePoint.position).normalized;

        Vector3 flatDirectionToPlayer = Vector3.ProjectOnPlane(directionToPlayer, Vector3.up).normalized;
        Vector3 flatGuardForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;


        //Debug.Log("ANGLE CENTERED : "+IsAimedCentered());
        float angleToPlayer = Vector3.Angle(flatGuardForward, flatDirectionToPlayer);

        playerInsideVisionCone = false;
        if (angleToPlayer <= (viewAngle * 0.5f))
        {
            
            playerInsideVisionCone = true;
            //Debug.Log("30 degree angle is on!");
        }

        //Debug.Log("playerInsideVisionCone : " + playerInsideVisionCone + " viewAngle: " + angleToPlayer +
        //    "EYE FORWARD: " + eyePoint.forward + " DirectionToPlayer: " + directionToPlayer);



        
        Ray ray = new Ray(
                      eyePoint.position,
                      directionToPlayer
            );
        //Debug.DrawLine(
        //eyePoint.position,
        //eyePoint.position + directionToPlayer * distanceToPlayer,
        //Color.black
        //);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, distanceToPlayer))
        {
     //       Debug.Log(
     //    " Ray Hit: " +
     //    hit.collider.name +
     //    " | Tag: " +
     //    hit.collider.tag
     //);
        }
        currentViewDistance = viewDistance;
        //if (playerMovement.isCrouching)
        //{
        //    currentViewDistance = crouchViewDistance;

        //}
        //if (playerMovement.isInGrass)
        //{
        //    currentViewDistance *= 0.5f;
        //    Debug.Log("The crouch view distance while in grass is " + currentViewDistance + " < 8f");
        //}
        //Debug.Log("EyePoint2 guard forward  : " +transform.forward);
        //Debug.Log("EyePoint2 Eye Forward : " + eyePoint.forward);
        updateLocationCertainty();
        updateExposureCertainty();
        playerCanSeeMe = IsVisibleToPlayer();
        guardCanSeeMe = CanGuardSeePlayer();

        //Debug.Log("Can Do lastKnownPosition OUTER : " + lastKnownPosition);
        //Debug.Log("isGuardVisionCone : " + isGuardVisionCone);
        if ((!isInCover()) &&
            (isGuardVisionCone)&&
            (playerCanSeeMe)&&
            (guardMovement.currentTacticalGoal == GuardMovement.TacticalGoal.GainBetterPosition)
            &&(distanceToPlayer < (currentViewDistance/2))
            )
        {
            //guardMovement.currentState = GuardMovement.GuardState.Patrol;
            DevLog.Log("Observe2 currentTacticalGoal guardVision currentTacticalGoal: "+ guardMovement.observeTacticalGoal);
            if(guardMovement.currentState == GuardMovement.GuardState.Observe) { return; }
            guardMovement.currentState = GuardMovement.GuardState.Patrol;
            playerDetected = false;
            lastKnownPosition = playerTarget.position;
            locationCertainty = 100f;
            exposureCertainty = 100f;
            return;
        }


        DevLog.Log("  CAN DO visible2 GUARD SEE PLAYER ? " + guardCanSeeMe);
        DevLog.Log("CAN DO visible2 DoPlayerCanSeeMe? " + playerCanSeeMe);


        if ((guardCanSeeMe)&&(!playerCanSeeMe))
        {
            //guardMovement.RotationTo(playerTarget.transform.position);
            //guardMovement.LookVertical(playerTarget.transform.position);
            DevLog.Log("CAN DO OYEAH");
            //playerDetected = true;
            //guardMovement.RotationTo(playerTarget.position);
            //guardMovement.LookVertical(playerTarget.position);
            guardMovement.RotationTo(playerTarget.position);
            guardMovement.LookVertical(playerTarget.position);
            DevLog.Log("Observe2 Decision guardCanSeeMe true and playerCanSeeMeFalse");
            OnPlayerSeen();
            
           // if ((guardMovement.currentState == GuardMovement.GuardState.AimFire) ||
           //(guardMovement.currentState == GuardMovement.GuardState.Patrol) ||
           //(guardMovement.currentState == GuardMovement.GuardState.Observe) ||
           //(guardMovement.currentState == GuardMovement.GuardState.Investigate)) { playerDetected = false; }
            return;
        }
        //if ((playerCanSeeMe)&&(guardCanSeeMe))
        if((playerCanSeeMe)&&(guardCanSeeMe))
        {

            DevLog.Log("Observe2 Decision playerInsideVisionCone playerCanSeeMeNow "+ playerInsideVisionCone+
                " distanceToPlayer: "+ distanceToPlayer+" <= currentViewDistance "+ currentViewDistance);

            playerDetected = false;

            //Debug.Log("playerCanSeeMe Best Choose Current Cover:" + currentCover);
            //`guardAI.currentIntent = HunterIntent.Conceal;

            //guardAI.currentIntent = HunterIntent.Relocate;
            guardAI.concealMentTimer += Time.deltaTime;
            //guardAI.currentCover = guardCover.ChooseBestCover(guardAI.coverPoints);
            //guardAI.currentState = GuardState.MoveToCover;
            
            //Debug.Log("CANDO CURRENTVIEWDISTANCE : " + distanceToPlayer+" visionCone: "+ playerInsideVisionCone);
            if (distanceToPlayer <= currentViewDistance)
            {

                //Debug.Log("CAN DO INSIDE CURRENTVIEW DISTANCE distance:  visionCone:" + playerInsideVisionCone);
                //Debug.Log("CAN DO INSIDE CURRENTVIEW DISTANCE distance: "+ distanceToPlayer+" currentViewDistance: "+ currentViewDistance+" visionCone:" +playerInsideVisionCone);

                if (playerInsideVisionCone )
                {
                    //Debug.Log("Can DO INSIDE PlayerInsideVision ");
                    //Debug.Log("Can Do Observe2 Turn to player");
                    playerWasNoticed = true;

                    guardMovement.RotationTo(playerTarget.position);
                    guardMovement.LookVertical(playerTarget.position);
                    //OnPlayerSeen();
                    //OnPlayerSeen();
                    // Check whether the guard is now sufficiently aimed.
                    //float aimAngle = Vector3.Angle(
                    //    eyePoint.forward,
                    //    directionToPlayer
                    //);

                    //Debug.Log(
                    //    "CAN DO INSIDE AIM ANGLE AFTER ROTATION: " +
                    //    aimAngle
                    //);

                    //if (aimAngle <= 5f)
                    //{
                    //    Debug.Log("CAN DO INSIDE GUARD AIM IS CENTERED");

                    //    OnPlayerSeen();

                    //    Debug.Log(
                    //        "CAN DO INSIDE PLAYER DETECTED - GUARD IS AIMED"
                    //    );
                    //}
                    //float bodyAngle = Vector3.Angle(transform.forward, directionToPlayer);


                    //rotationFinished = bodyAngle < 8f;
                    //Debug.Log("Body Angle :" + bodyAngle+" rotationFinished: "+ rotationFinished);
                    //if (rotationFinished)
                    //{


                    //    if (aimSettleTimer >= aimSettleTime)
                    //    {
                    //        aimTimer += Time.deltaTime;

                    //    }
                    //    aimSettleTimer += Time.deltaTime;
                    //}
                    //else {
                    //    aimMemoryTimer += Time.deltaTime;
                    //    if (aimMemoryTimer >= aimMemoryTime) {
                    //        aimTimer = 0f;
                    //        aimMemoryTimer = 0f;
                    //    }
                    //}


                }


               
                    if (CanSeePlayer())
                    {
                        //Debug.Log("CAN DO INSIDE Player Detected");
                        //Debug.Log("CAN DO Observe2 playerInsideVisionCone Ray Hit:" + hit.collider.name);
                        //if (aimTimer >= aimConfirmTime)
                        //{
                        OnPlayerSeen();
                        //    Debug.Log("aimTimer :PASOK");
                        //OnPlayerSeen();

                        //guardMovement.FireShot();
                        //aimTimer = 0f;
                        //guardAI.currentState = GuardAI.GuardState.Observe;
                        //    Debug.Log("aimTimer : " + aimTimer +
                        //       "aimMemoryTimer: " + aimMemoryTimer);
                        //}

                        //Debug.Log(" CURRENT TACTICAL GOAL : Record the lastKnownPosition :" + lastKnownPosition);
                        //lastSeenCertainty = true;
                        //guardCover.ResetFailedPeeks();
                       
                    }
                   
                    


           

            }
            else
            {
                
                forgetTimer += Time.deltaTime;
               
                if (forgetTimer >= forgetTime)
                {
                     
                    forgetTimer = 0f;
                    DevLog.Log("Observe2 CURRENT TACTICAL GOAL : PLAYER DETECTED FALSE lastKnownPosition");
                    playerDetected = false;
                    isObserving = false;
                    //observeTimer = 0f;
                    //fireTimer = 0f;

                }
                //if (lastSeenCertainty)
                //{
                    //guardAI.currentState = GuardAI.GuardState.Observe;
                    lastSeenCertainty = false;
                //}
            }
                //Debug.Log("For fire playerInsideVisionCone : EARLY RETURN!");
            if (guardAI.currentCover == null) return;
            
           
        }
        else
        {
            if ((guardMovement.currentState == GuardMovement.GuardState.AimFire)) { playerDetected = false; }
            lastSeenCertainty = false;
            //guardAI.currentState = GuardAI.GuardState.Observe;

            guardAI.concealMentTimer = 0f;
        }
        
        

        }
    }
