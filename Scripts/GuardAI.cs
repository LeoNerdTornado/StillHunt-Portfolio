using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
//using UnityEditor.Experimental.GraphView;

//using UnityEditor.Overlays;

//using UnityEditor.XR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
//using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class GuardAI : MonoBehaviour
{

    // Start is called before the first frame update

    public Animator guardAnimator;
    public Transform guardAIRifle;



    PlayerMovement playerMovement;
    GameManager gameManager;
    GuardMovement guardMovement;
    GuardAI guardAI;
    GuardVision guardVision;
   
    GuardCover guardCover;
    
    GuardCover.PeekDirection fakeSide;

    public enum HunterIntent { 
        Conceal,
        Pressure,
        Bait,
        Relocate,
        Hunt,
        Strike
    }





    public HunterIntent currentIntent;
   

    
    public enum CoverDecision { 
        Fight,
        Fake,
        Wait,
        Relocate,
        Strike
    
    }

    public string coverReturnDecision = "";

    
    public GuardCover.PeekDirection sidePeek = GuardCover.PeekDirection.Left;

    
    
    

    private CapsuleCollider bodyCapsule;
    public Transform[] coverPoints;

    public Transform headPoint;
    public Transform chestPoint;
    public Transform legPoint;
    public Transform currentCover;
    public Transform playerTarget;
    public Transform player;
           Transform targetPoint;
           Quaternion targetRotation;
    

    [Header("Patrol Navigation Matrix")]
    private Transform currentNavigationPoint;
    private Transform navigationTargetPoint;

    private int currentPoint = 0;

    

    public SpawnManager spawnManager;
    public int health = 100;
    [Header("Guard Personality!")]

    [Range(0f, 100f)]
    public float aggression = 50f;

    [Range(0f, 100f)]
    public float patience = 50f;

    [Range(0f, 100f)]
    public float deception = 50f;

    [Range(0f, 100f)]
    public float caution = 50f;


   
    private bool NextSearchRoute = false;

    [Header("Suppression Episode Memory")]
    public int highSuppressionEpisode = 0;

    public bool needsBetterPositionAfterSuppression = false;
    public bool needsEscapeRouteAfterSuppression = false;

    public bool suppressionEpisodeActive = false;


    public bool isInShadow = false;
    

    [SerializeField] public float suppressionEpisodeStartThreshold = 150f;
    [SerializeField] public float suppressionEpisodeRecoveryThreshold = 50f;
    [SerializeField] public float baseSuppressionEpisodeRecoveryThreshold = 50f;
    [SerializeField] public float hitRecoveryThresholdIncrease = 15f;
    [SerializeField] public float hitRecoveryThresholdRecovery = 10;

    
    public float baitDelay = 5f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 4f;
    public float fireDelay = 0.02f;
    public float patrolSpeed = 1.5f;
    public float observeTime = 1.0f;
    public float rotationSpeed = 5f;
    public float aimTime = 1.5f;
    public float searchTime = 3f;
    public float maxSuppression = 200f;
    public float suppressionRecovery = 15f;
    public float concealMentTimer = 0f;
    public float certaintyGainSight = 1;
    public float certaintyGainSound = 1;
    public float certaintyDecay = 1;
    public float surviveGoalScore = 0f;
    public float suppressionAimTimer = 1f;
    public float suppressionAimTime = 0f;
    public float suppressionAimLevel = 35f;
    public float resetBatch = 0f;
    public float observeTimer = 0f;

    public float suspiciousGrassLookDuration = 2f;
    public float suppressionLevel = 0f;

    [Header("Tactical Thinking ")]
    public float tacticalThinkInterval = 0.5f;
    public float tacticalThinkTimer = 0f;

    [SerializeField]
    private float strikeScore = 0f;
    [SerializeField]
    private float strikeSmallAddition = 10f;

    [SerializeField]
    private float strikeBigAddition = 50f;

    private float randomWaitTime;
    
    
    private float exposureMemoryTimer = 0f;
    private float silentTimer = 0f;

    
    private bool isFakePeekLeft = false;
    private bool isFakePeekRight = false;
    private bool isMovingToCover = false;
    private bool isInCover = false;
    private bool isWaitingInCover = false;
    private bool isLeaningLeft = false;
    private bool isLeaningRight = false;
    private bool isReturningToCover = false;
    private bool isFiringFromCover = false;
    private bool playerCanSeeMe;

    private float observeWaitTimer = 0f;
    private float fireShotGrassTimer = 0f;
    private float forceMoveGoalScore = 0f;
    private float pinPlayerGoalScore = 0f;
    private float betterPositionGoalScore = 0f;
    private float huntPlayerGoalScore = 0f;
    private float gainBetterPositionGoalScore = 0f;
   

    
    float waitTime = 0f;

    private float searchTimer = 0f;
    
    private float aimTimer = 0f;
    private float fireTimer = 0f;

    private Transform lastPatrolCheckPoint;

    
    private List<Transform> patrolRoute = new List<Transform>();
    
    

    
            
            
    

    Player plaYer;
    public Vector3 lastShotPosition;
    public Vector3 previousShotPosition;
    public Vector3 movementDirection;
    public Vector3 predictedPosition;
    


    Vector3 distance ;
           Vector3 direction ;
           Vector3 lookDirection;
    


    public Vector3 lastKnownPosition;
    


    


    
    
    public int repeatedShotCount;
    public int pressureShotsRemaining;
    public int failedPeeks = 0;

    private int scanIndex = 0;

    private Vector2[] scanPattern;

   
    

 
    
    
    
    
   
   
   
    
    
    
    
    //public TacticalGoal EvaluateTacticalGoal() {
    //    surviveGoalScore = 0f; // suppression Level >100
    //    forceMoveGoalScore = 0f; //If Suppressed attack when player is move to attack; Else move to hide or run, AI run
    //    pinPlayerGoalScore = 0f; //If Suppressed is low like < 50 but see player attack
       
    //    huntPlayerGoalScore = 0f; //If suppressed is low hunt according to predictionCertainty

    //    betterPositionGoalScore = 0f;
    //    gainBetterPositionGoalScore = 0; // Suppressed when the player is on tactical position
    //    surviveGoalScore += suppressionLevel;
    //    Debug.Log("Tactical Goal A - Survive Score: suppression level " + surviveGoalScore);

    //    if (guardVision.playerCanSeeMe) {
    //        surviveGoalScore += 30f;
    //        surviveGoalScore += suppressionLevel * 0.5f; // --
    //        Debug.Log("Tactical Goal A - Survive Score: playerCanSeeMe " + surviveGoalScore);
    //    }
    //    surviveGoalScore += guardCover.failedPeeks * 15f;
    //    surviveGoalScore = Mathf.Clamp(
    //                        surviveGoalScore,
    //                        0f,
    //                        150f
    //                        );

    //    Debug.Log("Tactical Goal A - Survive Score:"+ surviveGoalScore);
    //    forceMoveGoalScore += repeatedShotCount * 15f;
    //    forceMoveGoalScore += guardVision.predictionCertainty * 0.5f;
    //    forceMoveGoalScore += aggression * 0.2f;
    //    forceMoveGoalScore -= suppressionLevel * 0.9f; //0.3

    //    if (guardVision.playerCanSeeMe) {
    //        forceMoveGoalScore -= 20f;
    //    }
    //    Debug.Log("Tactical Goal A - Force Move Score: suppression Level " + forceMoveGoalScore);
    //    forceMoveGoalScore = Mathf.Clamp(
    //                        forceMoveGoalScore,
    //                        0f,
    //                        150f
    //                         );
    //    Debug.Log("Tactical Goal A - Force Move Score: "+forceMoveGoalScore);

    //    pinPlayerGoalScore += guardVision.predictionCertainty * 0.6f;
    //    pinPlayerGoalScore += repeatedShotCount * 10f;
    //    pinPlayerGoalScore += aggression * 0.15f;
    //    pinPlayerGoalScore -= suppressionLevel * 0.4f;
    //    pinPlayerGoalScore = Mathf.Clamp(
    //                            pinPlayerGoalScore,
    //                            0f,
    //                            150f
    //                            );
    //    float A = 0;
    //    Debug.Log("Tactical Goal - Pin Player Score: "+ pinPlayerGoalScore);

    //    if (guardCover.AreBothSidesDangerous(suppressionLevel)) {
    //        A = 50;
    //        gainBetterPositionGoalScore += 50f;
    //    }
    //        gainBetterPositionGoalScore += guardCover.failedPeeks * 15f;
    //        gainBetterPositionGoalScore += suppressionLevel * 0.3f;
    //        gainBetterPositionGoalScore += aggression * 0.1f;
    //        gainBetterPositionGoalScore = Mathf.Clamp(
    //                                        gainBetterPositionGoalScore,
    //                                        0f,
    //                                        150f
    //                                        );
    //         Debug.Log("Tactical Goal - Gain Better Position Score:"+ gainBetterPositionGoalScore+" from "+
    //                    "AreBothSidesDangerous :"+ A+
    //                    "guardCover.failedPeeks : "+ guardCover.failedPeeks+
    //                    "suppressionLevel : "+ suppressionLevel+
    //                    "AGGRESSION : "+ aggression);
    //    //huntPlayerGoalScore += (guardVision.locationCertainty * 0.7f) + (guardVision.predictionCertainty * 0.3f);
    //        huntPlayerGoalScore += guardVision.predictionCertainty * 0.6f;
    //        huntPlayerGoalScore += repeatedShotCount * 10f;
    //         huntPlayerGoalScore += aggression * 0.2f;
                
    //            if ((!guardVision.playerDetected) && (guardVision.predictionCertainty >= 70f))
    //            {
    //                huntPlayerGoalScore += 30f;
    //            }
    //         huntPlayerGoalScore -= suppressionLevel * 0.4f;
                
    //         huntPlayerGoalScore = Mathf.Clamp(
    //                                 huntPlayerGoalScore,
    //                                 0f,
    //                                 150f
    //                                );
    //        Debug.Log("Tactical Goal - Hunt Player Score: "+huntPlayerGoalScore);
    //        TacticalGoal bestGoal = TacticalGoal.Survive;
    //        float bestScore = surviveGoalScore;
    //        if (forceMoveGoalScore > bestScore) {
    //            bestScore = forceMoveGoalScore;
    //            bestGoal = TacticalGoal.ForcePlayerToMove;
    //        }
    //        if (pinPlayerGoalScore > bestScore) {
    //            bestScore = pinPlayerGoalScore;
    //            bestGoal = TacticalGoal.PinPlayer;
    //        }
    //        if (gainBetterPositionGoalScore > bestScore) {
    //            bestScore = gainBetterPositionGoalScore;
    //            bestGoal = TacticalGoal.GainBetterPosition;
    //        }   
    //        if (huntPlayerGoalScore > bestScore) {
    //            bestScore = huntPlayerGoalScore;
    //            bestGoal = TacticalGoal.HuntPlayer;
    //            guardVision.playerDetected = false;
    //        }
    //        //Debug.Log("TACTICAL GOAL WINNER | "+
    //        //          $"Survive:{surviveGoalScore} | "+
    //        //          $"ForceMove:{forceMoveGoalScore} | "+
    //        //          $"Pin:{pinPlayerGoalScore} | "+
    //        //          $"BetterPosition:{gainBetterPositionGoalScore} | "+
    //        //          $"Hunt:{huntPlayerGoalScore}  | "+
    //        //          $"WINNER:{bestGoal} ({bestScore}) | ");
    //    return bestGoal;
        
    //}
    
   
    
   

        public void Start()
        {
            bodyCapsule = GetComponent<CapsuleCollider>();
            
            
            plaYer = player.GetComponent<Player>();
            playerMovement = plaYer.GetComponent<PlayerMovement>();
            gameManager = spawnManager.GetComponent<GameManager>();
            guardMovement = GetComponent<GuardMovement>();
            guardVision = gameObject.GetComponent<GuardVision>();
            guardMovement.currentState = GuardMovement.GuardState.Patrol;
            guardMovement.currentTacticalGoal = GuardMovement.TacticalGoal.Patrol;
            guardCover = gameObject.GetComponent<GuardCover>();
            guardCover.coverPosition = Vector3.zero;
            FindAllCoverPoints();
        //guardAnimator = GetComponentInChildren<Animator>();
        //Debug.Log("New GuardMovement currentTacticalGoal : " + guardMovement.currentTacticalGoal);
        if (guardVision == null)
            {
                //Debug.Log("NOT FOUND");
            }
            else
            {
                //Debug.Log("FOUND");
            }
            guardMovement.BuildPatrolMatrix();
            //currentPatrolNode = GetNearestPatrolNode(transform.position);
            //if (currentPatrolNode != null)
            //{
            //    Debug.Log("PATROL NODE GUARD STATE NODE:" + currentPatrolNode.name);
            //}
            //nextPatrolNode = GetNextPatrolPoint(currentPatrolNode);
            //if (nextPatrolNode != null)
            //{
            //    Debug.Log("PATROL NODE Guard Random Next Node:" + nextPatrolNode.name);

            //}

        }
    
    public void TestPersonalityDecision() {
        CoverDecision decision = EvaluateCurrentCover();
        DevLog.Log(
        "PERSONALITY TEST COVER SCORES | " +
        $"Decision: {decision} | " +
        $"Aggression:{aggression} " +
        $"Patience:{patience} " +
        $"Deception:{deception} " +
        $"Caution:{caution}"
              );
        

    }
    public float GetSuppressionLevel() {
        return suppressionLevel;
    }
    Vector3 GetHeadPosition()
    {
        Vector3 localHead = bodyCapsule.center + Vector3.up * (bodyCapsule.height * 0.33f);
        return transform.TransformPoint(localHead);

    }
    Vector3 GetChestPosition()
    {
        return transform.TransformPoint(bodyCapsule.center);

    }
    Vector3 GetLegPosition()
    {
        Vector3 localLeg = bodyCapsule.center - Vector3.up * (bodyCapsule.height * 0.35f);
        return transform.TransformPoint(localLeg);
    }
    public float CalculateBulletSuppression(Vector3 bulletOrigin, Vector3 bulletDirection) {

        float headDistance = DistanceToBulletLine(GetHeadPosition(), bulletOrigin, bulletDirection);
        float chestDistance = DistanceToBulletLine(GetChestPosition(), bulletOrigin, bulletDirection);
        float legDistance = DistanceToBulletLine(GetLegPosition(), bulletOrigin, bulletDirection);
        float addSuppression = 1.20f;
        //Debug.Log("headDistance: "+ headDistance+" chestDistance: "+ chestDistance+" legDistance: "+ legDistance);
        float headSuppression = 0f;
        if (headDistance < (0.15f+ addSuppression)) { headSuppression = 150f; }
        else if (headDistance < (0.40f+ addSuppression)) { headSuppression = 100f; }
        else if (headDistance < (0.80f+ addSuppression)) { headSuppression = 50f; }

        float chestSuppression = 0f;
        if(chestDistance < (0.20f+ addSuppression)) { chestSuppression = 100f; }
        else if (chestDistance < (0.50f+ addSuppression)) { chestSuppression = 50f; }
        else if (chestDistance < (1f+ addSuppression)) { chestSuppression = 25f; }

        float legSuppression = 0f;
        if (legDistance < (0.20f+ addSuppression)) { legSuppression = 50f; }
        else if (legDistance < (0.50f+ addSuppression)) { legSuppression = 25f; }
        else { legSuppression = 0f; }
        float suppression = Mathf.Max(headSuppression, chestSuppression, legSuppression);
        //Debug.Log("Show headSuppression : " + headSuppression + 
        //            " chestSuppression: " + chestSuppression + 
        //            " legSuppression: " + legSuppression +
        //            "suppression: "+ suppression);
        

        return suppression;
    
    }
    private float DistanceToBulletLine(Vector3 point, Vector3 bulletOrigin, Vector3 bulletDirection) {
        bulletDirection.Normalize();
        Vector3 toPoint = point - bulletOrigin;
        float projection = Vector3.Dot(toPoint, bulletDirection);
        Vector3 closestPoint = bulletOrigin + bulletDirection * projection;
        float distance = Vector3.Distance(point, closestPoint);
        //Debug.DrawLine(
        //    bulletOrigin,
        //    toPoint,
        //    Color.red,
        //    10f
        //    );
        return distance;
    }
    public float CalculateSuppression(Vector3 bulletPoint) {
        float headDistance = Vector3.Distance(bulletPoint, headPoint.position);
        float chestDistance = Vector3.Distance(bulletPoint, chestPoint.position);
        float legDistance = Vector3.Distance(bulletPoint,legPoint.position);
        float headSuppression = 0f;
        if (headDistance < 0.15f) { headSuppression = 150f; }
        else if (headDistance < 0.4f) { headSuppression = 100f; }
        else if (headDistance < 0.8f) { headSuppression = 50f; }
        //Debug.Log("Show headSuppression: "+ headSuppression);
        float chestSuppression = 0f;
        if (chestSuppression < 0.2f) { chestSuppression += 100f;  }
        else if (chestSuppression < 0.5f) { chestSuppression += 50f; }
        else if (chestSuppression < 1f) { chestSuppression += 25f; }
        //Debug.Log("Show chestSuppression: " + chestSuppression);
        float legSuppression = 0f;

        if (legDistance < 0.2f) { legSuppression += 50f; }
        else if (legDistance < 0.5f) { legSuppression += 25f; }
        else { legSuppression = 0; }
        //Debug.Log("Show legSuppression: " + legSuppression);
        float suppression = Mathf.Max(headSuppression, chestSuppression, legSuppression);

        return suppression;
    }
    public void AddSuppression(float amount)
    {
        if (guardVision.playerCanSeeMe)
        {
            DevLog.Log(" For fire Suppression for playerCanSeeMe:" + suppressionLevel);
            guardVision.exposureCertainty = 100f;
            suppressionLevel += amount;
            
            suppressionLevel = Mathf.Clamp(
                suppressionLevel,
                0f,
                maxSuppression
                );

            //currentState = GuardState.Patrol;
            //UpdateTacticalGoal();
            DevLog.Log("Observe2 Decision Lag For Fire Suppression2 :" + suppressionLevel + " | Goal: " + guardMovement.currentTacticalGoal);
            //UpdateTacticalGoal();

            //guardAI.currentState = GuardAI.GuardState.Observe;
            //guardVision.PlayerRepeatedShot(player.transform.position);


        }

    }
    
    public void RegisterHit()
    {
        suppressionEpisodeRecoveryThreshold += hitRecoveryThresholdIncrease;

        suppressionEpisodeRecoveryThreshold = Mathf.Clamp(
            suppressionEpisodeRecoveryThreshold,
            baseSuppressionEpisodeRecoveryThreshold,
            maxSuppression
            );
        //Debug.Log("GUARD HIT | "+
        //            "Recovery Threshold increase to: "+
        //            suppressionEpisodeRecoveryThreshold);

    }
    private void UpdateCrouchAnimation()
    {
        if(guardAnimator == null)
        {
            //Debug.Log("guardAnimator | NULL");
            return;
        }
       
               
                
                
        if ((guardMovement.currentState == GuardMovement.GuardState.Patrol)
            || (guardMovement.currentState == GuardMovement.GuardState.EscapeRoute)
            || (guardMovement.currentState == GuardMovement.GuardState.Strike)
            || (guardMovement.currentState == GuardMovement.GuardState.MoveToCover)
            || (guardMovement.currentState == GuardMovement.GuardState.Investigate)
            )
        {
            if (currentCover != null)
            {
                //Debug.Log("Crawl MoveToCover");
                CoverPoint coverPoint = currentCover.GetComponent<CoverPoint>();
                if (coverPoint.concealmentTypes.Contains(CoverPoint.ConcealmentType.Bush))
                {
                    SetRiflePose(
                          new Vector3(-0.232f, -0.15f, 0.059f),
                          new Vector3(0f, 10f, -60f),
                          new Vector3(20f, 20f, 30f)
                      );
                    //Debug.Log("ResetVerticalLook | Rifle | Crawl");
                    guardAnimator.SetBool("IsCrawlForward", true);
                    guardAnimator.SetBool("IsCrouchStand", false);
                }

                return;
            }
            SetRiflePose(
                new Vector3(0.129f, 0.174f, 0.009f),
                new Vector3(0f, 100f, 10f),
                new Vector3(20f, 20f, 30f)
                );
            //    Debug.Log("guardAnimator | IsCrouchStand: FALSE");
            //Debug.Log("ResetVerticalLook | Rifle | CROUCH WALKING");
            guardAnimator.SetBool("IsCrawlForward", false);
            guardAnimator.SetBool("IsCrouchStand",false);

        }
        else if(
            (guardMovement.currentState == GuardMovement.GuardState.SearchPatrol)
            || (guardMovement.currentState == GuardMovement.GuardState.WaitInCover)
            || (guardMovement.currentState == GuardMovement.GuardState.HideInCover)
            ||(guardMovement.currentState == GuardMovement.GuardState.AimFire)
            )
        {
            SetRiflePose(
                new Vector3(-0.118f, -0.061f, -0.04f),
                new Vector3(0f, 0, -50f),
                new Vector3(20f, 20f, 30f)
            );
            //Debug.Log("ResetVerticalLook | Rifle | DUCK");
            //Debug.Log("guardAnimator | IsCrouchStand: TRUE");

            guardAnimator.SetBool("IsCrawlForward", false);
            guardAnimator.SetBool("IsCrouchStand", true);
        }
    }
    private void SetRiflePose
        (
        Vector3 position,
        Vector3 rotation,
        Vector3 scale
        )
    {
        if(guardAIRifle == null)
        {
            //Debug.Log("ResetVerticalLook | Rifle | NULL");
            return;
        }
        guardAIRifle.localPosition = position;
        guardAIRifle.localEulerAngles = rotation;
        guardAIRifle.localScale = scale;
    }
   private void FindAllCoverPoints()
    {
        //if(coverPoints != null) { return; }
        CoverPoint[] allCoverPoints =
            FindObjectsOfType<CoverPoint>();

        List<Transform> aiCoverPoints =
            new List<Transform>();

        foreach(CoverPoint point in allCoverPoints)
        {
            if(point == null) { continue; }
            if ((!point.CanAiUseOnly()))
            {
                continue;
            }
            aiCoverPoints.Add(point.transform);


        }
        coverPoints = aiCoverPoints.ToArray();


    }
   

    // Update is called once per frame
    void Update()
    {
        
        //Debug.DrawLine(
        //    GetHeadPosition(),
        //    GetHeadPosition() +Vector3.up * 0.35f,
        //    Color.red

        //    );
        //Debug.DrawLine(
        //    GetChestPosition(),
        //    GetChestPosition() + Vector3.up *0.3f,
        //    Color.green
        //    );
        //Debug.DrawLine(
        //    GetLegPosition(),
        //    GetLegPosition() +Vector3.up * 0.3f,
        //    Color.blue
        //    );
        
        
        


        //if (isFollowingInvestigationRoute)
        //{

        //    FollowInvestigationRoute();
        //}
        //if (isMovingToExactInvestigationPosition) {
        //    MoveToExactInvestigationPosition();

        //}

        
        if (Input.GetKeyDown(KeyCode.T)) {
            guardCover.TestRiskReward(coverPoints);
        }
        if (Input.GetKeyDown(KeyCode.P)) {

            TestPersonalityDecision();
        }
        if (Input.GetKeyDown(KeyCode.Y)) {

            guardMovement.AddPatrolSuspicion(40, playerTarget.position);
        }


        //if (timeSinceLastShot >= baitDelay) {timeSinceLastShot = 5f;currentIntent = HunterIntent.Bait;}
        UpdateCrouchAnimation();
       
        if (guardVision.playerDetected)
        {
            //Debug.Log("GUARD NOTICED MOVING GRASS Player Detected 2!");
            //CheckForMovingGrass();
            if (guardMovement.currentState == GuardMovement.GuardState.Search && guardMovement.isSearching)
            {
                guardMovement.EndSearch(true);
            }

            guardMovement.RotationTo(playerTarget.position);
            guardMovement.LookVertical(playerTarget.position);
            guardMovement.ResetVerticalLook();
            //guardMovement.Stop();
            if (guardVision.isObserving)
            {

                //observeTimer += Time.deltaTime;
                //if (observeTimer > observeTime)
                //{



                    //fireTimer += Time.deltaTime;
                    //Debug.Log("Observation Complete! But fireTimer:" + fireTimer + " in fireDelay:" + fireDelay);
                    //if (fireTimer > fireDelay)
                    //{
                        observeTimer = 0f;
                            //`currentState = GuardState.MoveToCover;
                            //Debug.Log("Observe2 CURRENT TACTICAL GOAL : Guard Fired!");
                                    
                                    guardMovement.currentState = GuardMovement.GuardState.AimFire;
                                    
                               
                                guardVision.isObserving = false;
                            //spawnManager.RespawnPlayerAtA();
                            fireTimer = 0f;
                        //guardMovement.FireShot();
                        //currentCover = guardCover.ChooseBestCover(coverPoints);
                        //isMovingToCover = true;

                        //Debug.Log("Guard Fired but the current state is:" + currentState);

                    //}
                //}
                //else
                //{
                //    //Debug.Log("the current state is:" + currentState+" but the guardVision.isObserving is"+ guardVision.isObserving);
                //    currentState = GuardState.Investigate;
                //}

            }

            //lookDirection = playerTarget.position - transform.position;
            //lookDirection.y = 0f;
            //targetRotation = Quaternion.LookRotation(lookDirection);

            //transform.rotation =
            // Quaternion.Slerp(
            //     transform.rotation,
            //     targetRotation,
            //     rotationSpeed * Time.deltaTime
            //     );
            //Debug.Log("Player Detected! ");
        }
        else
        {
            if (guardMovement.currentState == GuardMovement.GuardState.Observe)
            {
                //Debug.Log("Observe2 CURRENT TACTICAL GOAL  : OBSERVE FIRST");
                
                observeTimer += Time.deltaTime;
                
                //UpdateTacticalGoal();

                
                guardMovement.Stop();
                guardMovement.RotationTo(playerTarget.position);
                guardMovement.LookVertical(playerTarget.position);
                
                if (observeTimer >= observeTime)
                {
                    observeTimer = 0f;

                    // =====================================================
                    // LOW SUPPRESSION + LOW PREDICTION = PATROL
                    // =====================================================
                    //if((suppressionLevel < 50f) &&
                    //    (!guardVision.playerDetected) &&
                    //    (guardVision.predictionCertainty < 70f))
                    //{
                    //    Debug.Log("TACTICAL GOAL STATE | Suppression < 50 | "+
                    //                "Player Unseen + Prediction Low -> PATROL");
                    //    currentState = GuardState.Patrol;
                    //    return;
                    //}

                    //UpdateTacticalGoal();

                    DevLog.Log("Observe2 currentTacticalGoal  : " + guardMovement.observeTacticalGoal+ 
                        " | needsBetterPositionAfterSuppression: "+ needsBetterPositionAfterSuppression);

                  
                    switch (guardMovement.observeTacticalGoal)
                    {
                        case GuardMovement.TacticalGoal.Survive:
                            //Debug.Log("TACTICAL GOAL | OBSERVE -> SURVIVE MOVE TO COVER");
                            if (coverPoints == null)
                            {

                                DevLog.Log("CoverPoints | wala");
                            }
                            currentCover = guardCover.ChooseBestCover(coverPoints);
                            guardMovement.currentState = GuardMovement.GuardState.MoveToCover;
                        break;
                        case GuardMovement.TacticalGoal.GainBetterPosition:
                            //Debug.Log("TACTICAL GOAL | OBSERVE -> GAIN BETTER POSITION -> MOVE TO COVER");
                            needsBetterPositionAfterSuppression = false;
                            currentCover = guardCover.ChooseBestCover(coverPoints);
                            guardMovement.currentState = GuardMovement.GuardState.MoveToCover;
                        break;
                        case GuardMovement.TacticalGoal.ForcePlayerToMove:
                            //Debug.Log("TACTICAL GOAL | OBSERVE -> FORCE MOVE");
                            // Temporary: ForceMove state does not exist yet.
                            // We will create its actual movement behavior later.
                            currentCover = guardCover.ChooseBestCover(coverPoints);
                            guardMovement.currentState = GuardMovement.GuardState.MoveToCover;
                        break;
                        case GuardMovement.TacticalGoal.PinPlayer:
                            //Debug.Log("TACTICAL GOAL | OBSERVE -> PIN PLAYER");
                            guardMovement.currentState = GuardMovement.GuardState.AimFire;
                        break;
                        case GuardMovement.TacticalGoal.HuntPlayer:
                            //Debug.Log("TACTICAL GOAL | OBSERVE -> HUNT PLAYER");
                            guardMovement.currentState = GuardMovement.GuardState.Investigate;
                        break;
                        case GuardMovement.TacticalGoal.Patrol:
                            //Debug.Log("TACTICAL GOAL | OBSERVE -> PATROL");
                            guardMovement.currentState = GuardMovement.GuardState.Patrol;
                        break;
                        case GuardMovement.TacticalGoal.EscapeRoute:
                            //Debug.Log("TACTICAL GOAL | OBSERVE -> ESCAPE ROUTE");
                            guardMovement.currentState = GuardMovement.GuardState.EscapeRoute;
                        break;

                    }
                    //switch (currentTacticalGoal)
                    //{

                    //    case TacticalGoal.Survive:

                    //        currentState = GuardState.MoveToCover;
                    //        break;
                    //    case TacticalGoal.ForcePlayerToMove:
                    //        currentState = GuardState.MoveToCover;
                    //        break;
                    //    case TacticalGoal.PinPlayer:
                    //        currentState = GuardState.MoveToCover;
                    //        break;
                    //    case TacticalGoal.GainBetterPosition:
                    //        currentState = GuardState.MoveToCover;
                    //        break;
                    //    case TacticalGoal.HuntPlayer:
                    //        currentState = GuardState.Investigate;
                    //        break;

                    //}
                }

            }
            else if(guardMovement.currentState == GuardMovement.GuardState.EscapeRoute)
            {
                if (guardVision.isInterrupt()) { return; }
                if (guardMovement.isFollowingIndirectionInvestigationRoute)
                {
                    currentCover = null;
                    guardMovement.FollowindirectioninvestigationRoute();
                    return;
                }
                if (guardMovement.isMovingToExactIndirectionInvestigationRoute)
                {
                    currentCover = null;
                    guardMovement.MoveToExactIndirectionInvestigationRoute();
                    return;
                }
                if ((!guardMovement.isFollowingEscapeRoute)&&
                    (!guardMovement.isWaitingAtEscapePoint))
                    //(!guardMovement.escapeRouteCompleted))
                {
                    guardMovement.PrepareEscapeRoute();
                }
                if (guardMovement.isFollowingEscapeRoute)
                {
                    guardMovement.FollowEscapeRoute();
                }
                if (guardMovement.isWaitingAtEscapePoint)
                {
                    guardMovement.HandleEscapePointWait();
                }
                
               
            }
            else if (guardMovement.currentState == GuardMovement.GuardState.Strike)
            {
                if (guardVision.isInterrupt()) { return; }
                if (guardMovement.isMovingToExactIndirectionStrikeRoute)
                {
                    currentCover = null;
                    guardMovement.isFollowingIndirectionStrikeRoute = false;
                    guardMovement.MoveToExactIndirectionStrikeRoute();
                    return;
                }
                    if (guardMovement.isFollowingIndirectionStrikeRoute)
                {
                    currentCover = null;
                    guardMovement.isWaitingAtStrikePoint = false;
                    guardMovement.FollowIndirectionStrikeRoute();
                    //guardMovement.isFollowingIndirectionStrikeRoute = false;
                    return;
                }
                if (guardMovement.isWaitingAtStrikePoint)
                {
                    if (!guardVision.hasStrikeRouteRecord)
                    {
                        guardVision.TestStrikeRouteMemory();
                    }
                    guardMovement.SetIndirectionStrikeRoute(guardVision.lastStrikeRoute);
                    guardMovement.isWaitingAtStrikePoint = false;
                }
               
            }
            else if (guardMovement.currentState == GuardMovement.GuardState.MoveToCover)
            {
                

                //direction = currentCover.position - transform.position;
                //direction.y = 0f;
                //direction.Normalize();
                //transform.position += direction * patrolSpeed * Time.deltaTime;
                //Debug.Log("Observe2 Move To Cover23!");

                if (guardCover.MoveToCover(currentCover))
                {
                    //Debug.Log("Observe2 Move To Cover23 Inside");
                    //guardCover.SetCover(currentCover);
                    CoverPoint coverPoint = currentCover.GetComponent<CoverPoint>();
                    if (coverPoint.concealmentTypes.Contains(CoverPoint.ConcealmentType.Bush))
                    {
                        guardMovement.searchPoint = currentCover.transform;
                        guardMovement.searchPhase = GuardMovement.SearchPhase.PauseAndListen;
                        //currentState = GuardState.SearchPatrol;
                        guardMovement.isSearchingInCover = true;
                        guardMovement.isPeekingSide = true;
                        guardMovement.currentState = GuardMovement.GuardState.HideInCover;
                    }
                    else if (coverPoint.concealmentTypes.Contains(CoverPoint.ConcealmentType.Rock))
                    {
                        guardMovement.currentState = GuardMovement.GuardState.WaitInCover;
                    }
                        

                }

                //  randomWaitTime = Random.Range(
                //minWaitTime,
                //maxWaitTime
                //);

                randomWaitTime = GetPersonalityWaitTime();
                waitTime = randomWaitTime + (suppressionLevel * 0.02f);
                waitTime = 0.3f;
            }
            else if (guardMovement.currentState == GuardMovement.GuardState.HideInCover)
            {
                if(suppressionLevel >= 150)
                {
                    currentCover = guardCover.ChooseBestCover(coverPoints);
                    guardMovement.currentState = GuardMovement.GuardState.MoveToCover;
                    return;
                }
                guardMovement.UpdateSearch();
            }
            else if (guardMovement.currentState == GuardMovement.GuardState.WaitInCover)
            {

                //Debug.Log("Observe2 AI Reached To Cover!");

                //coverWaitTimer += Time.deltaTime;

                //if (guardCover.WaitInCover(waitTime))
                //{
                //Debug.Log("Current Intent:" + currentIntent);

                //isWaitingInCover = false;asd
                //guardVision.lastSeenCertainty = true;asd
                //currentTacticalGoal = EvaluateTacticalGoal();
                //if (currentTacticalGoal == TacticalGoal.HuntPlayer) return;

                //Debug.Log("TACTICAL GOAL WINNER :" + guardMovement.currentTacticalGoal);
                CoverDecision decision = EvaluateCurrentCover();
                //Debug.Log("Observe2 Decision 2 :" + decision
                             //+ " | predictionCertainty : " + guardVision.predictionCertainty
                             //+ " | exposureCertainty : " + guardVision.exposureCertainty
                             // + " | locationCertainty : " + guardVision.locationCertainty

                             //);
                //+ " | "+ coverReturnDecision);
                //CoverDecision decision = CoverDecision.Fight;

                switch (decision)
                {
                    case CoverDecision.Fight:
                        currentIntent = HunterIntent.Pressure;
                        //guardCover.SetCoverWeights(0.7f, 1.5f);
                        break;
                    case CoverDecision.Fake:
                        currentIntent = HunterIntent.Bait;
                        //guardCover.SetCoverWeights(1f, 1.2f);
                        break;
                    case CoverDecision.Wait:
                        currentIntent = HunterIntent.Conceal;
                        //guardCover.SetCoverWeights(1.5f, 0.5f);
                        break;
                    case CoverDecision.Relocate:
                        guardVision.hasStrikeRouteRecord = false;
                        guardCover.coverPosition = Vector3.zero;
                        currentIntent = HunterIntent.Relocate;
                        //guardCover.SetCoverWeights(1.3f,0.8f);
                        break;
                    case CoverDecision.Strike:
                        guardVision.hasStrikeRouteRecord = false;
                        currentIntent = HunterIntent.Strike;
                        break;
                }
                ApplyPersonalityCoverWeights(decision);
                switch (currentIntent)
                {
                    case HunterIntent.Strike:
                        guardMovement.isWaitingAtStrikePoint = true;
                        guardMovement.currentState = GuardMovement.GuardState.Strike;
                        break;
                    case HunterIntent.Relocate:
                        //Debug.Log("CurrentIntent: Relocate");
                        if (guardVision.locationCertainty > 0)
                        {
                            if (suppressionLevel >= 70) { guardMovement.currentState = GuardMovement.GuardState.Observe; return; }


                            //currentCover = guardCover.ChooseBestCover(coverPoints);
                            //currentState = GuardState.MoveToCover;
                            guardMovement.currentState = GuardMovement.GuardState.Investigate;


                        }
                        else { guardMovement.currentState = GuardMovement.GuardState.Patrol; }



                        break;
                    case HunterIntent.Pressure:
                        //Debug.Log("CurrentIntent: Pressure");
                        float pressureWaitTime = waitTime * GetPressureWaitMultiplier();
                        //Debug.Log(
                        //    $"PRESSURE TIMING | Aggression:{aggression} Wait:{pressureWaitTime}"
                        //   );

                        if (guardCover.WaitInCover(pressureWaitTime))
                        {


                            //guardCover.RefreshPeekDangers();
                            sidePeek = guardCover.GetBestPeekDirection();
                            //Debug.Log("Observe2 Hunter Intent Pressure side:" + sidePeek);
                            switch (sidePeek)
                            {
                                case GuardCover.PeekDirection.Left:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanLeft;
                                    break;
                                case GuardCover.PeekDirection.Right:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanRight;
                                    break;
                                case GuardCover.PeekDirection.Up:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanUp;
                                    break;

                            }

                            //if (guardCover.lean()) { currentState = GuardState.LeanLeft; ; }
                            //else { currentState = GuardState.LeanRight; }
                        }
                        break;
                    case HunterIntent.Bait:
                        //Debug.Log("CurrentIntent: Bait");
                        float baitWaitTime = waitTime * GetBaitWaitMultiplier();
                        //Debug.Log(
                        //         $"BAIT TIMING | Deception:{deception} Wait:{baitWaitTime}"
                        //         );
                        if (guardCover.WaitInCover(baitWaitTime))
                        {
                            //guardCover.RefreshPeekDangers();
                            sidePeek = guardCover.GetBestPeekDirection();
                            switch (sidePeek)
                            {
                                case GuardCover.PeekDirection.Left:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanLeft;
                                    break;
                                case GuardCover.PeekDirection.Right:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanRight;
                                    break;
                                case GuardCover.PeekDirection.Up:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanUp;
                                    break;

                            }
                            //if (guardCover.lean()) { currentState = GuardState.LeanLeft; ; }
                            //else { currentState = GuardState.LeanRight; }
                        }
                        break;
                    case HunterIntent.Conceal:


                        if (guardCover.SilentWait(3f))
                        {
                            //guardCover.RefreshPeekDangers();
                            sidePeek = guardCover.GetBestPeekDirection();
                            //Debug.Log("Observe2 CurrentIntent: Conceal side: " + sidePeek);
                            switch (sidePeek)
                            {
                                case GuardCover.PeekDirection.Left:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanLeft;
                                    break;
                                case GuardCover.PeekDirection.Right:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanRight;
                                    break;
                                case GuardCover.PeekDirection.Up:
                                    guardMovement.currentState = GuardMovement.GuardState.LeanUp;
                                    break;

                            }
                            //currentIntent = HunterIntent.Pressure;
                            //currentState = GuardState.WaitInCover;

                        }

                        //waitTime *= 1.5f;

                        //
                        break;
                }

            }
            else if (guardMovement.currentState == GuardMovement.GuardState.LeanLeft)
            {

                if (guardCover.leanLeft())
                {
                    if (currentCover == null)
                    {
                        currentCover = guardCover.ChooseBestCover(coverPoints);
                        guardMovement.currentState = GuardMovement.GuardState.MoveToCover;
                        return;
                    }
                    guardMovement.searchPoint = currentCover.transform;
                    guardMovement.searchPhase = GuardMovement.SearchPhase.PauseAndListen;
                    //currentState = GuardState.SearchPatrol;
                    guardMovement.isSearchingInCover = true;
                    guardMovement.currentState = GuardMovement.GuardState.Aim;
                    guardMovement.isPeekingSide = true;
                    ////if (guardCover.fakePeek(guardCover.leftSuppression)) {
                    //if (guardCover.ShouldFakePeek())
                    //{
                    //    fakeSide = guardCover.GetMostSuppressedSide();

                    //    isFakePeekLeft = true;
                    //    currentState = GuardState.ReturnToCover;
                    //}
                    //else
                    //{
                    //    guardCover.StartExposure();
                    //    currentState = GuardState.Aim;
                    //}
                    //===================>KULANG NG fakeSide = guardCover.GetMostSuppressedSide();
                }
                //else { currentState = GuardState.ReturnToCover; }

            }
            else if (guardMovement.currentState == GuardMovement.GuardState.LeanUp)
            {
                if (guardCover.leanUp())
                {
                    if (currentCover == null)
                    {
                        currentCover = guardCover.ChooseBestCover(coverPoints);
                        guardMovement.currentState = GuardMovement.GuardState.MoveToCover;
                        return;
                    }
                    guardMovement.searchPoint = currentCover.transform;
                    guardMovement.searchPhase = GuardMovement.SearchPhase.PauseAndListen;
                    guardMovement.isSearchingInCover = true;
                    guardMovement.currentState = GuardMovement.GuardState.Aim;
                    guardMovement.isPeekingSide = true;
                }
            }
            else if (guardMovement.currentState == GuardMovement.GuardState.LeanRight)
            {
                if (guardCover.leanRight())
                {
                    if (currentCover == null)
                    {
                        currentCover = guardCover.ChooseBestCover(coverPoints);
                        guardMovement.currentState = GuardMovement.GuardState.MoveToCover;
                        return;
                    }
                    //if (guardCover.fakePeek(guardCover.rightSuppression)) {
                    guardMovement.searchPoint = currentCover.transform;
                    guardMovement.searchPhase = GuardMovement.SearchPhase.PauseAndListen;
                    //currentState = GuardState.SearchPatrol;
                    guardMovement.isSearchingInCover = true;
                    guardMovement.isPeekingSide = true;
                    guardMovement.currentState = GuardMovement.GuardState.Aim;
                    //if (guardCover.ShouldFakePeek())
                    //{
                    //    fakeSide = guardCover.GetMostSuppressedSide();
                    //    isFakePeekRight = true;
                    //    currentState = GuardState.Aim;
                    //    //currentState = GuardState.ReturnToCover; 
                    //}
                    //else
                    //{
                    //    guardCover.StartExposure();
                    //    currentState = GuardState.Aim;
                    //}
                    //===================>KULANG NG fakeSide = guardCover.GetMostSuppressedSide();
                    //else { currentState = GuardState.ReturnToCover; }
                }
            }
            else if (guardMovement.currentState == GuardMovement.GuardState.Aim)
            {
                //Debug.Log("failedPeek Aim!");
                guardCover.TickExposure();


                //guardCover.IsOverExposed() ||
                //Debug.Log("Vertical Outsider!");
                if ((guardCover.IsCurrentSideTooHot()) || (suppressionLevel >= 150f))
                {
                    //Debug.Log("Observe2 Decision 2  suppressionLevel: " + suppressionLevel);
                    guardMovement.isSearchingInCover = false;
                    //Debug.Log("iSuppression Abort Aim! Too exposed for player!");
                    aimTimer = 0f;
                    guardCover.RegisterFailedPeek();
                    guardMovement.currentState = GuardMovement.GuardState.ReturnToCover;
                    return;
                }

                guardMovement.UpdateSearch();


                //UpdateSearch();
                //aimTimer += Time.deltaTime;

                //if (aimTimer > aimTime)
                //{

                //aimTimer = 0;
                //if (guardCover.trapShotFire())
                //{ guardMovement.RotationTo(predictedPosition); }

                //else
                //{ guardMovement.RotationTo(playerTarget.position); }
                //=================== LEFT ======RIGHT=======UP====BACK SCAN=====>asd { HUNT}
                //===================ELSE ((lastKnownLocation!= null) &&(lastSeenCertainty) &&(guardVision.certaintyLocation>0)) { ?HUNT }
                ////=================if(lastSeenCertainty){ AIM COVER AND FIRE if() }
                ///==================={    SCAN IF NONE,   guardCover.RegisterFailedPeek(); IF SEEN, HEARD FOOTSTEP AND GUNSHOT Resets!  }
                ///

                //if (!guardCover.HasEnoughPrediction(guardVision.predictionCertainty))
                //{
                //    aimTimer = 0f;asd
                //    guardCover.RegisterFailedPeek();

                //    currentState = GuardState.Fire;
                //    return;
                //}
                //currentState = GuardState.ReturnToCover;

                //} //AIM TIMER BRACKET
            }
            else if (guardMovement.currentState == GuardMovement.GuardState.AimFire)
            {
                //guardMovement.RotationTo(playerTarget.transform.position);
                //guardMovement.LookVertical(playerTarget.transform.position);
                //Debug.Log("currentGuardState AimFire ");
                if (!guardVision.CanGuardSeePlayer())
                {

                    DevLog.Log("currentGuardState AimFire canGuardSeeMe");
                    //Debug.Log("pCertainty AMBUSH SHOT RESULT CANCELLED - PLAYER LOST!");
                    aimTimer = 0f;
                    guardVision.failedAmbushTile = guardVision.bestAmbushTile;
                    guardVision.ambushPredictionChanged = true;
                    guardVision.ResetAmbush();


                    
                    guardVision.UpdatePredictionState();
                    if (guardVision.isInCover())
                    {
                        guardVision.playerDetected = false;
                        return;
                    }
                    guardVision.playerDetected = false;

                    guardMovement.currentState = GuardMovement.GuardState.Observe;
                    return;
                }
                //if((guardVision.CanGuardSeePlayer())&&(!guardVision.playerCanSeeMe)){
                //    guardVision.playerDetected = false;
                //    currentState = GuardState.Patrol;
                //    return;

                //}
                //if (!guardVision.IsPlayerInsideKillZone())
                //{
                //    Debug.Log("currentGuardState AimFire IsPlayerInsideKillZone");
                //    Debug.Log("pCertainty AMBUSH SHOT RESULT ESCAPED KILL ZONE!");
                //    guardVision.predictionCertainty -= 20f;
                //    guardVision.predictionCertainty = Mathf.Clamp(
                //        guardVision.predictionCertainty, 0f, 100f);
                //    guardVision.failedAmbushTile = guardVision.bestAmbushTile;
                //    guardVision.ambushPredictionChanged = true;
                //    guardVision.ResetAmbush();
                //    aimTimer = 0f;
                //    currentState = GuardState.Observe;
                //    guardVision.UpdatePredictionState();
                //    guardVision.playerDetected = false;
                //    return;
                //}
                //Debug.Log("pCertainty AMBUSH AimFire AMBUSH TIMING!");

                guardMovement.RotationTo(playerTarget.transform.position);
                guardMovement.LookVertical(playerTarget.transform.position);
                //aimTimer
                aimTimer += Time.deltaTime;
                if (aimTimer >= aimTime)
                {
                    //thinkingRouteTimer = 0f;

                    //Debug.Log("pCertainty AMBUSH AimFire FireShot!");
                    bool hitPlayer = guardMovement.FireShot();
                    //Debug.Log("pCertainty AMBUSH SHOT RESULT | HIT Player : " + hitPlayer);
                    //aimTimer = 0;

                    //currentState = GuardState.Observe;

                    //currentState = GuardState.ReturnToCover;
                    //currentState = GuardState.Fire;
                    //currentIntent = HunterIntent.Pressure;
                }
            }

            else if (guardMovement.currentState == GuardMovement.GuardState.SearchPatrol)
            {
                //Debug.Log("Oyeah Patrol");
                //Debug.Log("Last Patrol Search !");
                //if ((guardVision.predictionCertainty > 0)) {  return; }
                if (guardVision.isInterrupt()) { return; }
                if (guardVision.lastKnownPosition != Vector3.zero)
                {

                    //Debug.Log("Last Patrol Search 2 Pasok!");
                    //currentState = GuardState.Investigate;
                    guardMovement.currentState = GuardMovement.GuardState.Observe;

                }
                //Debug.Log("Last Patrol Search!");
                //if ((guardVision.lastKnownPosition != null) || (guardVision.lastKnownPosition != Vector3.zero))
                //{
                //    Debug.Log("Last Patrol Search!");

                //    return;
                //}
                guardMovement.UpdateSearch();
            }

            else if (guardMovement.currentState == GuardMovement.GuardState.Fire)
            {

                //isFiringFromCover = false;
                //isReturningToCover = true;
                if (currentIntent == HunterIntent.Pressure)
                {
                    pressureShotsRemaining--;
                    if (pressureShotsRemaining > 0)
                    {
                        guardMovement.FireShot();
                        guardCover.ResetFailedPeeks();
                        guardMovement.currentState = GuardMovement.GuardState.Aim;
                    }
                    else
                    {
                        guardMovement.FireShot();
                        guardCover.ResetFailedPeeks();
                        currentIntent = HunterIntent.Conceal;
                        guardMovement.currentState = GuardMovement.GuardState.ReturnToCover;
                        guardCover.ToggleLeanSide();
                    }
                }
                else
                {
                    guardMovement.FireShot();
                    guardCover.ResetFailedPeeks();
                    guardMovement.currentState = GuardMovement.GuardState.ReturnToCover;
                    guardCover.ToggleLeanSide();
                }

                //Debug.Log("Fire from cover!");
            }

            else if (guardMovement.currentState == GuardMovement.GuardState.ReturnToCover)
            {
                //isWaitingInCover = true;
                if (guardCover.ReturnToCover())
                {
                    //Debug.Log("Observe2 DecisionLag : ReturnToCover");
                    guardMovement.currentState = GuardMovement.GuardState.WaitInCover;
                    //    randomWaitTime = Random.Range(
                    //minWaitTime,
                    //     maxWaitTime
                    //    );
                    randomWaitTime = GetPersonalityWaitTime();
                    waitTime = randomWaitTime + (suppressionLevel * 0.02f);
                    bool sameStrikeRoute = guardVision.TestStrikeRouteMemory();
                    updateStrikeScore(sameStrikeRoute);
                    
                    //waitTime = 0.3f;
                    //if (isFakePeekLeft){ currentState = GuardState.LeanRight; isFakePeekLeft = false; Debug.Log("iSuppression lean Left Again"); }
                    //else if (isFakePeekRight) { currentState = GuardState.LeanLeft; isFakePeekRight = false; Debug.Log("iSuppression lean Right Again"); }
                    //guardCover.ResetExposure();


                }
                //asd
                //switch (EvaluateCurrentCover())
                //{
                //    case CoverDecision.Fight:
                //        //guardCover.CanStillFight();
                //        break;
                //    case CoverDecision.Fake:
                //        guardCover.ShouldFakePeek();
                //        break;
                //    case CoverDecision.Wait:
                //        guardCover.AreBothSidesDangerous();
                //        break;
                //    case CoverDecision.Relocate:
                //        if (guardCover.ShouldRelocate(guardVision.predictionCertainty))
                //        {
                //            currentIntent = HunterIntent.Relocate;
                //            currentState = GuardState.MoveToCover;
                //        }
                //        else
                //        {
                //            currentState = GuardState.WaitInCover;
                //        }
                //        break;


                //        //}


            }

            else if (guardMovement.currentState == GuardMovement.GuardState.Investigate)
            {
                //NEED ONLY LAST KNOWN POSITION
                currentCover = null;
                if (guardVision.isInterrupt()) { return; }
                if ((guardVision.CanGuardSeePlayer()) && (guardVision.playerCanSeeMe))
                {
                    //Debug.Log("CAN DO PATROL SEE PLAYER");
                    //guardMovement.RotationTo(playerTarget.transform.position);
                    //guardMovement.LookVertical(playerTarget.transform.position);
                    //guardMovement.Stop();
                    return;
                }

                guardMovement.thinkingRouteTimer += Time.deltaTime;
                if (guardMovement.thinkingRouteTimer < 1f) return;
                if (!guardMovement.investigationRoutePrepared)
                {

                    guardMovement.PrepareInvestigationRoute();
                    guardMovement.investigationRoutePrepared = true;
                    DevLog.Log("For fire SEARCH POINT INVESTIGATION STARTED | " +
                              "TARGET: " + guardVision.lastKnownPosition);
                }
                if (guardMovement.isFollowingInvestigationRoute)
                {
                    DevLog.Log("For fire SEARCH POINT FollowInvestigationRoute()");
                    guardMovement.FollowInvestigationRoute();
                }
                else if (guardMovement.isMovingToExactInvestigationPosition)
                {
                    DevLog.Log("For fire SEARCH POINT MoveToExactInvestigationPosition()");
                    guardMovement.MoveToExactInvestigationPosition();
                }
                //Debug.Log("CAN DO STOCK !");
                return;
                //float known = Vector3.Distance(transform.position, guardVision.lastKnownPosition);
                //Debug.Log("Is Investigating? Yes the last known position is " + guardVision.lastKnownPosition + " direction:" + known + " < 2f");
                //guardMovement.MoveTo(guardVision.lastKnownPosition);
                //if (Vector3.Distance(transform.position, guardVision.lastKnownPosition) < 2f)
                //{
                //    PrepareInvestigationRoute();
                //    BeginSearch();
                //}

            }
            else if (guardMovement.currentState == GuardMovement.GuardState.Search)
            {


                guardMovement.UpdateSearch();
            }
            else if (guardMovement.currentState == GuardMovement.GuardState.CheckSuspiciousGrass)
            {
                //guardMovement.Stop();
                if (guardMovement.suspiciousGrass == null)
                {
                    guardMovement.currentState = GuardMovement.GuardState.Patrol;
                    return;
                }
                guardMovement.RotationTo(guardMovement.suspiciousGrass.transform.position);
                guardMovement.LookVertical(guardMovement.suspiciousGrass.transform.position);
                guardMovement.suspiciousGrassLookTimer += Time.deltaTime;
                //if (suspiciousGrassLookTimer >= suspiciousGrassLookDuration) {
                if (!guardMovement.suspiciousGrassRegistered)
                {
                    //AddPatrolSuspicion(15f, suspiciousGrass.transform.position);
                    guardMovement.suspiciousGrassRegistered = true;
                    DevLog.Log("GRASS SUSPICION CONFIRMED : " + guardMovement.suspiciousGrass.name +
                                  " | TOTAL SUSPICION:" + guardMovement.patrolSuspicion +
                                  " | TOTAL SUSPICION LOCATION:" + guardMovement.suspiciousGrass.transform.position);

                    //}
                    //currentState = GuardState.Patrol;

                    guardMovement.suspiciousGrassLookTimer = 0f;

                }
                switch (guardMovement.currentGrassResponse)
                {
                    case GuardMovement.GrassResponse.Ignore:

                        guardMovement.currentState = GuardMovement.GuardState.Patrol;
                        break;
                    case GuardMovement.GrassResponse.Observe:
                        DevLog.Log("GRASS RESPONSE SUSPICIOUS GRASS IS " + guardMovement.suspiciousGrass.name);
                        guardMovement.Stop();
                        guardMovement.RotationTo((guardMovement.suspiciousGrass.transform.position - transform.position));
                        observeWaitTimer += Time.deltaTime;
                        DevLog.Log("GRASS RESPONSE OBSERVE STOP!");
                        if (observeWaitTimer < 2f) return;
                        DevLog.Log("GRASS RESPONSE OBSERVE AND PATROL CONTINUE!");
                        observeWaitTimer = 0f;

                        guardMovement.currentGrassResponse = GuardMovement.GrassResponse.Ignore;
                        guardMovement.suspiciousGrass = null;
                        break;
                    case GuardMovement.GrassResponse.SuppressFire:
                        guardMovement.Stop();
                        guardMovement.RotationTo((guardMovement.suspiciousGrass.transform.position - transform.position));

                        guardMovement.FireShot();
                        DevLog.Log("GRASS RESPONSE SUPPRESSING FIRE!");
                        fireShotGrassTimer += Time.deltaTime;
                        if (fireShotGrassTimer < 5F) return;
                        DevLog.Log("GRASS RESPONSE SUPPRESSING FIRE STOP!");
                        guardMovement.currentGrassResponse = GuardMovement.GrassResponse.Investigate;
                        break;
                    case GuardMovement.GrassResponse.Investigate:

                        guardVision.lastKnownPosition = guardMovement.suspiciousGrass.transform.position;
                        guardMovement.currentState = GuardMovement.GuardState.Investigate;
                        //currentGrassResponse = GrassResponse.SuppressFire;
                        DevLog.Log("GRASS RESPONSE INVESTIGATE!");
                        break;
                    case GuardMovement.GrassResponse.MoveToCover:
                        currentCover = guardCover.ChooseBestCover(coverPoints);
                        guardMovement.currentState = GuardMovement.GuardState.MoveToCover;
                        DevLog.Log("GRASS RESPONSE MOVE TO COVER!");
                        //currentState = GuardState.Patrol;
                        break;

                }


            }
            else if (guardMovement.currentState == GuardMovement.GuardState.Patrol)
            {
                //if ((guardVision.predictionCertainty > 0) ) { return; }
                //Debug.Log("Last Patrol Patrol 2! lastKnownPosition: " + guardVision.lastKnownPosition);
                if (guardVision.isInterrupt()) { return; }
                //if (guardVision.lastKnownPosition != Vector3.zero)
                //{
                //    //currentState = GuardState.Investigate;
                //    currentState = GuardState.Observe;
                //    Debug.Log("Last Patrol Patrol 2 Pasok! lastKnownPosition: " + guardVision.lastKnownPosition);

                //}

                //if ((guardVision.lastKnownPosition != Vector3.zero))
                //{
                //    Debug.Log("Last Patrol!");

                //    return;
                //}
                //guardMovement.ResetVerticalLook();
                guardMovement.PatrolGraphMovement();
                //if (Input.GetKey(KeyCode.G))
                //{

                //    //PatrolGraphMovement();
                //    PrepareInvestigationRoute();
                //    Debug.Log("INVESTIGATION ROUTE READY PATROL NODE G");

                ////}
                ////CheckForMovingGrass();
                //if (guardVision.playerDetected)
                //{
                //    isCheckingPatrolCover = false;
                //    patrolCheckPoint = null;
                //    patrolLookTimer = 0f;
                //    return;
                //}
                ////targetPoint = patrolPoints[currentPoint];asd
                //targetPoint = GetNearestPatrolPoint(transform.position);
                //Debug.Log("GET NEAREST POINT :" + targetPoint.name);
                //if (isCheckingPatrolCover)
                //{
                //    guardMovement.Stop();
                //    if (patrolCheckPoint != null)
                //    {
                //        guardMovement.RotationTo(patrolCheckPoint.position);
                //    }
                //    patrolLookTimer += Time.deltaTime;
                //    if (patrolLookTimer >= patrolLookDuration)
                //    {
                //        if (patrolCheckPoint != null)
                //        {
                //            recentlyCheckedPatrolCovers.Add(patrolCheckPoint);
                //            if (recentlyCheckedPatrolCovers.Count > maxRecentPatrolChecks)
                //            {
                //                recentlyCheckedPatrolCovers.RemoveAt(0);
                //            }
                //            Debug.Log("PATROL COVER CHECKED: " + patrolCheckPoint.name);
                //        }
                //        patrolCheckPoint = null;
                //        patrolLookTimer = 0f;
                //        isCheckingPatrolCover = false;

                //    }

                //}
                //else
                //{
                //    guardMovement.MoveTo(targetPoint.position);
                //    patrolCheckTimer += Time.deltaTime;
                //    float currentCheckInterval = GetPatrolCheckInterval();
                //    Debug.Log("Current Check Interval :" + currentCheckInterval);

                //    if (patrolCheckTimer >= currentCheckInterval)
                //    {
                //        patrolCheckTimer = 0f;
                //        if (patrolSuspicion >= 30f)
                //        {
                //            guardMovement.RotationTo(suspiciousPosition);
                //        }

                //        patrolCheckPoint = guardCover.ChoosePatrolCheckPoint(
                //                            transform.position,
                //                            patrolCoverCheckRadius,
                //                            recentlyCheckedPatrolCovers,
                //                            coverPoints
                //                            );

                //        if (patrolCheckPoint != null)
                //        {
                //            isCheckingPatrolCover = true;
                //            patrolLookTimer = 0f;
                //            Debug.Log("PATROL CHECK :" + patrolCheckPoint.name +
                //                      " | Suspicion:" + patrolSuspicion +
                //                      " | Interval:" + currentCheckInterval +
                //                      " | Suspicious Position:" + suspiciousPosition
                //                );

                //        }
                //    }


                //}

                //if ((!isCheckingPatrolCover) && (Vector3.Distance(transform.position, targetPoint.position) < 2f))
                //{

                //    currentPoint++;

                //    if (currentPoint >= patrolPoints.Length)
                //    {
                //        currentPoint = 0;
                //    }
                //}



            }
            
        }
    }
    
    
       

   
    float GetPersonalityWaitTime() {
        float patienceNormalized = patience / 100f;
        float baseWait = Random.Range(
            minWaitTime,
          maxWaitTime
            );
        float personalityBonus = patienceNormalized * 2f;
        return baseWait * personalityBonus;
    
    }
    float GetPressureWaitMultiplier()
    {
        float aggressionNormalized = aggression / 100f;

        return Mathf.Lerp(
            1.2f,
            0.5f,
            aggressionNormalized
            );

    }
    float GetBaitWaitMultiplier() {
        float deceptionNormalized = deception / 100;
        return Mathf.Lerp(
            1f,
            1.8f,
            deceptionNormalized
            );
    }
   
    
    

    void ApplyPersonalityCoverWeights(CoverDecision decision) {
        float aggressionNormalized = aggression / 100f;
        float patienceNormalized = patience / 100f;
        float deceptionNormalized = deception / 100f;
        float cautionNormalized = caution / 100f;
        
        switch (decision) {
            case CoverDecision.Fight:
                guardCover.SetCoverWeights(1f-aggressionNormalized * 0.3f, 1f + aggressionNormalized * 0.5f);
            break;
            case CoverDecision.Fake:
                guardCover.SetCoverWeights(1f, 1f + deceptionNormalized * 0.3f);
            break;
            case CoverDecision.Wait:
                guardCover.SetCoverWeights(1f + patienceNormalized * 0.5f, 0.7f);
            break;
            case CoverDecision.Relocate:
                guardCover.SetCoverWeights(1f + cautionNormalized * 0.5f, 0.8f);
            break;    
        
        }
        
    }
    private void updateStrikeScore(bool sameRoute)
    {
        if (sameRoute)
        {
            strikeScore += strikeBigAddition + guardVision.locationCertainty;
        }
        else
        {
            strikeScore += strikeSmallAddition;
        }
        strikeScore = Mathf.Clamp(strikeScore, 0f, 200f);
       //     Debug.Log(
       //    "STRIKE SCORE | " +
       //    "SameRoute: " + sameRoute +
       //    " | Score: " + strikeScore
       //);
    }
    public CoverDecision EvaluateCurrentCover()
    {
        //guardCover.RefreshPeekDangers();
        float fightScore = 0f;
        float fakeScore = 0f;
        float waitScore = 0f;
        float relocateScore = 0f;
        //strikeScore = 0f;
        float diff = 0f;

        float aggressionBias = aggression * 0.2f;
        float patienceBias = patience * 0.2f;
        float deceptionBias = deception * 0.2f;
        float cautionBias = caution * 0.2f;
        //============= FIGHT! ======================= EYES 
        fightScore += aggressionBias;
        fightScore += guardVision.exposureCertainty * 0.35f;
        fightScore += guardVision.locationCertainty * 0.45f;
        fightScore += guardVision.predictionCertainty * 0.20f;

        fightScore += Mathf.InverseLerp(200f, 0f, suppressionLevel) * 60f;
        if ((guardVision.locationCertainty > 80f) && (guardVision.predictionCertainty > 80f)) { fightScore += 30f; }
        if ((suppressionLevel > 150f) && (!guardVision.playerCanSeeMe)) { fightScore *= 0.5f; }

            //fightScore += aggressionBias;
            //fightScore = guardVision.exposureCertainty * 0.35f +
            //             guardVision.locationCertainty * 0.45f +
            //             guardVision.predictionCertainty * 0.20f;
            //fightScore -= suppressionLevel;

            fightScore = Mathf.Clamp(fightScore, 0, 200);
        //============ WAIT! ================
        waitScore += patienceBias;
        if (!guardVision.playerDetected) {
            waitScore += 40f;
            
        }

        waitScore += (100f - guardVision.locationCertainty) * 0.5f;
        waitScore += (100f - guardVision.predictionCertainty) * 0.3f;
        if (suppressionLevel < 60f) { waitScore += 40f; }
        else if (suppressionLevel < 120f) { waitScore += 20f; }
        else  { waitScore += 0f; }
        waitScore = Mathf.Clamp(
                    waitScore,
                    0,
                    200
                    );
        //========== DIFF===========
        diff = Mathf.Abs(guardCover.leftSuppression - guardCover.rightSuppression);
        fakeScore += GetFakePressureScore();
        fakeScore += diff * 0.5f;
        fakeScore += guardVision.locationCertainty * 0.2f;
        fakeScore += guardVision.predictionCertainty * 0.3f;
        fakeScore += deceptionBias;
        if (suppressionLevel >= 140f) {
            float multiplier = Mathf.InverseLerp(200f, 140f, suppressionLevel);
            fakeScore *= multiplier;
        
        }
        fakeScore = Mathf.Clamp(fakeScore, 0, 200);
        //============RELOCATE==============
        //relocateScore = guardCover.GetRelocationPressure(suppressionLevel);
        double pressureRelocateScore = 0f;
        if (guardCover.AreBothSidesDangerous(suppressionLevel)) {
            pressureRelocateScore = 60f;
            relocateScore += 60;
            
        }
        relocateScore += Mathf.InverseLerp(120f, 200f, suppressionLevel) * 40f;
        relocateScore += guardCover.failedPeeks * 20;
        relocateScore += cautionBias;
        relocateScore = Mathf.Clamp(
                        relocateScore,
                        0,
                        200
                        );
        //Debug.Log("PERSONALITY DECISION AreBothSidesDangerous : " + pressureRelocateScore+
        //            " failedPeeks: "+ guardCover.failedPeeks+
        //            " cautionBias: "+ cautionBias);
        //Debug.Log(
        //        "PERSONALITY DECISION | " +
        //        $"Fight:{fightScore} " +
        //        $"Fake:{fakeScore} " +
        //        $"Wait:{waitScore} " +
        //        $"Relocate:{relocateScore} | " +
        //        $"Aggression:{aggression} " +
        //        $"Patience:{patience} " +
        //        $"Deception:{deception} " +
        //        $"Caution:{caution}"+
        //        $"Strike: "+strikeScore
        //    );
        coverReturnDecision = $"Fight:{fightScore} " +
                $"Fake:{fakeScore} " +
                $"Wait:{waitScore} " +
                $"Relocate:{relocateScore} | " +
                $"Strike: " + strikeScore;
        //STRIKE
        //strikeScore = 
        if ((fightScore > waitScore) && (fightScore > fakeScore) && (fightScore > relocateScore) && (fightScore > strikeScore)) {
           
            return CoverDecision.Fight;
        }
        if ((fakeScore > fightScore) && (fakeScore > waitScore) && (fakeScore > relocateScore) && (fakeScore > strikeScore))
        {
            return CoverDecision.Fake;
        }
        if ((waitScore > fightScore) && (waitScore > fakeScore)  && (waitScore > relocateScore) && (waitScore > strikeScore))
        {
            return CoverDecision.Wait;
        }
        if ((relocateScore > fightScore) && (relocateScore > fakeScore) && (relocateScore > waitScore) && (relocateScore > strikeScore))
        {
            return CoverDecision.Relocate;
        }
        if ((strikeScore > fightScore) && (strikeScore > fakeScore) && (strikeScore > waitScore) && (strikeScore > relocateScore))
        {
            return CoverDecision.Strike;
        }

        return CoverDecision.Wait;
    }
    private float GetFakePressureScore() {
        float suppression = suppressionLevel;


        if (suppression < 40f) return 20f;
        if (suppression < 80f) return 60f;
        if (suppression < 120f) return 100f;
        if (suppression < 160f) return 50f;

        return 0f;

    }
    

    //void EvaluateCurrentCover()
    //{

    //    if (guardCover.IsCurrentSideTooHot())
    //    {

    //        guardCover.RegisterFailedPeek();
    //    }
    //    if (guardCover.ShouldRelocateAfterFailures())
    //    {


    //    }
    //    else
    //    {
    //        currentState = GuardState.WaitInCover;

    //    }
    //}
    
    
    //void FireShot() {
        
    //    float chance = Random.Range(0f,100f);
    //    float accuracy = GetAccuracy();
    //    accuracy -= suppressionLevel * 0.3f;
    //    accuracy = Mathf.Clamp(
    //          accuracy,
    //          10f,
    //          100f
    //        );

    //    if (chance <= accuracy) {
    //        Debug.DrawLine(
    //            guardVision.eyePoint.position,
    //            playerTarget.position,
    //            Color.blue,
    //            1f
    //            );
    //        Debug.Log("GRASS RESPONSE Player was Hit!"); }
    //    else {
    //        Vector3 missOffset = new Vector3(
    //                Random.Range(-2f, 2f),
    //                Random.Range(-1f, 1f),
    //                Random.Range(-2f, 2f)
    //            );
    //        Vector3 missPoint = playerTarget.position + missOffset;
    //        Debug.DrawLine(
    //            guardVision.eyePoint.position,
    //            missPoint,
    //            Color.yellow,
    //            1f
    //            );
    //        nearMissDistance = Vector3.Distance(
    //            missPoint,
    //            playerTarget.position
    //            );
    //        Debug.Log("GRASS RESPONSE Player was Missed!"); }

    //}
    public void ResetAI()
    {
        //guardAI = FindObjectOfType<GuardAI>();
        //if (guardAI == null) { return; }
        resetBatch++;
        health = 100;
       
       
        guardVision.playerDetected = false;
        guardVision.playerCanSeeMe = false;
        guardVision.guardCanSeeMe = false;
        guardVision.isInvestigating = false;
        guardVision.isObserving = false;
       
        guardMovement.isSearching = false;
        guardMovement.searchPoint = null;
        guardMovement.searchedPoints.Clear();
        guardMovement.searchAttempts = 0;
        guardMovement.searchPauseTimer = 0f;
        guardMovement.searchScanTimer = 0f;
        guardMovement.currentState = GuardMovement.GuardState.Patrol;
        guardMovement.searchPhase = GuardMovement.SearchPhase.LookAtLikelyDirection;
        suppressionLevel = 0;
        currentCover = null;
        guardMovement.ResetVerticalLook();
        guardMovement.ResetSpine2ForNewRound();
        guardVision.lastKnownPosition = Vector3.zero;
        //guardVision.playerDetected = false;

       
        isMovingToCover = false;
        isInCover = false;
        isWaitingInCover = false;

        isLeaningLeft = false;
        isLeaningRight = false;

        isReturningToCover = false;
        isFiringFromCover = false;

        guardVision.forgetTimer = 0f;
        observeTimer = 0f;
        fireTimer = 0f;
        searchTimer = 0f;
        guardCover.coverWaitTimer =0f;

        currentPoint = 0;

        
    }
    
}
