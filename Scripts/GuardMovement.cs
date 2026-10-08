using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static UnityEngine.GraphicsBuffer;

public class GuardMovement : MonoBehaviour
{
    // Start is called before the first frame update

    //    Step 4 — Tune the speed

    //Try these values:

    //verticalRotationSpeed = 2

    //Very slow surveillance.

    //verticalRotationSpeed = 5

    //Natural.

    //verticalRotationSpeed = 10

    //Military quick look.

    //verticalRotationSpeed = 20

    //Almost instant.

    //For Still Hunt, I'd start with 5 or 6.
    public enum GrassResponse
    {
        Ignore,
        Observe,
        Investigate,
        SuppressFire,
        MoveToCover,
        Nothing

    }
    public GrassResponse currentGrassResponse;
    public GrassArea[] grassAreas;

    public GrassArea suspiciousGrass;
    public enum GuardState
    {
        Patrol,//used
        Observe,//used
        MoveToCover,//used
        WaitInCover,//used
        LeanLeft,//used/ mild
        LeanRight,//used /mild
        LeanUp,//used /mild
        ReturnToCover,//used mild
        AimFire,//used
        Fire,
        Investigate,//used
        Search,
        Dead,
        Aim,
        FakePeek,
        CheckSuspiciousGrass,
        SearchPatrol,//used
        EscapeRoute,//used
        Strike,//used
        HideInCover,//used
        ForceToRelocate//used

    }
    public GuardState currentState;
    public GuardState state;
    public enum SearchPhase
    {
        LookAtLikelyDirection,
        MoveToSearchPoint,
        PauseAndListen,
        ScanLeft,
        ScanCenter,
        ScanRight,
        SearchFailed
    }
    public SearchPhase searchPhase;
    public enum VerticalScan
    {
        Down,
        Middle,
        Up

    }
    private VerticalScan verticalScan = VerticalScan.Down;
    public enum TacticalGoal
    {
        Survive,
        ForcePlayerToMove,
        PinPlayer,
        GainBetterPosition,
        HuntPlayer,
        Patrol,
        EscapeRoute,
        ForceToRelocate
    }
    public TacticalGoal currentTacticalGoal;
    public TacticalGoal observeTacticalGoal;

    [Header("Escape Route")]
    private PatrolPoint escapeStartNode;
    private PatrolPoint escapeTargetNode;
    private PatrolPoint currentPatrolNode;
    private PatrolPoint nextPatrolNode;
    private PatrolPoint previousPatrolNode;
    private PatrolPoint investigationStartNode;
    private PatrolPoint investigationTargetNode;


    public PatrolPoint GetFinalEscapeRouteNode()
    {
        return escapeTargetNode;
    }

    private PatrolPoint recoveryPatrolPoint = null;

    private List<PatrolPoint> failedRecoveryPoints = new List<PatrolPoint>();
    private List<PatrolPoint> walkRecoveryPatrolPoints = new List<PatrolPoint>();
    private List<PatrolPoint> recentTacticalPatrolNodes = new List<PatrolPoint>();
    private List<PatrolPoint> investigationRoute = new List<PatrolPoint>();
    private List<PatrolPoint> indirectionInvestigationRoute = new List<PatrolPoint>();
    private List<PatrolPoint> indirectionStrikeRoute = new List<PatrolPoint>();


    [SerializeField]
    private int maxRecentTacticalPatrolNodes = 10;

    public int maxRecentPatrolChecks = 3;
    public int searchAttempts = 0;

    private int escapeRouteIndex = 0;
    private int maxSearchAttempts = 5;
    private int indirectionInvestigationRouteIndex = 0;
    private int indirectionStrikeRouteIndex = 0;

    private List<PatrolPoint> escapeRoute = new List<PatrolPoint>();
    private List<PatrolPoint> checkedEscapePatrolPoint = new List<PatrolPoint>();
    private List<PatrolPoint> postEscapeRoute = new List<PatrolPoint>();


    [SerializeField] private float escapeWaitDuration = 1f;

    private float escapeWaitTimer = 0f;
    private float patrolCheckTimer = 0f;
    private float certainty = 100f;
    private float isGroundedFall = 0f;



    CharacterController controller;
    public Transform[] patrolPoints;

    [SerializeField] private Transform eyePoint;
    [SerializeField] private Transform muzzlePoint;

    public  Transform searchPoint;

    private Transform patrolCheckPoint;
   

    private List<Transform> recentlyCheckedPatrolCovers = new List<Transform>();
    public  List<Transform> searchedPoints = new List<Transform>();

    [SerializeField] private float verticalRotationSpeed = 5f; //10f

    [Header("Vertical Look Rig")]
    [SerializeField] private Transform verticalLookTarget;
    [SerializeField] private MultiAimConstraint spine2VerticalAim;
    [SerializeField] private float verticalLookDistance = 5f;
    [SerializeField] private float verticalLookAngle = 15f;

    [SerializeField]
    private Transform spine2;
    private Quaternion spine2BaseLocalRotation;


    [SerializeField] private float TracerSpeed = 120f;

    [SerializeField] private int fallbackPlayerDamage = 5;

    [SerializeField] private GameObject bulletTracerPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab; 
   


    public Player player;

    [SerializeField] private float stuckTimeout = 5f;
    [SerializeField] private float stuckSampleInterval = 0.5f;
    [SerializeField] private float minimumProgressDistance = 0.08f;


    [Header("Patrol Suspicion")]
    public float patrolSuspicion = 0f;
    public float normalPatrolCheckInterval = 3f;
    public float suspiciousPatrolCheckInterval = 1f;

    public float thinkingRouteTimer = 0f;
    public float rotationSpeed = 5f;
    public float PatrolSpeed =2.5f;
    public float closeAccuracy = 90f;
    public float mediumAccuracy = 70f;
    public float farAccuracy = 45f;
    public float closeRange = 10f;
    public float mediumRange = 20f;
    public float sprintSpeed = 0;
    public float patrolLookDuration = 0.7f;
    public float patrolCheckInterval = 2f;
    public float patrolCoverCheckRadius = 8f;
    public float patrolConnectionDistance = 8f;
    public float grassNoticeDistance = 15f;
    public float suspiciousGrassLookTimer = 0f;
    public float searchPauseTimer = 0f; 
    public float searchScanTimer = 0f;
    public float timeSinceLastShot = 0f;

    private int patrolRouteIndex = 0;
    private int investigationRouteIndex = 0;

    private float patrolLookTimer = 0.7f;
    private float patrolWaitTimer = 0f;
    private float stuckSampleTimer = 0f;
    private float stuckTimer = 0f;
   
    private float searchLookDuration = 1f;
    private float searchPauseDuration = 1.5f;
    
    private float searchScanDuration = 2f;
 

    float gravity = -12f;
    float verticalVelocity;
    float nearMissDistance = 0f;

    
    
    private bool isCheckingPatrolCover = false;
    private bool isSearchingInCoverTrue = false;
    private bool searchNextPhase = false;
    private bool isRecoveringFromStuck = false;
    private bool isRecoveringFromStuckFalse = false;

    public bool isWaitingAtEscapePoint = false;
    public bool isWaitingAtStrikePoint = false;
    public bool suspiciousGrassRegistered = false;
    public bool isSearching = false;
    public bool isMovingToExactInvestigationPosition = false;
    public bool isMovingToExactIndirectionInvestigationRoute = false;
    public bool isMovingToExactIndirectionStrikeRoute = false;
    public bool isFollowingInvestigationRoute = false;
    public bool investigationRoutePrepared = false;
    public bool isWaitingAtPatrolNode = false;
    public bool isFollowingEscapeRoute = false;
    public bool isSearchingInCover = false;
    public bool isPeekingSide = false;
    public bool escapeRouteCompleted = false;
    public bool isFollowingIndirectionInvestigationRoute = false;
    public bool isFollowingIndirectionStrikeRoute = false;

    public SpawnManager spawnManager;
    private GameManager gameManager;
    private GuardVision guardVision;
    private GuardAI guardAI;
    private GuardCover guardCover;
    private PlayerHealth playerHealth;

   

    public Animator soldierAnimator;

    private List<GameObject> activeTracers = new List<GameObject>();
  

    BulletThreatUI bulletThreatUI;

    public Vector3 target;
    public Vector3 OriginalBaseTarget;
    public Vector3 suspiciousPosition;

    private Vector3 likelySearchDirection;
    private Vector3 baseTarget;
    private Vector3 lastProgressPosition;
    private Vector3 originalMoveTarget;

    Vector3 direction;
    Vector3 movement;
    public void Start()
    {
        guardVision = GetComponent<GuardVision>();
        guardAI = GetComponent<GuardAI>();
        guardCover = GetComponent<GuardCover>();
        controller = GetComponent<CharacterController>();
        player = guardAI.playerTarget.GetComponent<Player>();
        playerHealth = guardAI.playerTarget.GetComponent<PlayerHealth>();
        gameManager = guardAI.spawnManager.GetComponent<GameManager>();
        bulletThreatUI = FindObjectOfType<BulletThreatUI>();
        spawnManager = guardAI.spawnManager;
        lastProgressPosition = transform.position;
        FindAllPatrolPoints();
        
        
        if (spine2 != null)
        {
            spine2BaseLocalRotation = spine2.localRotation;
            DevLog.Log(
       "ResetVerticalLook | SPINE2 BASE ROTATION | " +
       spine2BaseLocalRotation.eulerAngles
   );
        }
    }
    public void FindAllPatrolPoints()
    {
        PatrolPoint[] points = FindObjectsOfType<PatrolPoint>();

        patrolPoints = new Transform[points.Length];

        for(int i=0; i  < points.Length; i++)
        {
            patrolPoints[i] = points[i].transform;

        }
        //Debug.Log("PATROL POINTS | FOUND: " + patrolPoints.Length);
    }
    public void Stop() {
        movement = Vector3.zero;
        movement.y = verticalVelocity;
        controller.Move(movement * Time.deltaTime);

    }
    public void ResetSpine2ForNewRound()
    {
        DevLog.Log("ResetVerticalLook | RESET ROUND | SPINE2");
        if (spine2VerticalAim != null)
        {
            spine2VerticalAim.weight = 0;
        }
        if((verticalLookTarget!=null)&&
            (eyePoint != null))
        {
            verticalLookTarget.position = eyePoint.position + transform.forward * verticalLookDistance;
        }
        if (spine2 != null)
        {
            spine2.localRotation = spine2BaseLocalRotation;
            //spine2.localRotation = Vector3.zero;
        }
    }
    public void ResetVerticalLook()
    {
        DevLog.Log("ResetVerticalLook");
        if (spine2VerticalAim != null)
        {
            spine2VerticalAim.weight = 0f;
        }
        if(verticalLookTarget != null)
        {
            verticalLookTarget.position = eyePoint.position +
                transform.forward * verticalLookDistance;
        }
        //if (eyePoint != null)
        //{
        //    eyePoint.localRotation = Quaternion.identity;
        //}
        //soldierAnimator.Rebind();
        //soldierAnimator.Update(0f);
    }
    public void LookVertical(Vector3 target)
    {
        if ((verticalLookTarget == null) ||
            (spine2VerticalAim == null) ||
            (eyePoint == null))
        {
            return;
        }
        if((guardVision.playerDetected)||
            (currentState == GuardState.AimFire))
        {
            Transform playerLookPoint = guardVision.GetBestVisiblePlayerPoint();
            if (playerLookPoint != null)
            {
                verticalLookTarget.position =
                    Vector3.Lerp(
                        verticalLookTarget.position,
                        playerLookPoint.position,
                        verticalRotationSpeed * Time.deltaTime
                        );
                DevLog.Log(
            "VERTICAL LOOK | FOLLOW PLAYER | " +
            playerLookPoint.name
    );
            }
        }
       
        Vector3 toTarget = target - eyePoint.position;

        DevLog.Log("vertical look batchReset: "+guardAI.resetBatch
            
            +" toTarget: "+ toTarget
            
            +" verticalScan: "+verticalScan);
        Vector3 flatDirection = new Vector3(
            toTarget.x,
            0f,
            toTarget.z
            );
        float horizontalDistance = flatDirection.magnitude;

        if (horizontalDistance < 0.01)
        {
            horizontalDistance = 0.01f;
        }

        float verticalDifference = target.y - eyePoint.position.y;

        float pitch = Mathf.Atan2(
            verticalDifference,
            horizontalDistance
            ) * Mathf.Rad2Deg;

        pitch = Mathf.Clamp(
            pitch,
            -verticalLookAngle,
            verticalLookAngle
            );

        //Debug.Log(
        //     "VERTICAL LOOK | reset batch: "+guardAI.resetBatch
        //     +" PITCH: " +pitch
        //     +" | WEIGHT BEFORE: " +
        //        spine2VerticalAim.weight
        //    );
        if ((!guardVision.playerDetected)
            && (currentState != GuardState.AimFire))
        {
            Vector3 desiredTarget =
                eyePoint.position +
                transform.forward *
                verticalLookDistance;

            desiredTarget.y =
                eyePoint.position.y +
                Mathf.Tan(pitch * Mathf.Deg2Rad) * verticalLookDistance;

            verticalLookTarget.position =
                Vector3.Lerp(
                    verticalLookTarget.position,
                    desiredTarget,
                    verticalRotationSpeed * Time.deltaTime
                    );
            DevLog.Log(
              "VERTICAL LOOK | FALSE FOLLOW PLAYER | "
      );
        }
        float verticalAmount =
            Mathf.InverseLerp(
                0f,
                verticalLookAngle,
                Mathf.Abs(pitch)
                );
        float targetWeight =
            Mathf.Clamp01(verticalAmount);

        if ((guardVision.playerDetected)||
            (currentState == GuardState.AimFire))
        {
            targetWeight = 1f;
            DevLog.Log("VERTICAL LOOK | FOLLOW PLAYER +1");
        }

        spine2VerticalAim.weight =
            Mathf.MoveTowards(
                spine2VerticalAim.weight,
                targetWeight,
                verticalRotationSpeed * Time.deltaTime
                );
        //Debug.Log(
        //    "VERTICAL LOOK | WEIGHT AFTER: " +
        //    spine2VerticalAim.weight
        //    );
        //Quaternion targetRotation =
        //    Quaternion.Euler(
        //        -pitch,
        //        0f,
        //        0f
        //        );
        //eyePoint.localRotation =
        //    Quaternion.Slerp(
        //        eyePoint.localRotation,
        //        targetRotation,
        //        verticalRotationSpeed * Time.deltaTime
        //        );                                           //There is an issue
    }
    //public void LookVertical(Vector3 target)
    //{

    //    if ((verticalLookTarget == null) || (spine2VerticalAim == null)) return;



    //    Vector3 direction = target - eyePoint.position;

    //    Vector3 localDirection = transform.InverseTransformDirection(direction);


    //    float pitch = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;


    //    //pitch = Mathf.Clamp(pitch, -30f, 30f);
    //    pitch = Mathf.Clamp(
    //        pitch,
    //        -verticalLookAngle,
    //        verticalLookAngle
    //        );

    //    //Create a target in front of the guard.

    //    Quaternion targetRotation = Quaternion.Euler(-pitch, 0f, 0f); //eyePoint

    //    Vector3 desiredTarget = eyePoint.position + transform.forward * verticalLookDistance;

    //    // Move target vertically according to desired pitch.
    //    desiredTarget.y = eyePoint.position.y + Mathf.Tan(pitch * Mathf.Deg2Rad) * verticalLookDistance;

    //    verticalLookTarget.position = Vector3.Lerp(
    //        verticalLookTarget.position,
    //        desiredTarget,
    //        verticalRotationSpeed * Time.deltaTime
    //        );

    //    //Determine whether we are looking vertically.
    //    float verticalAmount = Mathf.InverseLerp(
    //        0f,
    //        verticalLookAngle,
    //        Mathf.Abs(pitch)
    //        );
    //    if (verticalAmount >= 1f)
    //    {
    //        Debug.Log("vertical amount : 1 -> " + verticalAmount);
    //    }
    //    else
    //    {
    //        Debug.Log("vertical amount : 0 -> " + verticalAmount);
    //        verticalAmount = 0.50f;
    //    }
    //    //if (guardVision.playerDetected)
    //    //{
    //    //    Debug.Log("vertical amount : player Detected");
    //    //    verticalAmount = 0.5f;
    //    //}
    //    //Debug.Log("vertical amount : "+ verticalAmount);
    //    //if (guardVision.playerDetected) { verticalAmount = 0f; }

    //    //Middle = normal animation
    //    //Up/Dow = Spine2 rig becomes active
    //    float targetWeight = Mathf.Clamp01(verticalAmount);

    //    spine2VerticalAim.weight =
    //        Mathf.MoveTowards(
    //            spine2VerticalAim.weight,
    //            targetWeight,
    //            verticalRotationSpeed * Time.deltaTime
    //            );


    //    eyePoint.localRotation =
    //                            Quaternion.Slerp(
    //                                eyePoint.localRotation,
    //                                targetRotation,
    //                                verticalRotationSpeed * Time.deltaTime
    //                                );
    //    //eyePoint.localRotation = Quaternion.Euler(-pitch, 0f, 0f);
    //    //Debug.Log("Pitch Pitch: " + pitch);
    //    //Debug.Log("Pitch Eye Local Rotation: " + eyePoint.localEulerAngles);

    //}

    float GetAccuracy()
    {
        float distance = Vector3.Distance(
            transform.position,
            guardVision.playerTarget.position
            );
        if (distance <= closeRange) return closeAccuracy; //10
        if (distance <= mediumRange) return mediumAccuracy; //20
        
        return farAccuracy;
    }
    public void TakeDamage(int damage)
    {
        
        guardAI.health -= damage;
        guardAI.RegisterHit();
        guardAI.suppressionLevel = 200f;
        //Debug.Log("Guard Health:" + guardAI.health);
        if (guardAI.health <= 0)
        {
            Die();
        }

    }
    public void Die()
    {
        //Debug.Log("ROUND Guard Eliminated!");
        foreach (GameObject tracer in activeTracers)
        {
            if (tracer != null)
            {
                Destroy(tracer);
            }

        }
        activeTracers.Clear();

        //guardAI.health = 200;
        gameObject.SetActive(false);
        //gameManager.GuardDefeated();
        if (spawnManager != null)
        {
            spawnManager.RespawnGuard();
        }
    }
    public bool FireShot()
    {
        
        if (guardAI.playerTarget == null)
        {
            //Debug.Log("fCertainty FIRE FAILED: No Player Target");
            
            return false;
            
        }
        bool didHitPlayer = false;

        //Vector3 origin = guardVision.eyePoint.position;
        //Vector3 targetPosition = guardAI.playerTarget.position;
        //Vector3 direction = (targetPosition - origin).normalized;
        //float distance = Vector3.Distance(origin, targetPosition);
        //Vector3 bulletEndPoint = targetPosition;

        //Debug.DrawRay(
        //    origin,
        //    direction * distance,
        //    Color.red, 1f);

        Vector3 origin = guardVision.eyePoint.position;

        //Vector3 targetPosition = guardAI.playerTarget.position;
        Transform shotTargetPoint = guardVision.GetBestVisiblePlayerPoint();

        if(shotTargetPoint == null)
        {
            DevLog.Log("GUARD SHOT | NO VISIBLE PLAYER POINT");
            return false;
        }
        Vector3 targetPosition = shotTargetPoint.position;
        // ---------------------------------------------------------
        // FIND THE BEST VISIBLE PART OF THE PLAYER
        // HEAD -> CHEST -> PELVIS
        // ---------------------------------------------------------
        Transform[] shotPoints = 
        { 
            guardVision.playerPelvisPoint,
            guardVision.playerChestPoint,
            guardVision.playerHeadPoint
        };

        string[] shotPointNames =
        {
            "PELVIS",
            "CHEST",
            "HEAD"
        };
        RaycastHit targetHit;
        bool foundVisibleTarget = false;

        for (int i=0;i < shotPoints.Length; i++)
        {
            Transform point = shotPoints[i];
            if (point == null) continue;

            Vector3 toPoint = point.position - origin;
            float pointDistance = toPoint.magnitude;

            //if (pointDistance > guardVision.currentViewDistance) continue;
            Vector3 pointDirection = toPoint.normalized;
            //Debug.DrawRay(
            //    origin,
            //    pointDirection * pointDistance,
            //    Color.yellow,
            //    1f
            //    );
            if(Physics.Raycast(
                origin,
                pointDirection,
                out targetHit,
                pointDistance,
                guardVision.visibilityMask
                )) 
            {
                PlayerHitZone hitZone = targetHit.collider.GetComponent<PlayerHitZone>();

                Player hitPlayer = targetHit.collider.GetComponentInParent<Player>();

                Player targetPlayer = guardAI.playerTarget.GetComponentInParent<Player>();

                if ((hitPlayer !=null && hitPlayer == targetPlayer) || 
                    (hitZone!=null))
                {
                    shotTargetPoint = point;
                    targetPosition = point.position;
                    foundVisibleTarget = true;
                    //Debug.DrawRay(
                    //    origin,
                    //    pointDirection * targetHit.distance,
                    //    Color.black,
                    //    2f
                    //    );
                    DevLog.Log("GUARD SHOT | TARGET POINT FOUND: "+ shotPointNames[i]);
                    break;
                }
                DevLog.Log(
                    "GUARD SHOT | "+
                    shotPointNames[i]+
                    " BLOCKED BY "+
                    targetHit.collider.name);
            }
        }
        Vector3 direction = (targetPosition - origin).normalized;
        float distance = Vector3.Distance(origin, targetPosition);
        Vector3 bulletEndPoint = targetPosition;
        //===============================

        float chance = Random.Range(0f, 100f);
        float accuracy = GetAccuracy();
        accuracy -= guardAI.suppressionLevel * 0.3f;
        accuracy = Mathf.Clamp(
            accuracy,
            10f,
            100f
            );
        SpawnMuzzleFlash();
        if (chance <= accuracy)
        {

            //Debug.Log("pCertainty AMBUSH SHOT RESULT | HIT | Chance: " + chance+" <= Accuracy: "+accuracy);

            RaycastHit hit;

            bool didHit = Physics.Raycast
                (origin,
                direction,
                out hit,
                distance,
                guardVision.visibilityMask
                );
            if (didHit)
            {
                PlayerHitZone playerHitZone =
                    hit.collider.GetComponentInParent<PlayerHitZone>();

                Player hitPlayer = hit.collider.GetComponentInParent<Player>();

                Player targetPlayer = guardAI.playerTarget.GetComponentInParent<Player>();

                // -----------------------------------------------------
                // PLAYER HITBOX
                // -----------------------------------------------------
            
                if(playerHitZone != null)
                {
                    int damage = playerHitZone.GetDamage();
                    DevLog.Log(
                        "GUARD SHOT | PLAYER HIT ZONE: "+playerHitZone.zoneType
                        +" | Collider: "+ hit.collider.name
                        +" | Damage: "+damage);
                    playerHitZone.ApplyDamage(damage);
                    didHitPlayer = true;
                    SpawnTracer(
                        muzzlePoint != null
                        ? muzzlePoint.position
                        : origin,
                        hit.point
                        );
                    if (bulletThreatUI != null)
                    {
                        bulletThreatUI.ShowRedIndicator(hit.point);
                    }
                    //Debug.DrawRay(
                    //    origin,
                    //    direction * hit.distance,
                    //    Color.black,
                    //    2f
                    //    );
                    DevLog.Log(
                        "GUARD SHOT | HIT POINT: "+hit.point
                        );
                    return true;
                }
                // -----------------------------------------------------
                // PLAYER BODY FALLBACK
                // -----------------------------------------------------
                if(hitPlayer != null && hitPlayer == targetPlayer)
                {
                    DevLog.Log("GUARD SHOT | PLAYER BODY FALLBACK DAMAGE: "+fallbackPlayerDamage);
                    if(playerHealth!= null)
                    {
                        playerHealth.TakeDamage(
                            PlayerHealth.ZoneType.Chest,
                            fallbackPlayerDamage
                            );
                           
                    }
                    SpawnTracer(
                        muzzlePoint != null
                        ? muzzlePoint.position
                        : origin,
                        hit.point
                        );
                    if (bulletThreatUI != null)
                    {
                        bulletThreatUI.ShowRedIndicator(hit.point);

                    }
                    //Debug.DrawRay(
                    //    origin,
                    //    direction * hit.distance,
                    //    Color.black,
                    //    2f
                    //    );
                    return true;
                }
                // -----------------------------------------------------
                // SOMETHING BLOCKED THE SHOT
                // -----------------------------------------------------
                DevLog.Log(
                    "GUARD SHOT | BLOCKED BY :" + hit.collider.name
                    + " | Layer : " + LayerMask.LayerToName(hit.collider.gameObject.layer)
                    );
                //Debug.DrawRay(
                //    origin,
                //    direction * hit.distance,
                //    Color.red,
                //    2f
                //    );
            }
            //RaycastHit[] hits = Physics.RaycastAll(
            //    origin,
            //    direction,
            //    distance
            //    );
            //System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            //PlayerHitZone playerHitZone = null;
            //RaycastHit hit = default;
            //bool foundPlayerHitZone = false;
            //bool foundPlayerBody = false;

            //foreach (RaycastHit currentHit in hits)
            //{
            //    PlayerHitZone currentZone = currentHit.collider.GetComponent<PlayerHitZone>();
            //    if (currentZone != null)
            //    {
            //        playerHitZone = currentZone;
            //        hit = currentHit;
            //        foundPlayerHitZone = true;
            //        Debug.Log("GUARD SHOT | PLAYER HITBOX FOUND: " + currentHit.collider.name);

            //        break;
            //    }
            //    Player currentPlayer = currentHit.collider.GetComponentInParent<Player>();
            //    if (currentPlayer != null)
            //    {
            //        foundPlayerBody = true;
            //        hit = currentHit;
            //        Debug.Log("GUARD SHOT | PLAYER MAIN BODY FOUND: " + currentHit.collider.name);
            //        continue;
            //    }
            //    Debug.Log("GUARD SHOT | BLOCKED BY: " + currentHit.collider.name);
            //    break;
            //}
            //if (foundPlayerHitZone)
            //{
            //    int damage = playerHitZone.GetDamage();
            //    Debug.Log("GUARD SHOT | PLAYER HIT ZONE: " +
            //                playerHitZone.zoneType +
            //                " | Damage: " +
            //                damage);

            //    playerHitZone.ApplyDamage(damage);
            //    didHitPlayer = true;
            //}
            //else if (foundPlayerBody)
            //{
            //    Debug.Log("GUARD SHOT | PLAYER BODY FALLBACK DAMAGE: " + fallbackPlayerDamage);
            //    if (playerHealth != null)
            //    {
            //        playerHealth.TakeDamage(PlayerHealth.ZoneType.Chest, fallbackPlayerDamage);
            //    }
            //    didHitPlayer = true;
            //}
            //if (foundPlayerHitZone || foundPlayerBody)
            //{
            //    SpawnTracer(muzzlePoint != null ? muzzlePoint.position : origin, hit.point);
            //    if (bulletThreatUI != null)
            //    {
            //        bulletThreatUI.ShowRedIndicator(hit.point);

            //    }
            //    Debug.Log("GUARD SHOT | HIT POINT: " + hit.point);
            //}
        }
        else
        {
            Vector3 missOffset = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-1f, 1f),
                Random.Range(-2f, 2f)
                );
            Vector3 missPoint = targetPosition + missOffset;

            Vector3 missDirection = (missPoint - origin).normalized;
            Vector3 closestBulletPoint = GetClosestPointOnBulletLine(
                guardVision.playerTarget.position,
                origin,
                missDirection
                );
            float bulletDistance = Vector3.Distance(guardVision.playerTarget.position, closestBulletPoint);
            if (bulletThreatUI != null)
            {
                bulletThreatUI.ShowWhiteIndicator(
                    closestBulletPoint,
                    bulletDistance
                    );

            }
            //Debug.DrawLine(
            //    guardVision.playerTarget.position,
            //    closestBulletPoint,
            //    Color.cyan,
            //    2f
            //    );
            //Debug.Log("BULLET CLOSEST POINT: " + closestBulletPoint + " | DISTANCE: " + bulletDistance);

            RaycastHit missHit;
            //Debug.Log("BULLET THREAT DISTANCE: " + bulletDistance);
            if (Physics.Raycast(
                origin,
                missDirection,
                out missHit,
                100f
                ))
            {
                bulletEndPoint = missHit.point;
            }
            else
            {

                bulletEndPoint = origin + missDirection * 100f;
            }
            SpawnTracer(muzzlePoint != null ? muzzlePoint.position : origin, bulletEndPoint);

            //Debug.DrawLine(
            //    origin,
            //    missPoint,
            //    Color.yellow,
            //    1f
            //    );
            //nearMissDistance = Vector3.Distance(missPoint, targetPosition);
            nearMissDistance = bulletDistance;
            //Debug.Log("fCertainty REAL SHOT | MISS | Chance: " +
            //            chance +
            //            " > Accuracy: " +
            //            accuracy);

        }


        //    if (Physics.Raycast(
        //        origin,
        //        direction,
        //        out hit,
        //        distance
        //        ))
        //    {

        //        Debug.Log("fCertainty REAL SHOT | RAY HIT: " + hit.collider.name);
        //        if ((hit.transform == guardVision.playerTarget) || (hit.transform.IsChildOf(guardVision.playerTarget)))
        //        {

        //            if (player != null)
        //            {
        //                //TakeDamage(1);
        //                player.TakeDamage(5);
        //                if (bulletThreatUI != null)
        //                {
        //                    bulletThreatUI.ShowRedIndicator(hit.point);

        //                }
        //                hitPlayer = true;
        //                Debug.Log("fCertainty REAL SHOT PLAYER DAMAGE!");
        //                //return true;
        //            }

        //        }
        //    }

        //}

        return didHitPlayer; 

    }
    private Vector3 GetClosestPointOnBulletLine(Vector3 point, Vector3 bulletOrigin, Vector3 bulletDirection)
    {
        bulletDirection.Normalize();
        Vector3 toPoint = point - bulletOrigin;

        float projection =
            Vector3.Dot(toPoint, bulletDirection);
        projection = Mathf.Max(0f, projection);

        Vector3 closestPoint = bulletOrigin + bulletDirection * projection;

        float distance = Vector3.Distance(point, closestPoint);

        return closestPoint;

    }
    public void RotationTo(Vector3 target) {
        //Debug.Log("RotationTo Called!");
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        //Debug.DrawRay(
        //    transform.position,
        //    direction.normalized * 3f,
        //    Color.black
        //    );
               
             if (direction.sqrMagnitude < 0.01) {
                return;
              }
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
                );
    }
    public void MoveToPredictionTile(PatrolPoint tile) {
        bool reachedPredictionTile;
        reachedPredictionTile = MoveTo(tile.transform.position);
        
    }
   
    private void UpdateSuppression()
    {
        if (guardAI.suppressionLevel > 0f)
        {

            guardAI.suppressionLevel -= guardAI.suppressionRecovery * Time.deltaTime;
            guardAI.suppressionLevel = Mathf.Clamp(
                guardAI.suppressionLevel,
                0f,
                guardAI.maxSuppression
                );
        }


        if (guardCover.coverPosition != Vector3.zero) { return; }
        if (guardAI.suppressionEpisodeRecoveryThreshold > guardAI.baseSuppressionEpisodeRecoveryThreshold)
        {
            guardAI.suppressionEpisodeRecoveryThreshold -= guardAI.hitRecoveryThresholdRecovery * Time.deltaTime;

            guardAI.suppressionEpisodeRecoveryThreshold = Mathf.Max(
                guardAI.suppressionEpisodeRecoveryThreshold,
                guardAI.baseSuppressionEpisodeRecoveryThreshold
                );
        }
        UpdateSuppressionEpisode();
        DevLog.Log("SUPPRESSION MEMORY | Level : " + guardAI.suppressionLevel
                    + " | Recovery Threshold: " + guardAI.suppressionEpisodeRecoveryThreshold
                    + " | Active: " + guardAI.suppressionEpisodeActive
                    + " | Completed Episodes: " + guardAI.highSuppressionEpisode);
    }
    private void UpdateSuppressionEpisode()
    {

        if (!guardAI.suppressionEpisodeActive)
        {
            if (guardAI.suppressionLevel >= guardAI.suppressionEpisodeStartThreshold)
            {
                guardAI.suppressionEpisodeActive = true;
                DevLog.Log("SUPPRESSION EPISODE STARTED | " +
                            "SUPPRESSION: " + guardAI.suppressionLevel);
            }
            return;
        }
        if (guardAI.suppressionLevel <= guardAI.suppressionEpisodeRecoveryThreshold)
        {
            guardAI.suppressionEpisodeActive = false;


            guardAI.highSuppressionEpisode++;

            if (guardAI.highSuppressionEpisode == 1)
            {
                guardAI.needsBetterPositionAfterSuppression = true;
                DevLog.Log("SUPPRESSION MEMORY POST SUPPRESSION RECOVERY | " +
                            "GUARD MUST GAIN A BETTER POSITION");
            }
            else if (guardAI.highSuppressionEpisode == 2)
            {
                guardAI.needsEscapeRouteAfterSuppression = true;
                DevLog.Log("SUPPRESSION MEMORY POST SUPPRESSION RECOVER | " +
                            "SECOND EPISODE -> GUARD MUST FIND ESCAPE ROUTE");
            }
            DevLog.Log("SUPPRESSION EPISODE COMPLETED | " +
                        "Episodes: " + guardAI.highSuppressionEpisode +
                        " | Suppression: " + guardAI.suppressionLevel);
        }
    }
    private void UpdateTacticalThinking()
    {
        guardAI.tacticalThinkTimer += Time.deltaTime;

        if (guardAI.tacticalThinkTimer < guardAI.tacticalThinkInterval) { return; }

        guardAI.tacticalThinkTimer = 0f;
        UpdateTacticalGoal();
    }
    public void UpdateTacticalGoal()
    {
        currentTacticalGoal = EvaluateTacticalGoal();
        DevLog.Log("Observe2 currentTacticalGoal CURRENT TACTICAL GOAL : " + currentTacticalGoal);
        ExecuteTacticalGoal();
    }
    public void ExecuteTacticalGoal()
    {
        switch (currentTacticalGoal)
        {
            case TacticalGoal.Survive:
                //Debug.Log("CURRENT TACTICAL BEHAVIOR: SURVIVE");
                break;
            case TacticalGoal.ForcePlayerToMove:
                //Debug.Log("TACTICAL BEHAVIOR: Force Player To Move");
                break;
            case TacticalGoal.PinPlayer:
                //Debug.Log("TACTICAL BEHAVIOR: Pin Player");
                break;
            case TacticalGoal.GainBetterPosition:
                //Debug.Log("TACTICAL BEHAVIOR: Gain Better Position");
                break;
            case TacticalGoal.HuntPlayer:
                //Debug.Log("TACTICAL BEHAVIOR: Hunt Player");
                break;
        }
    }
    public TacticalGoal EvaluateTacticalGoal()
    {
        // =========================================================
        // 0–49 : DELIBERATE COMBAT / HUNT
        // =========================================================
        //Debug.Log("Suppression3 Come From Evaluate: " + guardAI.suppressionLevel);

        if (guardAI.needsEscapeRouteAfterSuppression)
        {
            DevLog.Log("TACTICAL GOAL | " +
                        "Second Suppression Episode -> " +
                        "ESCAPE ROUTE");
            return TacticalGoal.EscapeRoute;
        }
        if (guardAI.needsBetterPositionAfterSuppression)
        {
            DevLog.Log("TACTICAL GOAL | " +
                        "Post-Suppression Recovery ->" +
                        " Gain Better Position");
            return TacticalGoal.GainBetterPosition;
        }
        if (guardAI.suppressionLevel < 50f)
        {
            if (guardVision.playerDetected)
            {
                DevLog.Log("TACTICAL GOAL | Suppression < 50 " +
                    "Player Detected -> Pin Player");
                return TacticalGoal.PinPlayer;
            }
            if (guardVision.predictionCertainty >= 70f)
            {
                DevLog.Log("TACTICAL GOAL | Suppression < 50 | " +
                    "Player Unseen + Prediction High -> Hunt Player");
                return TacticalGoal.HuntPlayer;
            }
            DevLog.Log("TACTICAL GOAL | Suppression Level < 50 | " +
                "Player Unseen + Prediction Low -> PATROL");
            // Patrol is not a TacticalGoal yet.
            // Observe will handle this separately later.
            return TacticalGoal.Patrol;
        }
        // =========================================================
        // 50–99 : GAIN BETTER POSITION
        // =========================================================
        if (guardAI.suppressionLevel < 100f)
        {
            DevLog.Log("TACTICAL GOAL | Suppression Level 50-99 -> " +
                      "GAIN BETTER POSITION");
            return TacticalGoal.GainBetterPosition;
        }
        // =========================================================
        // 100–149 : SURVIVE
        // =========================================================
        if (guardAI.suppressionLevel < 150f)
        {
            DevLog.Log("TACTICAL GOAL | Suppression 100-149 -> " +
                        "SURVIVE");
            return TacticalGoal.Survive;
        }
        // =========================================================
        // 150–200 : FORCE MOVE
        // =========================================================
        DevLog.Log("TACTICAL GOAL | Suppression 150-200 -> " +
                    "FORCE MOVE");
        return TacticalGoal.ForcePlayerToMove;

    }
    
    public PatrolPoint GetNearestPatrolPoint(Vector3 position)
    {
        PatrolPoint nearestPoint = null;
        float nearestDistance = Mathf.Infinity;
        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;
            PatrolPoint patrolPoint = point.GetComponent<PatrolPoint>();
            float distance = Vector3.Distance(
                             position,
                             point.transform.position
                            );
            //Debug.Log("GET NEAREST POINT PATROL POINT :" + point.name + " && DistanceTo: " + distance + " < nearestDistance:" + nearestDistance);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = patrolPoint;

            }
        }

        return nearestPoint;
    }
    private void RememberTacticalPatrolNode(PatrolPoint point)
    {
        if(point == null) { return; }
        if (recentTacticalPatrolNodes.Contains(point))
        {
            //recentTacticalPatrolNodes.Remove(point);
            return;
        }
        recentTacticalPatrolNodes.Add(point);
        
        while (recentTacticalPatrolNodes.Count > maxRecentTacticalPatrolNodes)
        {
            recentTacticalPatrolNodes.RemoveAt(0);
        }
        DevLog.Log(
       "PATROL INSTINCT | PATROL MEMORY | RECENT TACTICAL NODE: " +
       point.name
        );
    }
    public bool IsRecentTacticalPatrolNode(PatrolPoint point)
    {
        if (point == null) { return false; }

        return recentTacticalPatrolNodes.Contains(point);
    }

    private Transform GetNextPatrolPointToward(Transform currentPoint, Vector3 targetPosition)
    {
        Transform bestPoint = null;

        float bestTargetDistance = Vector3.Distance(
                                    currentPoint.position,
                                    targetPosition
                                    );
        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;
            if (point == currentPoint) continue;

            float connectionDistance = Vector3.Distance(
                                        currentPoint.position,
                                        point.position
                                        );
            if (connectionDistance > patrolConnectionDistance) continue;

            float targetDistance = Vector3.Distance(
                                    point.position,
                                    targetPosition
                                    );
            if (targetDistance < bestTargetDistance)
            {
                bestTargetDistance = targetDistance;
                bestPoint = point;
            }
        }

        return bestPoint;
    }
    public void PrepareInvestigationRoute()
    {
        
        Vector3 bestLocation;
        investigationStartNode = GetNearestPatrolPoint(transform.position);


        if (guardVision.bestAmbushTile != null)
        {
            DevLog.Log("pCertainty N AMBUSH bestAmbushTile Site: " + guardVision.bestAmbushTile.name);
            bestLocation = guardVision.bestAmbushTile.transform.position;
        }
        else
        {

            bestLocation = guardVision.lastKnownPosition;
        }
        investigationTargetNode = GetNearestPatrolPoint(bestLocation);
        investigationRoute = BuildPatrolRoute(investigationStartNode, investigationTargetNode);
        investigationRouteIndex = 0;
        if (investigationRoute.Count > 0)
        {
            isFollowingInvestigationRoute = true;
            
            //Debug.Log("INVESTIGATION ROUTE READY | " +
            //          "START: " + investigationStartNode.name +
            //           " | TARGET: " + investigationTargetNode.name +
            //           " | NODES: " + investigationRoute.Count);
        }
        else
        {
           
            isFollowingInvestigationRoute = false;
            //Debug.Log("INVESTIGATION ROUTE FAILED!");
        }

    }
    public void FollowInvestigationRoute()
    {


        if (!isFollowingInvestigationRoute) return;

        if ((investigationRoute == null) || (investigationRoute.Count == 0))
        {
            isFollowingInvestigationRoute = false;
            return;
        }
        //guardVision.viewDistance = 28f;
        if (isCheckingPatrolCover)
        {
            Stop();
            if (patrolCheckPoint != null)
            {

               RotationTo(patrolCheckPoint.position);
            }
            patrolLookTimer += Time.deltaTime;
            if (patrolLookTimer < patrolLookDuration) return;
            DevLog.Log("RECENTLY LOOK :" + patrolCheckPoint.name);
            recentlyCheckedPatrolCovers.Add(patrolCheckPoint);
            if (recentlyCheckedPatrolCovers.Count > maxRecentPatrolChecks)
            {
                recentlyCheckedPatrolCovers.RemoveAt(0);
            }
            patrolCheckPoint = null;
            patrolLookTimer = 0f;
            isCheckingPatrolCover = false;
            return;
        }
        if (investigationRouteIndex >= investigationRoute.Count)
        {
            isFollowingInvestigationRoute = false;

            //if (!guardVision.finalAmbushSetUp)
            //{
            isMovingToExactInvestigationPosition = true;
            //}
            //Debug.Log("Investigation Matrix Route Complete:" +
            //               "NOW MOVING TO EXACT POSITION: " + guardVision.lastKnownPosition);
            return;
        }
        PatrolPoint targetNode = investigationRoute[investigationRouteIndex];
        if (targetNode == null)
        {
            investigationRouteIndex++;
            return;
        }

        patrolCheckTimer += Time.deltaTime;
        if (patrolCheckTimer >= patrolCheckInterval)
        {
            patrolCheckTimer = 0f;
            patrolCheckPoint = guardCover.ChoosePatrolCheckPoint(
                transform.position,
                patrolCoverCheckRadius,
                recentlyCheckedPatrolCovers,
                guardAI.coverPoints
                );
            if (patrolCheckPoint != null)
            {
                isCheckingPatrolCover = true;
                patrolLookTimer = 0f;

            }
        }
        bool arrived = MoveTo(targetNode.transform.position);
        //Debug.Log("INVESTIGATION ROUTE READY OUTER: ARRIVED: " + arrived + " TARGETNODE: " + targetNode.name);
        if (arrived)
        {
            guardVision.finalAmbushSetUp = false;
            //Debug.Log("INVESTIGATION ROUTE READY ARRIVED AT: " + targetNode.name);
            investigationRouteIndex++;

        }

    }

   
    public List<PatrolPoint> BuildPatrolRoute(PatrolPoint start, PatrolPoint target)
    {
        List<PatrolPoint> result = new List<PatrolPoint>();
        if (start == null || target == null) return result;

        Queue<PatrolPoint> frontier = new Queue<PatrolPoint>();
        Dictionary<PatrolPoint, PatrolPoint> cameFrom = new Dictionary<PatrolPoint, PatrolPoint>();

        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            PatrolPoint current = frontier.Dequeue();
            if (current == target) break;

            foreach (PatrolPoint next in current.connectedPoints)
            {
                if (next == null) continue;
                if (cameFrom.ContainsKey(next)) continue;

                frontier.Enqueue(next);
                cameFrom[next] = current;

            }

        }
        if (!cameFrom.ContainsKey(target))
        {
            //Debug.Log("NO PATROL ROUTE FOUND!");
            return result;
        }
        PatrolPoint routeNode = target;
        while (routeNode != null)
        {
            result.Add(routeNode);
            routeNode = cameFrom[routeNode];
        }
        result.Reverse();

        return result;
    }
    public void MoveToExactInvestigationPosition()
    {
        if (!isMovingToExactInvestigationPosition) return;

        bool arrived = MoveTo(guardVision.lastKnownPosition);
        //Debug.Log("INVESTIGATION ROUTE READY INVESTIGATION EXACT POSITIION | " +
        //           "TARGET: " + guardVision.lastKnownPosition +
        //           " | ARRIVED: " + arrived);
        if (arrived)
        {
            isMovingToExactInvestigationPosition = false;
            investigationRoutePrepared = false;
            DevLog.Log("For fire INVESTIGATION ROUTE READY INVESTIGATION EXACT POSITION FINALLY!");
            BeginSearch();
        }
    }
    private PatrolPoint GetNextPatrolPoint(PatrolPoint currentPoint)
    {
        if (currentPoint == null) return null;
        if (currentPoint.connectedPoints.Count == 0) return null;

        List<PatrolPoint> possiblePoints = new List<PatrolPoint>();

        foreach (PatrolPoint point in currentPoint.connectedPoints)
        {
            if (point == null) continue;
            if (point == previousPatrolNode) continue;
            possiblePoints.Add(point);
            
        }
        if (possiblePoints.Count == 0)
        {
            return previousPatrolNode;
        }
        DevLog.Log("PATROL NODE :possiblePoints.Count: " + possiblePoints.Count);

        PatrolPoint tacticalPoint =
            guardVision.GetBestTacticalPatrolPoint(possiblePoints);

        if (tacticalPoint != null)
        {
            DevLog.Log(
                "PATROL INSTINCT | " +
                "TACTICAL NEXT POINT : " +
                tacticalPoint.name
            );
            RememberTacticalPatrolNode(tacticalPoint);

            return tacticalPoint;
        }
        int randomIndex = Random.Range(0, possiblePoints.Count);


        return possiblePoints[randomIndex];
    }
    private void DebugPatrolRoute()
    {
        string previousName = previousPatrolNode != null ? previousPatrolNode.name : "NONE";
        string currentName = currentPatrolNode != null ? currentPatrolNode.name : "NONE";
        string nextName = nextPatrolNode != null ? nextPatrolNode.name : "NONE";
        DevLog.Log("PATROL NODE MATRIX2 POSITION | " +
                  "PREVIOUS: " + previousName +
                  " -> CURRENT: " + currentName +
                  " -> NEXT: " + nextName);


    }
    public void PatrolGraphMovement()
    {
        
        if (isCheckingPatrolCover)
        {
            Stop();
            if (patrolCheckPoint != null)
            {

                RotationTo(patrolCheckPoint.position);
            }
            patrolLookTimer += Time.deltaTime;
            if (patrolLookTimer < patrolLookDuration) return;
            DevLog.Log("RECENTLY LOOK :" + patrolCheckPoint.name);
            recentlyCheckedPatrolCovers.Add(patrolCheckPoint);
            if (recentlyCheckedPatrolCovers.Count > maxRecentPatrolChecks)
            {
                recentlyCheckedPatrolCovers.RemoveAt(0);
            }
            patrolCheckPoint = null;
            patrolLookTimer = 0f;
            isCheckingPatrolCover = false;
            return;
        }
        if (isWaitingAtPatrolNode)
        {
            Stop();
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer < 2f) return;
            patrolWaitTimer = 0f;
            isWaitingAtPatrolNode = false;

            if (nextPatrolNode != null)
            {
                RotationTo(nextPatrolNode.transform.position);
            }


        }

        if (currentPatrolNode == null)
        {
            currentPatrolNode = GetNearestPatrolNode(transform.position);
            return;
        }
        if (nextPatrolNode == null)
        {
            nextPatrolNode = GetNextPatrolPoint(currentPatrolNode);
            return;
        }

        patrolCheckTimer += Time.deltaTime;
        if (patrolCheckTimer >= patrolCheckInterval)
        {
            patrolCheckTimer = 0f;
            patrolCheckPoint = guardCover.ChoosePatrolCheckPoint(
                transform.position,
                patrolCoverCheckRadius,
                recentlyCheckedPatrolCovers,
                guardAI.coverPoints
                );
            if (patrolCheckPoint != null)
            {
                isCheckingPatrolCover = true;
                patrolLookTimer = 0f;

            }
        }
        //guardVision.CanGuardSeePlayerFromPoint(nextPatrolNode);
        //guardVision.CanPlayerSeeGuardFromPoint(nextPatrolNode);
        //guardVision.EvaluateTacticalVisibility(nextPatrolNode);
        //guardVision.RecordTacticalVisibility(nextPatrolNode);
        bool arrived = MoveTo(nextPatrolNode.transform.position);

        DevLog.Log("PATROL NODE ARRIVED:" + arrived + " currentPatrolNode: " + currentPatrolNode.name + " nextPatrolNode: " + nextPatrolNode.name);
        if (arrived)
        {

            isWaitingAtPatrolNode = true;

            searchPoint = currentPatrolNode.transform;
            searchPhase = SearchPhase.PauseAndListen;
            currentState =GuardState.SearchPatrol ;
            previousPatrolNode = currentPatrolNode;
            currentPatrolNode = nextPatrolNode;
            
            nextPatrolNode = GetNextPatrolPoint(currentPatrolNode);
            DevLog.Log("PATROL NODE ARRIVED AT: " + nextPatrolNode.name);

            if (nextPatrolNode != null)
            {
                DevLog.Log("PATROL NODE NEXT DESTINATION: " + nextPatrolNode.name);
            }
            //DebugPatrolRoute();

            return;




        }

    }
    public void BuildPatrolMatrix()
    {
        PatrolPoint[] allPatrolPoints =
            FindObjectsOfType<PatrolPoint>();

        foreach (PatrolPoint point in allPatrolPoints)
        {
            if (point == null) continue;

            point.BuildConnection(allPatrolPoints);


        }
        DevLog.Log("PATROL NODE MATRIX BUILT | TOTAL POINTS: " + allPatrolPoints.Length);

    }
    private PatrolPoint GetNearestPatrolNode(Vector3 position)
    {
        PatrolPoint[] allPoints = FindObjectsOfType<PatrolPoint>();
        PatrolPoint nearestPoint = null;
        float nearestDistance = float.MaxValue;
        foreach (PatrolPoint point in allPoints)
        {
            DevLog.Log(" ");
            if (point == null) continue;
            //Debug.Log("PATROL NODE :" + point.name);
            float distance = Vector3.Distance(
                             position,
                             point.transform.position
                            );
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = point;
            }
        }


        return nearestPoint;
    }
    private Vector3 verticalScanPosition(VerticalScan verticalPosition, Vector3 lookPosition, SearchPhase searchPosition, Transform searchCover)
    {


        switch (verticalPosition)
        {
            //case VerticalScan.Down:
            ////target = searchPoint.position - searchPoint.right * 3f;
            //target = lookPosition;
            //target.y -= 30f;
            //searchNextPhase = true;
            //verticalScan = VerticalScan.Up;
            //break;
            case VerticalScan.Middle:
                target = lookPosition;
                //target.y = 0;
                searchPhase = searchPosition;
                searchNextPhase = true;
                verticalScan = VerticalScan.Up;
                break;
            case VerticalScan.Up:
                target = lookPosition;
                //target.y += 15f;
                target.y += 3f;
                searchPhase = searchPosition;
                verticalScan = VerticalScan.Middle;
                searchNextPhase = false;
                break;

        }
        DevLog.Log("Vertical Search Cover: " + searchCover.name + " Vertical scanPosition: " + searchPosition + " verticalScan: " + verticalPosition);
        return target;
    }
    public void UpdateSearch()
    {
        if (guardVision.playerDetected)
        {
            isSearchingInCoverTrue = true;
            //Debug.Log("For fire Player Detected!");
            //Debug.Log("Player Detected!");
            EndSearch(true);
            return;
        }
        else
        {
            if (isSearchingInCover)
            {
                isSearchingInCoverTrue = false;
                //Debug.Log("Vertical isSearchingInCoverTrue");

            }

        }


        switch (searchPhase)
        {
            case SearchPhase.LookAtLikelyDirection:
                Stop();
                RotationTo(transform.position + likelySearchDirection * 5f);
                searchPauseTimer += Time.deltaTime;
                if (searchPauseTimer >= searchLookDuration)
                {
                    searchPauseTimer = 0f;
                    searchPoint = guardCover.ChooseSearchPoint(
                        guardVision.lastKnownPosition,
                        likelySearchDirection,
                        searchedPoints,
                        guardAI.coverPoints
                    );

                    searchPhase = searchPoint != null
                        ? SearchPhase.MoveToSearchPoint
                        : SearchPhase.SearchFailed;
                }
                break;

            case SearchPhase.MoveToSearchPoint:
                if (searchPoint == null)
                {
                    searchPhase = SearchPhase.SearchFailed;
                    break;
                }

                MoveTo(searchPoint.position);
                //Debug.Log("For fire MoveToSearch!" + Vector3.Distance(transform.position, searchPoint.position) + " < 1.5f ");
                if (Vector3.Distance(transform.position, searchPoint.position) < 2f)
                {
                    Stop();
                    searchPauseTimer = 0f;
                    searchPhase = SearchPhase.PauseAndListen;
                }
                break;

            case SearchPhase.PauseAndListen:
                Stop();

                searchPauseTimer += Time.deltaTime;
                if (searchPauseTimer >= searchPauseDuration)
                {

                    searchScanTimer = 0f;
                    verticalScan = VerticalScan.Up;
                    target = searchPoint.position - searchPoint.right * 3f;
                    baseTarget = target;
                    target = target + guardVision.lastKnownPosition; //care
                    OriginalBaseTarget = baseTarget;
                    searchPhase = SearchPhase.ScanLeft;
                    //ResetVerticalLook();
                }
                //guardVision.viewDistance = 38f;

                DevLog.Log("For fire Pause&Listen!");
                break;

            case SearchPhase.ScanLeft:
                DevLog.Log("For fire ScanLeft!");
                RotationTo(target);

                LookVertical(target);


                searchScanTimer += Time.deltaTime;
                if (searchScanTimer >= searchScanDuration)
                {
                    searchScanTimer = 0f;
                    if (searchNextPhase)
                    {

                        target = guardVision.lastKnownPosition;
                        searchNextPhase = false;
                        baseTarget = target;

                        searchPhase = SearchPhase.ScanCenter;
                        if ((!isSearchingInCoverTrue) && (isPeekingSide))
                        {

                            guardCover.RegisterFailedPeek();

                            DevLog.Log("Vertical None Left ");
                        }
                        //ResetVerticalLook();
                        return;
                    }
                    target = verticalScanPosition(verticalScan, baseTarget, searchPhase, searchPoint);
                    target = target + guardVision.lastKnownPosition; //care

                }

                break;

            case SearchPhase.ScanCenter:
                DevLog.Log("For fire ScanCenter!");
                RotationTo(target);

                LookVertical(target);




                searchScanTimer += Time.deltaTime;
                if (searchScanTimer >= searchScanDuration)
                {
                    searchScanTimer = 0f;
                    if (searchNextPhase)
                    {

                        target = searchPoint.position + searchPoint.right * 3f;
                        baseTarget = target;
                        target = target + guardVision.lastKnownPosition; //care
                        searchNextPhase = false; searchPhase = SearchPhase.ScanRight;
                        if ((!isSearchingInCoverTrue) && (isPeekingSide))
                        {
                            DevLog.Log("Vertical None Center");
                            guardCover.RegisterFailedPeek();

                        }
                        //ResetVerticalLook();
                        return;
                    }
                    target = verticalScanPosition(verticalScan, baseTarget, searchPhase, searchPoint);
                    target = target + guardVision.lastKnownPosition; //care

                }

                break;

            case SearchPhase.ScanRight:
                DevLog.Log("For fire ScanRight!");
                RotationTo(target);
                LookVertical(target);





                searchScanTimer += Time.deltaTime;
                if (searchScanTimer >= searchScanDuration)
                {
                    searchScanTimer = 0f;


                    if (searchNextPhase)
                    {
                        ResetVerticalLook();
                        if (
                            (isSearchingInCover) &&
                            (currentState !=GuardState.HideInCover )
                            )
                        {

                            currentState = GuardState.ReturnToCover;
                            isSearchingInCover = false;
                            if ((!isSearchingInCoverTrue) && (isPeekingSide))
                            {
                                DevLog.Log("Vertical None Right");
                                guardCover.RegisterFailedPeek();

                            }
                        }
                        else if(currentState == GuardState.HideInCover)
                        {

                            if (guardVision.lastKnownPosition != Vector3.zero)
                            {
                                guardVision.hasStrikeRouteRecord = false;
                                isWaitingAtStrikePoint = true;
                                currentState = GuardMovement.GuardState.Strike;
                                return;
                            }
                            guardAI.currentCover = null;
                            currentState = GuardState.Patrol;
                        }
                        else if (isWaitingAtPatrolNode)
                        {

                            currentState = GuardState.Patrol;
                            //Debug.Log("Vertical continue Patrol!");
                        }
                        else
                        {
                            searchedPoints.Add(searchPoint);
                            searchAttempts++;
                            PickNextSearchPointOrEnd();

                            target = Vector3.zero;
                        }
                        
                        searchNextPhase = false;
                        return;
                    }
                    target = verticalScanPosition(verticalScan, baseTarget, searchPhase, searchPoint);
                    target = target + guardVision.lastKnownPosition; //care



                }
                break;

            case SearchPhase.SearchFailed:
                currentState = GuardState.Patrol;
                EndSearch(false);
                break;
        }
    }

    void PickNextSearchPointOrEnd()
    {
        if (searchAttempts >= maxSearchAttempts)
        {
            guardVision.viewDistance = 18f;
            searchPhase = SearchPhase.SearchFailed;
            thinkingRouteTimer = 0f;
            return;
        }

        guardVision.locationCertainty -= 10f;
        guardVision.locationCertainty = Mathf.Clamp(
            guardVision.locationCertainty,
            0f,
            100f
        );

        CoverPoint currentSearchCover = searchPoint != null
            ? searchPoint.GetComponent<CoverPoint>()
            : null;

        searchPoint = guardCover.ChooseConnectedSearchPoint(
            currentSearchCover,
            guardVision.lastKnownPosition,
            likelySearchDirection,
            searchedPoints,
            guardAI.coverPoints
        );

        if (searchPoint == null)
        {
            searchPhase = SearchPhase.SearchFailed;
        }
        else
        {
            searchPauseTimer = 0f;
            searchScanTimer = 0f;
            searchPhase = SearchPhase.MoveToSearchPoint;
            DevLog.Log("SEARCH NEXT POINT : " + searchPoint.name + " attempt " + searchAttempts);
        }
    }

    public void EndSearch(bool foundPlayer)
    {
        isSearching = false;
        searchPoint = null;
        searchedPoints.Clear();
        searchPauseTimer = 0f;
        searchScanTimer = 0f;
        ResetVerticalLook();
       
        if (foundPlayer)
        {
            //guardVision.locationCertainty = Mathf.Max(guardVision.locationCertainty, 70f);
            //UpdateTacticalGoal();
            //currentCover = guardCover.ChooseBestCover(coverPoints);
            //if (currentCover != null)
            //{
            //    currentState = GuardState.MoveToCover;
            //}
            //else
            //{
            //    currentState = GuardState.Patrol;
            //}
            //Debug.Log("SEARCH ENDED: Player found. Handing off to tactical brain.");
            //return;
        }

        //guardVision.locationCertainty -= 5f * searchAttempts;
        //guardVision.locationCertainty = Mathf.Clamp(
        //    guardVision.locationCertainty,
        //    0f,
        //    100f
        //);
        //UpdateTacticalGoal();

        //if (guardVision.locationCertainty >= 70f)
        //{
        //    currentState = GuardState.Patrol;
        //}
        //else if (guardVision.locationCertainty >= 30f)
        //{
        //    currentIntent = HunterIntent.Relocate;
        //    currentCover = guardCover.ChooseBestCover(coverPoints);
        //    currentState = currentCover != null
        //        ? GuardState.MoveToCover
        //        : GuardState.Patrol;
        //}
        //else
        //{
        //    currentIntent = HunterIntent.Bait;
        //    currentState = GuardState.Patrol;
        //}

        //Debug.Log(
        //    "SEARCH ENDED: Failed. Certainty=" +
        //    guardVision.locationCertainty +
        //    " Intent=" +
        //    guardAI.currentIntent
        //);
    }
    public void BeginSearch()
    {
        currentState = GuardState.Search;
        isSearching = true;
        searchPhase = SearchPhase.LookAtLikelyDirection;
        searchAttempts = 0;
        searchedPoints.Clear();
        searchPoint = null;
        searchPauseTimer = 0f;
        searchScanTimer = 0f;

        likelySearchDirection = guardVision.lastKnownPosition - transform.position;
        likelySearchDirection.y = 0f;
        if (likelySearchDirection.sqrMagnitude < 0.01f)
        {
            likelySearchDirection = transform.forward;
        }
        else
        {
            likelySearchDirection.Normalize();
        }

        Stop();
        //Debug.Log("SEARCH BEGUN at " + guardVision.lastKnownPosition);
    }
    public void AddPatrolSuspicion(float amount, Vector3 position)
    {
        patrolSuspicion += amount;
        patrolSuspicion = Mathf.Clamp(
                          patrolSuspicion,
                          0f,
                          100f
                            );
        suspiciousPosition = position;

        //Debug.Log("PATROL SUSPICION : " + patrolSuspicion +
        //          " | POSITION:" + suspiciousPosition);

    }
    public void HearGrassNoise(Vector3 grassPosition, float amount, string grassName)
    {
        float distanceToGrass = Vector3.Distance(transform.position, grassPosition);
        if (distanceToGrass > guardVision.hearingDistance) return;

        AddPatrolSuspicion(amount, grassPosition);
        GrassArea[] allGrass = FindObjectsOfType<GrassArea>();
        foreach (GrassArea grass in allGrass)
        {
            if (Vector3.Distance(grass.transform.position, grassPosition) < 0.5f)
            {
                suspiciousGrass = grass;
                DevLog.Log(
                "GRASS RESPONSE GUARD HEARD WEAK GRASS NOISE | " +
                grassPosition +
                " | SUSPICION POINTS: " +
                patrolSuspicion +
                " Grass Name:" +
                suspiciousGrass.name
                );
                break;
            }
        }

        if (suspiciousGrass != null)
        {
            suspiciousGrassRegistered = false;
            suspiciousGrassLookTimer = 0f;

            currentState = GuardState.CheckSuspiciousGrass;
            guardVision.ResetTacticalHypothesis();
        }
        if (patrolSuspicion < 20) currentGrassResponse = GrassResponse.Ignore;
        if ((patrolSuspicion > 20) && (patrolSuspicion < 40)) currentGrassResponse = GrassResponse.Observe;
        if ((patrolSuspicion > 40)) currentGrassResponse = GrassResponse.SuppressFire;
        patrolSuspicion = 0f;
        //if ((patrolSuspicion > 60)) currentGrassResponse = GrassResponse.SuppressFire;
        //currentGrassResponse = GrassResponse.MoveToCover;
    }
    public float GetPatrolCheckInterval()
    {
        if (patrolSuspicion >= 30)
        {
            return suspiciousPatrolCheckInterval;
        }
        return normalPatrolCheckInterval;

    }
    public void CheckForMovingGrass()
    {
        foreach (GrassArea grass in grassAreas)
        {
            if (grass == null) continue;
            if (!grass.isMoving) continue;

            float distance = Vector3.Distance(
                            transform.position,
                            grass.transform.position
                            );
            if (distance > grassNoticeDistance) continue;
            suspiciousGrass = grass;
            suspiciousGrassLookTimer = 0f;
            suspiciousGrassRegistered = false;

            currentState = GuardState.CheckSuspiciousGrass;

            DevLog.Log("GUARD WILL CHECK SUSPICIOUS GRASS: " + grass.gameObject.name);
            DevLog.Log("GUARD NOTICED MOVING GRASS : " + grass.gameObject.name);


        }

    }
    void UpdateIntent()
    {

        if (guardVision.playerCanSeeMe || guardVision.playerDetected)
        { //OR PLAYER CAN SEE ME
          //guardVision.locationCertainty = 100f;
          //guardVision.exposureCertainty += 30f * Time.deltaTime;


            //guardVision.locationCertainty = 80f;
            //guardVision.exposureCertainty = 25f;
            //Debug.Log("PredictionCertainty 2:" + guardVision.predictionCertainty);
            //exposureMemoryTimer = 20f;

            //guardVision.guardCertaintyConfidence(guardVision.playerCanSeeMe);

            //guardVision.concealScore += 100f;
            //guardVision.pressureScore += guardVision.exposureCertainty;

            //certainty += certaintyGainSight * Time.deltaTime;

        }
        if (guardVision.heardGunshot)
        {
            //certainty += certaintyGainSound * Time.deltaTime;

        }

        if ((guardVision.playerDetected || guardVision.playerCanSeeMe) == false)
        {


            //guardVision.locationCertainty -= 15f * Time.deltaTime;
            //guardVision.locationCertainty = Mathf.Clamp(
            //    guardVision.locationCertainty,
            //    0f,
            //    100f
            //    );
            //if (guardVision.locationCertainty < 30)
            //{
            //    guardVision.baitScore += (100f - guardVision.locationCertainty);
            //}
            //else if (guardVision.locationCertainty > 30f && guardVision.locationCertainty < 70f)
            //{
            //    guardVision.relocateScore += 40f;
            //}
            //else if (guardVision.locationCertainty > 70f) {
            //    guardVision.huntScore += 50f;
            //}
            //    exposureMemoryTimer -= 1f * Time.deltaTime;
            //exposureMemoryTimer = Mathf.Clamp(
            //    exposureMemoryTimer,
            //    0f,
            //    20f
            //    );
            //if(exposureMemoryTimer <= 0) {
            //    exposureMemoryTimer = 0f;
            //    guardVision.exposureCertainty -= 10f * Time.deltaTime;
            //    guardVision.exposureCertainty = Mathf.Clamp(
            //        guardVision.exposureCertainty,
            //        0f,
            //        100f
            //        );


            //}
            //guardVision.guardCertaintyConfidence(false);
            //Debug.Log("look Time:" + exposureMemoryTimer + " exposure Certainty:" + guardVision.exposureCertainty + " " + guardVision.playerCanSeeMe + " " + guardVision.heardGunshot);
        }

        DevLog.Log("Certainty:" + certainty);


        //suppressionLevel -= suppressionLevel * Time.deltaTime;
        ////suppressionLevel -= Time.deltaTime;
        //if (suppressionLevel < 0.01f) {
        //    suppressionLevel = 0f;

        //}
        //suppressionLevel = Mathf.Clamp(
        //    suppressionLevel,
        //    0f,
        //    maxSuppression
        //    );



        DevLog.Log(" : Suppression Level:" + guardAI.suppressionLevel);
        //if (guardVision.playerCanSeeMe) {
        //    if (suppressionLevel > 70)
        //    {
        //        guardVision.relocateScore += suppressionLevel;
        //        //currentIntent = HunterIntent.Relocate;

        //    }
        //    else {
        //        //currentIntent = HunterIntent.Pressure;
        //    }
        //    return;
        //}
        timeSinceLastShot += Time.deltaTime;
        if (timeSinceLastShot > 25f)
        {
            timeSinceLastShot = 25;
            //currentIntent = HunterIntent.Bait;
            //timeSinceLastShot = Mathf.Clamp(timeSinceLastShot, 0f, 25f);
        }

        //currentIntent = HunterIntent.Conceal;

    }
    private PatrolPoint GetBestEscapePatrolPoint()
    {
        PatrolPoint[] allPoints = FindObjectsOfType<PatrolPoint>();

        PatrolPoint bestPoint = null;
        float bestDistance = float.MaxValue;

        foreach (PatrolPoint point in allPoints)
        {
            if (point == null) { continue; }

            if (!point.isHiddenPatrolPoint) { continue; }

            if (checkedEscapePatrolPoint.Contains(point)) { continue; }



            float distance = Vector3.Distance(
                transform.position,
                point.transform.position
                );
            if (distance < bestDistance)
            {

                bestDistance = distance;
                bestPoint = point;
            }
        }
        if (bestPoint != null)
        {
            DevLog.Log("ESCAPE ROUTE | HIDDEN PATROL POINT FOUND: " + bestPoint.name
                       + " | Distance: " + bestDistance
                );

        }
        else
        {
            DevLog.Log("ESCAPE ROUTE | NO HIDDEN PATROL POINT FOUND!");

        }

        return bestPoint;
    }
    public void FollowEscapeRoute()
    {
        if (!isFollowingEscapeRoute) { return; }
        if ((escapeRoute == null) || (escapeRoute.Count == 0))
        {
            isFollowingEscapeRoute = false;
            return;
        }
        if (isCheckingPatrolCover)
        {
            Stop();

            if (patrolCheckPoint != null)
            {
                RotationTo(patrolCheckPoint.position);
            }
            patrolLookTimer += Time.deltaTime;
            if (patrolLookTimer < patrolLookDuration)
            {
                return;
            }
            DevLog.Log("ESCAPE ROUTE | PATROL COVER CHECKED: " + patrolCheckPoint.name);
            recentlyCheckedPatrolCovers.Add(patrolCheckPoint);
            if (recentlyCheckedPatrolCovers.Count > maxRecentPatrolChecks)
            {
                recentlyCheckedPatrolCovers.RemoveAt(0);
            }
            patrolCheckPoint = null;
            patrolLookTimer = 0f;
            isCheckingPatrolCover = false;
            return;
        }
        if (escapeRouteIndex >= escapeRoute.Count)
        {
            isFollowingEscapeRoute = false;
            isWaitingAtEscapePoint = true;
            escapeWaitTimer = 0f;
            
            Stop();
            DevLog.Log("ESCAPE ROUTE | ARRIVED AT HIDDEN PATROL POINT: " + escapeTargetNode.name);
            return;
        }
        PatrolPoint targetNode = escapeRoute[escapeRouteIndex];
        if (targetNode == null)
        {
            escapeRouteIndex++;
            return;
        }
        patrolCheckTimer += Time.deltaTime;

        if (patrolCheckTimer >= patrolCheckInterval)
        {
            patrolCheckTimer = 0f;

            patrolCheckPoint = guardCover.ChoosePatrolCheckPoint(
                transform.position,
                patrolCoverCheckRadius,
                recentlyCheckedPatrolCovers,
                guardAI.coverPoints
                );
            if (patrolCheckPoint != null)
            {
                isCheckingPatrolCover = true;
                patrolLookTimer = 0f;
                return;
            }
        }
        bool arrived = MoveTo(targetNode.transform.position);
        DevLog.Log("ESCAPE ROUTE | MOVING TO: " + targetNode.name
                   + " | ARRIVED:  " + arrived);
        if (arrived)
        {
            DevLog.Log("ESCAPE ROUTE | ARRIVED AT NODE : " + targetNode.name);
            guardVision.RecordEscapeRouteNode(targetNode);
            escapeRouteIndex++;
        }
    }
    public void PrepareEscapeRoute()
    {
        escapeStartNode = GetNearestPatrolPoint(transform.position);
        escapeTargetNode = GetBestEscapePatrolPoint();
        if (escapeStartNode == null)
        {
            isFollowingEscapeRoute = false;
            DevLog.Log("ESCAPE ROUTE FAILED | " +
                        "NO START PATROL POINT");
            return;
        }
        if (escapeTargetNode == null)
        {
            isFollowingEscapeRoute = false;
            DevLog.Log("ESCAPE ROUTE FAILED | " +
                        "NO HIDDEN PATROL POINT");
            return;
        }
        escapeRoute = BuildPatrolRoute(
            escapeStartNode,
            escapeTargetNode
            );
        escapeRouteIndex = 0;

        if (escapeRoute.Count > 0)
        {
            isFollowingEscapeRoute = true;
            DevLog.Log("ESCAPE ROUTE READY | "
                    + " START: " + escapeStartNode.name
                    + " | TARGET: " + escapeTargetNode.name
                    + " | NODES: " + escapeRoute.Count);
        }
        else
        {
            isFollowingEscapeRoute = false;
            DevLog.Log("ESCAPE ROUTE FAILED | " +
                        "NO PATROL ROUTE FOUND");
        }
    }
    public void HandleEscapePointWait()
    {
        if (!isWaitingAtEscapePoint)
        {
            return;
        }
        Stop();
        escapeWaitTimer += Time.deltaTime;
        if(escapeWaitTimer < escapeWaitDuration)
        {
            return;
        }
        escapeWaitTimer = 0f;
        isWaitingAtEscapePoint = false;
        DevLog.Log("ESCAPE ROUTE | HIDDEN POINT WAIT COMPLETE");
        PatrolPoint escapeNode;
        if (!guardVision.playerCanSeeMe)
        {
            DevLog.Log("ESCAPE ROUTE | PLAYER CANNOT SEE GUARD");
            escapeNode = GetFinalEscapeRouteNode();

            if (escapeNode != null)
            {
                guardVision.RecordTacticalPossibilitiesFromEscapeNode(escapeNode);
            }
            guardAI.needsEscapeRouteAfterSuppression = false;
            //escapeRouteCompleted = true;

            //guardAI.currentCover = guardCover.ChooseBestCover(guardAI.coverPoints);
            //currentState = GuardState.MoveToCover;
            DevLog.Log("ESCAPE ROUTE "+ escapeNode.name + " | HIDDEN SUCCESS  |TACTICAL HYPOTHESIS READY | WAITING FOR POST-ESCAPE DECISION ");

        }
        else
        {
            DevLog.Log("ESCAPE ROUTE | PLAYER CAN STILL SEE GUARD");
            if (!checkedEscapePatrolPoint.Contains(escapeTargetNode))
            {
                checkedEscapePatrolPoint.Add(escapeTargetNode);
                DevLog.Log("ESCAPE ROUTE | TESTED HIDDEN POINT RECORDED: "+ escapeTargetNode.name);
            }
            RetryEscapeRoute();
        }
    }
    private void RetryEscapeRoute()
    {
        isFollowingEscapeRoute = false;
        isWaitingAtEscapePoint = false;

        escapeRoute.Clear();
        escapeRouteIndex = 0;

        DevLog.Log("ESCAPE ROUTE | RETRYING WITH ANOTHER HIDDEN POINT");
        PrepareEscapeRoute();

    }
    public void SetPostEscapeRoute(List<PatrolPoint> route)
    {
        postEscapeRoute.Clear();
        if(route == null) {
            //Debug.Log("POST ESCAPE ROUTE | NULL ROUTE");
            return; }
        foreach (PatrolPoint point in route)
        {
            if(point == null)
            {
                continue;
            }
            postEscapeRoute.Add(point);
        }
        DevLog.Log(
        "ESCAPE ROUTE | TACTICAL POSSIBILITIES | POST ESCAPE ROUTE | STORED | NODES: " +
        postEscapeRoute.Count
        );

    }
    public void SetIndirectionInvestigationRoute(List<PatrolPoint> route)
    {
        indirectionInvestigationRoute.Clear();

        if (route == null)
        {
           // Debug.Log(
           //"INDIRECTION INVESTIGATION ROUTE | NULL ROUTE"
           // );
            return;
        }
        foreach (PatrolPoint point in route)
        {
            if (point == null)
            {
                continue;
            }
            indirectionInvestigationRoute.Add(point);
        }
        indirectionInvestigationRouteIndex = 0;
        if (indirectionInvestigationRoute.Count>0)
        {
            isFollowingIndirectionInvestigationRoute = true;
        }
        DevLog.Log(
        "ESCAPE ROUTE | TACTICAL POSSIBILITIES | INDIRECTION INVESTIGATION ROUTE | STORED | NODES: " +
        indirectionInvestigationRoute.Count
        );
        

    }
    public void SetIndirectionStrikeRoute(List<PatrolPoint> route)
    {
        indirectionStrikeRoute.Clear();

        if (route == null)
        {
            DevLog.Log(
           "INDIRECTION INVESTIGATION ROUTE | NULL ROUTE"
            );
            return;
        }
        foreach (PatrolPoint point in route)
        {
            if (point == null)
            {
                continue;
            }
            indirectionStrikeRoute.Add(point);
        }
        indirectionStrikeRouteIndex = 0;
        isFollowingIndirectionStrikeRoute = false;
        if (indirectionStrikeRoute.Count > 0)
        {
            isFollowingIndirectionStrikeRoute = true;
        }
        DevLog.Log(
        "STRIKE MEMORY | TACTICAL POSSIBILITIES | INDIRECTION STRIKE ROUTE | STORED | NODES: " +
        indirectionStrikeRoute.Count
        );
        

    }
    public void FollowIndirectionStrikeRoute()
    {
        if (!isFollowingIndirectionStrikeRoute)
        {
            return;
        }
        if ((indirectionStrikeRoute == null) ||
            (indirectionStrikeRoute.Count == 0))
        {
            isFollowingIndirectionStrikeRoute = false;
            DevLog.Log(
           "STRIKE MEMORY | INDIRECTION STRIKE ROUTE | " +
           "NO ROUTE"
       );

            return;
        }
        // =========================================================
        // PATROL COVER CHECK
        // =========================================================
        if (isCheckingPatrolCover)
        {
            Stop();
            if (patrolCheckPoint != null)
            {
                RotationTo(patrolCheckPoint.position);
            }
            patrolLookTimer += Time.deltaTime;

            if (patrolLookTimer < patrolLookDuration)
            {
                return;
            }
            DevLog.Log(
           "STRIKE MEMORY | TACTICAL POSSIBILITIES | " +
           "PATROL COVER 2 CHECKED: " +
           patrolCheckPoint.name
       );
            recentlyCheckedPatrolCovers.Add(patrolCheckPoint);
            if (recentlyCheckedPatrolCovers.Count > maxRecentPatrolChecks)
            {
                recentlyCheckedPatrolCovers.RemoveAt(0);
            }
            patrolCheckPoint = null;
            patrolLookTimer = 0f;
            isCheckingPatrolCover = false;
            return;
        }
        // =========================================================
        // ROUTE COMPLETE
        // =========================================================

        if (indirectionStrikeRouteIndex >= indirectionStrikeRoute.Count)
        {
            
            isFollowingIndirectionStrikeRoute = false;
            isMovingToExactIndirectionStrikeRoute = true;
            DevLog.Log("STRIKE MEMORY | TACTICAL POSSIBILITIES " +
            "INDIRECTION STRIKE ROUTE COMPLETE | " +
            "NOW MOVING TO EXACT LKP: " +
            guardVision.lastKnownPosition
        );

            return;
        }
        // =========================================================
        // CURRENT TACTICAL ROUTE NODE
        // =========================================================

        PatrolPoint targetNode =
            indirectionStrikeRoute[indirectionStrikeRouteIndex];

        if (targetNode == null)
        {
            indirectionStrikeRouteIndex++;
            return;
        }
        // =========================================================
        // SEARCH FOR PATROL COVER
        // =========================================================
        patrolCheckTimer += Time.deltaTime;

        if (patrolCheckTimer >= patrolCheckInterval)
        {
            patrolCheckTimer = 0;

            patrolCheckPoint =
                guardCover.ChoosePatrolCheckPoint(
                    transform.position,
                    patrolCoverCheckRadius,
                    recentlyCheckedPatrolCovers,
                    guardAI.coverPoints
                    );
            if (patrolCheckPoint != null)
            {
                isCheckingPatrolCover = true;
                patrolLookTimer = 0f;
                return;
            }

        }
        // =========================================================
        // MOVE TO TACTICAL ROUTE NODE
        // =========================================================

        bool arrived = MoveTo(targetNode.transform.position);
        DevLog.Log("STRIKE MEMORY | TACTICAL POSSIBILITIES | " +
        "INDIRECTION STRIKE ROUTE | " +
        "MOVING TO: " + targetNode.name +
        " | ARRIVED: " + arrived
    );
        if (arrived)
        {
            DevLog.Log("STRIKE ROUTE | TACTICAL POSSIBILITIES | " +
            "INDIRECTION STRIKE ROUTE | " +
            "ARRIVED AT: " + targetNode.name
        );

            indirectionStrikeRouteIndex++;
        }
    }


    public void FollowindirectioninvestigationRoute()
    {
        if (!isFollowingIndirectionInvestigationRoute)
        {
            return;
        }
        if((indirectionInvestigationRoute == null)||
            (indirectionInvestigationRoute.Count == 0))
        {
            isFollowingIndirectionInvestigationRoute = false;
            DevLog.Log(
           "INDIRECTION INVESTIGATION ROUTE | " +
           "NO ROUTE"
       );

            return;
        }
        // =========================================================
        // PATROL COVER CHECK
        // =========================================================
        if (isCheckingPatrolCover)
        {
            Stop();
            if (patrolCheckPoint != null)
            {
                RotationTo(patrolCheckPoint.position);
            }
            patrolLookTimer += Time.deltaTime;

            if (patrolLookTimer < patrolLookDuration)
            {
                return;
            }
            DevLog.Log(
           "ESCAPE ROUTE | TACTICAL POSSIBILITIES | " +
           "PATROL COVER 2 CHECKED: " +
           patrolCheckPoint.name
       );
            recentlyCheckedPatrolCovers.Add(patrolCheckPoint);
            if (recentlyCheckedPatrolCovers.Count > maxRecentPatrolChecks)
            {
                recentlyCheckedPatrolCovers.RemoveAt(0);
            }
            patrolCheckPoint = null;
            patrolLookTimer = 0f;
            isCheckingPatrolCover = false;
            return;
        }
        // =========================================================
        // ROUTE COMPLETE
        // =========================================================

        if (indirectionInvestigationRouteIndex >= indirectionInvestigationRoute.Count)
        {
            isFollowingIndirectionInvestigationRoute = false;
            isMovingToExactIndirectionInvestigationRoute = true;
            DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES "+
            "INDIRECTION INVESTIGATION ROUTE COMPLETE | " +
            "NOW MOVING TO EXACT LKP: " +
            guardVision.lastKnownPosition
        );

            return;
        }
        // =========================================================
        // CURRENT TACTICAL ROUTE NODE
        // =========================================================

        PatrolPoint targetNode =
            indirectionInvestigationRoute[indirectionInvestigationRouteIndex];

        if(targetNode == null)
        {
            indirectionInvestigationRouteIndex++;
            return;
        }
        // =========================================================
        // SEARCH FOR PATROL COVER
        // =========================================================
        patrolCheckTimer += Time.deltaTime;

        if (patrolCheckTimer >= patrolCheckInterval)
        {
            patrolCheckTimer = 0;

            patrolCheckPoint =
                guardCover.ChoosePatrolCheckPoint(
                    transform.position,
                    patrolCoverCheckRadius,
                    recentlyCheckedPatrolCovers,
                    guardAI.coverPoints
                    );
            if (patrolCheckPoint != null)
            {
                isCheckingPatrolCover = true;
                patrolLookTimer = 0f;
                return;
            }

        }
        // =========================================================
        // MOVE TO TACTICAL ROUTE NODE
        // =========================================================

        bool arrived = MoveTo(targetNode.transform.position);
        DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES | "+
        "INDIRECTION INVESTIGATION ROUTE | " +
        "MOVING TO: " + targetNode.name +
        " | ARRIVED: " + arrived
    );
        if (arrived)
        {
            DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES | " +
            "INDIRECTION INVESTIGATION ROUTE | " +
            "ARRIVED AT: " + targetNode.name
        );

            indirectionInvestigationRouteIndex++;
        }
    }
    public void MoveToExactIndirectionStrikeRoute()
    {
        if (!isMovingToExactIndirectionStrikeRoute) { return; }
        bool arrived =
        MoveTo(guardVision.lastKnownPosition);

        DevLog.Log("STRIKE MEMORY | TACTICAL POSSIBILITIES " +
            "INDIRECTION STRIKE ROUTE | " +
            "MOVING TO EXACT POSITION | " +
            "TARGET: " + guardVision.lastKnownPosition +
            " | ARRIVED: " + arrived
        );

        if (arrived)
        {
            isMovingToExactIndirectionStrikeRoute = false;

            DevLog.Log("STRIKE MEMORY | TACTICAL POSSIBILITIES " +
                "INDIRECTION STRIKE ROUTE | " +
                "EXACT LKP REACHED!"
            );

            BeginSearch();
        }
    }
    public void MoveToExactIndirectionInvestigationRoute()
    {
        if (!isMovingToExactIndirectionInvestigationRoute) { return; }
        bool arrived =
        MoveTo(guardVision.lastKnownPosition);

        DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES "+
            "INDIRECTION INVESTIGATION ROUTE | " +
            "MOVING TO EXACT POSITION | " +
            "TARGET: " + guardVision.lastKnownPosition +
            " | ARRIVED: " + arrived
        );

        if (arrived)
        {
            isMovingToExactIndirectionInvestigationRoute = false;

            DevLog.Log("ESCAPE ROUTE | TACTICAL POSSIBILITIES "+
                "INDIRECTION INVESTIGATION ROUTE | " +
                "EXACT LKP REACHED!"
            );

            BeginSearch();
        }
    }
    public void guardResetKnown()
    {
        currentState = GuardMovement.GuardState.Patrol;
        guardVision.lastKnownPosition = Vector3.zero;
        guardVision.lastKnownPosition = Vector3.zero;
        guardVision.playerDetected = false;
        guardAI.currentCover = null;
    }
    private void UpdateStuckDetection()
    {
        stuckSampleTimer += Time.deltaTime;
        //Debug.Log(
        //   "GUARD MOVEMENT | UPDATE STUCK SAMPLE TIMER | " + stuckSampleTimer

        //   );
        if (stuckSampleTimer < stuckSampleInterval)
        {
            return;
        }
        
        float horizontalDistance = Vector3.Distance(
            new Vector3(
                transform.position.x,
                0f,
                transform.position.z
                ),
            new Vector3(
                lastProgressPosition.x,
                0f,
                lastProgressPosition.z
                )
            );
        DevLog.Log(
           "GUARD MOVEMENT | UPDATE STUCK DETECTION | " 
           
           );
        if (horizontalDistance < minimumProgressDistance)
        {
            stuckTimer += stuckSampleTimer;
            DevLog.Log(
            "GUARD MOVEMENT | NOT MAKING PROGRESS | " +
            "STUCK TIME: " + stuckTimer.ToString("F1")
            );
        }
        else
        {
            stuckTimer = 0f;
        }
        lastProgressPosition = transform.position;
        stuckSampleTimer = 0f;
        
    }
    public bool MoveTo(Vector3 target)
    {
        originalMoveTarget = target;
        UpdateStuckDetection();
        //isRecoveringFromStuck = false;
        DevLog.Log("GUARD MOVEMENT | isRecoveringFromStuck : "+ isRecoveringFromStuck+" stuckTimer : "+ stuckTimer);
        if ((!isRecoveringFromStuck && stuckTimer >= stuckTimeout)
            || isRecoveringFromStuckFalse)
        {
            DevLog.Log(
            "GUARD MOVEMENT | STUCK | " +
            "SEARCHING FOR RECOVERY PATROL POINT"
        );
            PatrolPoint recoveryPoint =
                FindRecoveryPatrolPoint(
                    transform.position,
                    target
                    );
            if (recoveryPoint != null)
            {
                recoveryPatrolPoint = recoveryPoint;
                walkRecoveryPatrolPoints.Add(recoveryPatrolPoint);
                isRecoveringFromStuck = true;
                isRecoveringFromStuckFalse = false;
                stuckTimer = 0f;
                DevLog.Log(
                "GUARD MOVEMENT | RECOVERY POINT FOUND: " +
                recoveryPatrolPoint.name
                );
            }
            else
            {
                DevLog.Log(
                "GUARD MOVEMENT | STUCK | " +
                "NO RECOVERY POINT FOUND"
            );
                isRecoveringFromStuckFalse = false;
                stuckTimer = 0f;
            }
        }
        // If recovering, temporarily move toward recovery point.
        // =========================================================
        // RECOVERY MOVEMENT
        // =========================================================
        if (isRecoveringFromStuck)
        {
            bool reachedRecoveryPoint =
                MoveDirectlyTo(
                    recoveryPatrolPoint.transform.position
                );
            if(stuckTimer >= stuckTimeout){ isRecoveringFromStuckFalse = true;}

            if (reachedRecoveryPoint)
            {
                DevLog.Log(
                    "GUARD MOVEMENT | RECOVERY POINT REACHED | " +
                    recoveryPatrolPoint.name
                );

                isRecoveringFromStuck = false;
                recoveryPatrolPoint = null;

                stuckTimer = 0f;
                stuckSampleTimer = 0f;
                lastProgressPosition = transform.position;
            }
            else
            {
                // Still travelling toward recovery point.
                return false;
            }

                //return false;
        }
        // =========================================================
        // NORMAL MOVEMENT TOWARD ORIGINAL TARGET
        // =========================================================
        bool reachedOriginalTarget =
            MoveDirectlyTo(originalMoveTarget);

        if (reachedOriginalTarget)
        {
            DevLog.Log(
            "GUARD MOVEMENT | ORIGINAL TARGET REACHED | " +
            originalMoveTarget
            );
            failedRecoveryPoints.Clear();
            walkRecoveryPatrolPoints.Clear();
            isRecoveringFromStuck = false;
            stuckTimer = 0f;
            stuckSampleTimer = 0f;
            lastProgressPosition = transform.position;

            return true;
        }

        return false;
        //return MoveDirectlyTo(target);

        //SECOND VERSION

        // =========================================================
        // RECOVERY MODE
        // =========================================================
        // If recovering, temporarily move toward recovery point.
        //if (isRecoveringFromStuck)
        //{
        //    // Recovery point itself is causing another stuck situation.
        //    if(stuckTimer >= stuckTimeout)
        //    {
        //        Debug.Log(
        //        "GUARD MOVEMENT | RECOVERY POINT FAILED | " +
        //        recoveryPatrolPoint.name
        //        );
        //        // Remember this recovery point as failed.
        //        if (!failedRecoveryPoints.Contains(recoveryPatrolPoint))
        //        {
        //            failedRecoveryPoints.Add(recoveryPatrolPoint);
        //        }
        //        // Find another recovery point.
        //        PatrolPoint nextRecoveryPoint =
        //            FindRecoveryPatrolPoint(
        //                transform.position,
        //                target
        //                );
        //        if (nextRecoveryPoint != null)
        //        {
        //            recoveryPatrolPoint = nextRecoveryPoint;
        //            stuckTimer = 0f;
        //            stuckSampleTimer = 0f;
        //            lastProgressPosition = transform.position;
        //            Debug.Log(
        //            "GUARD MOVEMENT | NEW RECOVERY POINT | " +
        //            recoveryPatrolPoint.name
        //        );
        //        }
        //        else
        //        {
        //            Debug.Log(
        //           "GUARD MOVEMENT | NO MORE RECOVERY POINTS"
        //            );

        //            isRecoveringFromStuck = false;
        //            recoveryPatrolPoint = null;

        //            stuckTimer = 0f;
        //            stuckSampleTimer = 0f;
        //            lastProgressPosition = transform.position;


        //        }
        //        return false;
        //    }
        //    // Continue moving toward current recovery point.
        //            bool reachedRecoveryPoint =
        //                MoveDirectlyTo(
        //                    recoveryPatrolPoint.transform.position
        //                    );
        //            if (reachedRecoveryPoint)
        //            {
        //                Debug.Log(
        //                "GUARD MOVEMENT | RECOVERY POINT REACHED | " +
        //                recoveryPatrolPoint.name
        //                );

        //        // Test whether we can now continue
        //        // from the recovery point toward the original target.
        //                bool pathToTargetIsClear =
        //                            IsPathClear(
        //                                transform.position,
        //                                originalMoveTarget
        //                        );
        //                    if (pathToTargetIsClear)
        //                    {
        //                                    Debug.Log(
        //                        "GUARD MOVEMENT | RECOVERY SUCCESS | " +
        //                        recoveryPatrolPoint.name +
        //                        " -> ORIGINAL TARGET CLEAR"
        //                        );
        //                        isRecoveringFromStuck = false;
        //                        recoveryPatrolPoint = null;

        //                        stuckTimer = 0f;
        //                        stuckSampleTimer = 0f;
        //                        lastProgressPosition = transform.position;
        //                    }
        //                    else
        //                    {
        //                                            Debug.Log(
        //                        "GUARD MOVEMENT | RECOVERY FAILED | " +
        //                        recoveryPatrolPoint.name +
        //                        " -> ORIGINAL TARGET BLOCKED"
        //                        );
        //                    // Remember this recovery point as failed.
        //                    if (!failedRecoveryPoints.Contains(recoveryPatrolPoint))
        //                    {
        //                        failedRecoveryPoints.Add(recoveryPatrolPoint);
        //                    }
        //            // Search for another recovery point.
        //                    PatrolPoint nextRecoveryPoint =
        //                            FindRecoveryPatrolPoint(
        //                               transform.position,
        //                               originalMoveTarget
        //                            );
        //                        if (nextRecoveryPoint != null)
        //                        {
        //                            recoveryPatrolPoint = nextRecoveryPoint;

        //                            stuckTimer = 0f;
        //                            stuckSampleTimer = 0f;
        //                            lastProgressPosition = transform.position;

        //                            Debug.Log(
        //                                "GUARD MOVEMENT | NEXT RECOVERY POINT | " +
        //                                recoveryPatrolPoint.name
        //                            );
        //                        }
        //                        else
        //                        {
        //                            Debug.Log(
        //                                "GUARD MOVEMENT | NO MORE RECOVERY POINTS"
        //                            );

        //                            isRecoveringFromStuck = false;
        //                            recoveryPatrolPoint = null;

        //                            stuckTimer = 0f;
        //                            stuckSampleTimer = 0f;
        //                            lastProgressPosition = transform.position;


        //                        }
        //        }


        //        // Remember this recovery point as failed.

        //    }
        //            return false;
        //}
        //// =========================================================
        //// NORMAL MOVEMENT
        //// =========================================================
        //if(stuckTimer >= stuckTimeout)
        //{
        //    Debug.Log(
        //    "GUARD MOVEMENT | STUCK | " +
        //    "SEARCHING FOR RECOVERY PATROL POINT"
        //    );
        //    PatrolPoint recoveryPoint =
        //        FindRecoveryPatrolPoint(
        //            transform.position,
        //            target
        //            );
        //    if (recoveryPoint != null)
        //    {
        //        recoveryPatrolPoint = recoveryPoint;

        //        isRecoveringFromStuck = true;

        //        stuckTimer = 0f;
        //        stuckSampleTimer = 0f;
        //        lastProgressPosition = transform.position;

        //        Debug.Log(
        //            "GUARD MOVEMENT | RECOVERY POINT FOUND: " +
        //            recoveryPatrolPoint.name
        //        );

        //        return false;
        //    }
        //    Debug.Log(
        //    "GUARD MOVEMENT | STUCK | " +
        //    "NO RECOVERY POINT FOUND"
        //);

        //    stuckTimer = 0f;

        //    return false;
        //}
        //return MoveDirectlyTo(target);
    }
    //private bool MoveDirectlyTo(Vector3 target)
    //{
    //    Vector3 flatTarget = new Vector3(
    //        target.x,
    //        transform.position.y,
    //        target.z
    //        );
    //    Debug.Log("GUARD MOVEMENT | MOVEDIRECTLY TO : "+ target);
    //    float distanceToTarget = Vector3.Distance(
    //        transform.position,
    //        flatTarget
    //        );
    //    if(distanceToTarget < 1f)
    //    {
    //        return true;
    //    }
    //    Vector3 direction = flatTarget - transform.position;
    //    if (controller.isGrounded)
    //    {
    //        verticalVelocity = -1f;
    //    }
    //    else
    //    {
    //        verticalVelocity += gravity * Time.deltaTime;
    //    }
    //    Vector3 movement =
    //        direction * (PatrolSpeed * sprintSpeed);

    //    movement.y = verticalVelocity;

    //    controller.Move(movement * Time.deltaTime);

    //    return false;
    //}
    public bool MoveDirectlyTo(Vector3 target)
    {

        //Debug.Log("MoveTo Called");
        Vector3 flatTarget = new Vector3(
            target.x,
            transform.position.y,
            target.z
            );
        float distanceToTarget = Vector3.Distance(
            transform.position,
            flatTarget
            );
        //float distanceToOriginalTarget =
        //    Vector3.Distance(
        //    flatTarget,
        //    originalMoveTarget
        //    );
        //float distanceToOriginalTarget =
        //     Vector3.Distance(
        //           new Vector3(
        //               transform.position.x,
        //               0f,
        //               transform.position.z
        //               ),
        //           new Vector3(
        //               originalMoveTarget.x,
        //               0f,
        //               originalMoveTarget.z
        //               )
        //         );
        ////Debug.Log("MOVE TEST | DISTANCE:  " + distanceToTarget);
        //if (distanceToOriginalTarget < 1f)
        //{
        //    Debug.Log("GUARD MOVEMENT | CLEAR");
        //    failedRecoveryPoints.Clear();
        //    walkRecoveryPatrolPoints.Clear();
        //}
        if (distanceToTarget < 1f)
        {
            DevLog.Log("GUARD MOVEMENT | CLEAR2 ORIGINAL MOVE TARGET: "+ originalMoveTarget);
            return true;

        }
        
        Vector3 direction = flatTarget - transform.position;
        direction.Normalize();
        if (controller.isGrounded)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        //if((!controller.isGrounded))
        //{
        //    isGroundedFall += Time.deltaTime;
        //    if (isGroundedFall > 10)
        //    {
        //        DevLog.Log("Respawn From Fall");
        //        Die();

               
        //    }
        //    return false;
        //}
            //isGroundedFall = 0f;
        Vector3 movement = direction * (PatrolSpeed + sprintSpeed);
        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);
        //Vector3 direction = target - transform.position;
        //direction.y = 0f;
        //direction.Normalize();
        //if (controller.isGrounded)
        //{
        //    verticalVelocity = -1f;
        //}
        //else {
        //    verticalVelocity += gravity * Time.deltaTime;
        //}
        //Vector3 movement = direction * PatrolSpeed;
        //        movement.y = verticalVelocity;
        //        controller.Move(movement * Time.deltaTime);

        //if (Vector3.Distance(transform.position, target) < 1f) {
        //    return true;
        //}

        return false;
    }
    private PatrolPoint FindRecoveryPatrolPoint(
        Vector3 stuckPosition,
        Vector3 originalTarget
        )
    {
        PatrolPoint[] allPoints =
            FindObjectsOfType<PatrolPoint>();

        System.Array.Sort(
            allPoints,
            (a, b) =>
                {
                    float distanceA =
                        Vector3.Distance(
                            stuckPosition,
                            a.transform.position
                            );

                    float distanceB =
                        Vector3.Distance(
                            stuckPosition,
                            b.transform.position
                            );
                    return distanceA.CompareTo(distanceB);
                }
            );
        foreach (PatrolPoint candidate in allPoints) {
            if (candidate == null)
            {
                continue;
            }
            if (failedRecoveryPoints.Contains(candidate))
            {
                DevLog.Log(
                    "GUARD MOVEMENT | RECOVERY TEST | " +
                    candidate.name +
                    " | ALREADY FAILED"
                );

                continue;
            }
            if (walkRecoveryPatrolPoints.Contains(candidate))
            {
                DevLog.Log(
                    "GUARD MOVEMENT | OLD WALK RECOVERY TEST | " +
                    candidate.name +
                    " | ALREADY FAILED"
                );

                continue;
            }
            // Don't select the original destination.
            if (candidate.transform.position == originalTarget) { continue; }


            // Test 1:
            // Can Guard reach the recovery point?
            bool pathToCandidate =
                IsPathClear(
                    transform.position,
                    candidate.transform.position
                    );

            if (!pathToCandidate)
            {
                DevLog.Log(
                "GUARD MOVEMENT | RECOVERY TEST | " +
                candidate.name +
                " | GUARD -> CANDIDATE BLOCKED"
            );

                continue;
            }
            // Test 2:
            // After reaching candidate, can Guard
            // continue toward the original target?
            bool pathToOriginalTarget =
                IsPathClear(
                    candidate.transform.position,
                    originalTarget
                    );
            if (!pathToOriginalTarget)
            {
                DevLog.Log(
                    "GUARD MOVEMENT | RECOVERY TEST | " +
                    candidate.name +
                    " | CANDIDATE -> TARGET BLOCKED"
                );

                continue;
            }
            //   Debug.DrawLine(
            //  candidate.transform.position,
            //originalTarget,
            //  Color.yellow,
            //  2f
            //  );
            //   Debug.DrawLine(
            // transform.position,
            //candidate.transform.position,
            // Color.blue,
            // 2f
            // );
            DevLog.Log(
            "GUARD MOVEMENT | RECOVERY TEST | VALID DETOUR: " +
            candidate.name
            );

            return candidate;
        }
        return null;
    }
    private bool IsPathClear(
        Vector3 start,
        Vector3 destination
        )
    {
        
        Vector3 direction = destination - start;

        float distance = direction.magnitude;

        //if (distance <= 0.01f) { return true; }

        direction.Normalize();

        Vector3 rayStart =
            start + Vector3.up * 1f;

       
        bool blocked =
            Physics.Raycast(
                rayStart,
                direction,
                distance,
                guardVision.obstacleMask
                );

        return !blocked;
    }
   

    //public void MoveToExactInvestigationPosition()
    //{
    //    if (!isMovingToExactInvestigationPosition) { return; }


    //}

    // Update is called once per frame
    void Update()
    {
        UpdateSuppression();
        UpdateTacticalThinking();
        UpdateIntent();
        if (Input.GetKeyDown(KeyCode.F))
        {
            SpawnMuzzleFlash();

        }
        if (guardAI.suppressionLevel >= 25f)
        {
            sprintSpeed = 0.5f;
            //guardAI.currentState = GuardAI.GuardState.Patrol;
            //guardVision.playerDetected = false;
        }
        else
        {
            sprintSpeed = 0;
        }
        //Debug.Log("sprintSpeed : " + sprintSpeed);
    }
    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }
        if(bulletTracerPrefab == null)
        {
            //Debug.LogWarning("No Bullet Tracer Prefab assigned!");
            return;
        }
        GameObject tracer = Instantiate(
            bulletTracerPrefab,
            start,
            Quaternion.identity
            );
        activeTracers.Add(tracer);
        Vector3 direction = end - start;
        if(direction.sqrMagnitude > 0.001f)
        {
            tracer.transform.rotation = Quaternion.LookRotation(direction.normalized);

        }
        //tracer.transform.position = end;
        //Destroy(tracer, 0.03f);
        StartCoroutine(
            MoveTracer(
                tracer,
                start,
                end
                )
            );

    }
    private IEnumerator MoveTracer(GameObject tracer, Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);

        float travelTime = distance / TracerSpeed;

        float timer = 0f;

        while (timer < travelTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / travelTime);
            tracer.transform.position = Vector3.Lerp(
                start,
                end,
                t
                );
            yield return null;
        }
        tracer.transform.position = end;
        activeTracers.Remove(tracer);
        Destroy(tracer);

    }
    private void SpawnMuzzleFlash()
    {
        if(muzzleFlashPrefab == null)
        {
            //Debug.LogWarning("No Muzzle Flash Prefab assigned!");
            return;
        }
        if(muzzlePoint == null)
        {

            //Debug.LogWarning("No Muzzle Point assigned!");
            return;
        }
        GameObject flash = Instantiate(
            muzzleFlashPrefab,
            muzzlePoint.position,
            muzzlePoint.rotation
            );
        //Debug.DrawLine(
        //                muzzlePoint.position,
        //                muzzlePoint.position + muzzlePoint.forward * 2f,
        //                Color.magenta,
        //                5fF
        //            );
//        Debug.Log(
//    "MUZZLE FLASH SPAWNED AT : " +
//    flash.transform.position
//);
        Destroy(flash, 0.03f);

    }
}
