using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;




public class CoverSystem : MonoBehaviour
{

    [SerializeField] private Camera playerCamera;
    enum PreviewType
    {
        Ground,
        Cover,
        Invalid

    }
    // Start is called before the first frame update
    PlayerMovement playerMovement;
    PreviewType currentPreviewType;
    Vector3 previewPosition;

    public GameObject preview;

    [Header("Muzzle Ray")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private float muzzleRayDistance = 10f;

    [Header("Player Cover Tracer")]
    public Transform feetTracer;
    public Transform leftLegTracer;
    public Transform rightLegTracer;

    public Transform abdomenTracer;

    public Transform chestTracer;

    public Transform headTracer;

    public Transform leftHandTracer;
    public Transform rightHandTracer;


    public float playerCoverSearchRadius = 5f;
    public CoverPoint.ConcealmentType typeOfConcealment;

    Cover selectedCover;
    CursorManager cursorManager;
    Transform nearestPoint = null;


    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        cursorManager = GetComponent<CursorManager>();
        if(playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }
    Transform GetNearestCoverPoint(Cover cover, Vector3 hitPosition){


        return null;
    }
    private Transform GetBestVisibleCoverPoint()
    {

        CoverPoint[] allPoints = FindObjectsOfType<CoverPoint>();
        Transform bestPoint = null;
        float bestScore = Mathf.Infinity;

        foreach (CoverPoint point in allPoints)
        {

            if (!point.CanPlayerUse()) { continue; }



            Vector3 pointPosition = point.transform.position;

            //1. DISTANCE CHECK
            float distance = Vector3.Distance(
                transform.position,
                pointPosition
                );
            if (distance > playerCoverSearchRadius) continue;

            //2. CAMERA DIRECTION CHECK
            Vector3 directionToPoint = pointPosition - playerCamera.transform.position;
            float angle = Vector3.Angle(
                playerCamera.transform.forward,
                directionToPoint
                );
            //reject covers that are too far
            //outside where the player is looking
            if (angle > 60f) continue;

            //3. CAMERA VIEW PORT
            Vector3 viewportPosition = playerCamera.WorldToViewportPoint(point.transform.position);
            
            //Behind Camera
            if (viewportPosition.z <= 0f) continue;

            //Outside Camera View
            if (viewportPosition.x < 0f ||
                viewportPosition.x > 1f ||
                viewportPosition.y < 0f ||
                viewportPosition.y > 1f
                ) continue;

            //float distance = Vector3.Distance(transform.position, point.transform.position);
            //if (distance > 10f) continue;

            //4. VISIBILITY RAYCAST
            Vector3 direction = point.transform.position - playerCamera.transform.position;
            float distanceToPoint = direction.magnitude;
            if (Physics.Raycast(
                playerCamera.transform.position,
                direction.normalized,
                out RaycastHit hit,
                distanceToPoint,
                ~0,
                QueryTriggerInteraction.Ignore)) { 
                    if(hit.transform!= point.transform) { continue; }
            }


            //Vector3 origin = playerCamera.transform.position;

            //Vector3 direction = pointPosition - origin;

            //float rayDistance = direction.magnitude;

            //direction.Normalize();

            //if (Physics.Raycast(
            //    origin,
            //    direction,
            //    out RaycastHit hit,
            //    rayDistance,
            //    ~0,
            //    QueryTriggerInteraction.Ignore)) {
            //    if ((hit.transform != point.transform) &&
            //        (!hit.transform.IsChildOf(point.transform))
            //        ) { continue; }

            //    }

            //5
            //Cover pointCover = point.GetComponentInParent

            // score 
            float angleScore = angle;
            float distanceScore = distance * 0.05f;
            float score = angleScore + distanceScore;

            //float score = Vector2.Distance(
            //    new Vector2(viewportPosition.x, viewportPosition.y),
            //    new Vector2(0.5f,0.5f)
            //    );
            //score += distance * 0.05f;


            // best point
            if(score < bestScore)
            {
                bestScore = score;
                bestPoint = point.transform;

            }

            
        }
        if(bestPoint != null)
        {
            //Debug.DrawLine(
            //    playerCamera.transform.position,
            //    bestPoint.position,
            //    Color.green,
            //    2f
                
            //    );
            //Debug.Log(" COVER TARGET SELECTED: "+bestPoint.name);
        }
        else
        {
            //Debug.Log("| COVER TARGET | NONE");
        }

            return bestPoint;
    }
    Transform GetNearestPlayerCoverPoint()
    {

        CoverPoint[] points = FindObjectsOfType<CoverPoint>();

        Transform bestPoint = null;
        float bestDistance = playerCoverSearchRadius;

        foreach (CoverPoint point in points)
        {
            if (!point.CanPlayerUse()) { continue; }
            float distance = Vector3.Distance(transform.position, point.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = point.transform;
            }

        }
        return bestPoint;
    }
    private Transform GetBestCoverPointForC()
    {

        // ---------------------------------------------------------
        // FIRST:
        // Find the actual cover the player is approaching.
        // ---------------------------------------------------------

        Cover hitCover = GetCoverFromPlayerFeetRay();

        
        if (hitCover == null)
        {

            //Debug.Log(" COVER C | NO COVER FOUND FROM FEET RAY");
            return null;
        }
        // ---------------------------------------------------------
        // SECOND:
        // Only evaluate Point_01–Point_04 belonging to
        // that cover.
        // ---------------------------------------------------------
        //Transform bestPoint = GetBestPointFromHitCover(hitCover);

        //if (bestPoint != null)
        //{
        //    CoverPoint point = bestPoint.GetComponent<CoverPoint>();
        //    if (point != null)
        //    {
        //        cursorManager.DetectHover(point);

        //    }
        //}

        ////==========================
        //Transform bestPoint = GetMuzzleRayCoverPoint(hitCover);
        Transform bestPoint = GetPlayerInsideCoverPoint(hitCover);
        // ---------------------------------------------------------
        // SECOND PRIORITY:
        // If player is not inside a CoverPoint,
        // use the normal CoverPoint selection.
        // ---------------------------------------------------------
        if (bestPoint == null)
        {
            bestPoint = GetBestPointFromHitCover(hitCover);
        }
        if (bestPoint != null)
        {
            CoverPoint point =
                bestPoint.GetComponent<CoverPoint>();

            if (point != null)
            {
                cursorManager.DetectHover(point);
            }
        }
        //CoverPoint point = bestPoint.GetComponent<CoverPoint>();


        DevLog.Log("COVER button C | hitCover: "+ hitCover.name+" bestPoint: "+ bestPoint);
        return bestPoint;

        //Transform visiblePoint = GetBestVisibleCoverPoint();

        //if(visiblePoint!= null)
        //{
        //    return visiblePoint;
        //}
        //return GetNearestPlayerCoverPoint();

    }
    private Transform GetBestSiblingCoverPoint()
    {

        Cover currentCover = playerMovement.currentCover;
        if (currentCover == null) { return null; }
        Transform currentPoint = playerMovement.currentCoverPoint;
        Transform bestPoint = null;
        float bestDistance = Mathf.Infinity;
        float bestScore = Mathf.Infinity;
        foreach (Transform pointTransform in currentCover.coverPoints) {
            if (pointTransform == null) continue;
            if (pointTransform == currentPoint) continue;

            //Don't Select the point we're already using.

            CoverPoint point = pointTransform.GetComponent<CoverPoint>();

            if (point == null) continue;

           

            //Don't select an occupied point.
            if (point.occupied) continue;

            //Player must be allowed to use it.
            if (!point.CanPlayerUse()) continue;

            //float distance = Vector3.Distance(
            //    transform.position,
            //    pointTransform.position
            //    );
            //if (distance > bestDistance) continue;


                Vector3 viewportPosition = playerCamera.WorldToViewportPoint(
                    pointTransform.position
                );

            //Behind Camera
            if (viewportPosition.z <= 0f) continue;

            //Outside Camera View
            if ((viewportPosition.x < 0f) ||
                (viewportPosition.x > 1f) ||
                (viewportPosition.y < 0f) ||
                (viewportPosition.y > 1f)) { continue; }

            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            Vector2 pointScreenPosition = new Vector2(
                viewportPosition.x,
                viewportPosition.y
                
                );

            float aimScore = Vector2.Distance(
                pointScreenPosition,
                screenCenter
                );

            if(aimScore < bestScore)
            {
                bestScore = aimScore;
                bestPoint = pointTransform;
            }
            //{
            //    //bestDistance = distance;
            //    //bestPoint = pointTransform;
            //}


        }

        return bestPoint;
    }
    public void TrySwitchSiblingCover()
    {
        //Debug.Log("COVER BUTTON | COVER SWITCH | C PRESSED");
        Transform selectedPoint = GetBestSiblingCoverPoint();

        if(selectedPoint == null) {
            //Debug.Log("COVER BUTTON SWITCH | NO AVAILABLE SIBLING COVER POINT!");
            return;
        }
        CoverPoint coverPoint = selectedPoint.GetComponent<CoverPoint>();
        if(coverPoint == null)
        {
            //Debug.Log("COVER BUTTON SWITCH | SELECTED POINT HAS NO COVERPOINT");
            return;
        }
        playerMovement.currentCoverPoint = selectedPoint;
        playerMovement.activeCoverPoint = selectedPoint;

        playerMovement.SetDestination(selectedPoint.position);
        //Debug.Log("COVER BUTTON | COVER SWITCH | MOVING TO SIBLING | "+
        //    "Cover: "+ playerMovement.currentCover.name+
        //    " | Point: "+ selectedPoint.name);
    }
    public void TryEnterNearestCover()
    {
        //Debug.Log("COVER BUTTON PRESSED !");
        Transform nearestPlayerPoint = GetBestCoverPointForC();

        GetCoverFromPlayerFeetRay();
        //Transform nearestPlayerPoint = GetNearestPlayerCoverPoint();
        if (nearestPlayerPoint == null)
        {
            //Debug.Log("COVER BUTTON | NO PLAYER COVER POINT FOUND.");
            return;
        }
        CoverPoint coverPoint = nearestPlayerPoint.GetComponent<CoverPoint>();


        if(coverPoint == null)
        {
            //Debug.Log("COVER BUTTON | NO COVER POINT PLAYER NOT FOUND");
            return;
        }
        //DebugCoverPointDirections(coverPoint);

        foreach (CoverPoint.ConcealmentType type in coverPoint.concealmentTypes)
        {
            if (type == CoverPoint.ConcealmentType.Rock)
            {
                playerMovement.isCrouching = true;
                typeOfConcealment = type;
            }
            if (type == CoverPoint.ConcealmentType.Tree)
            {
                playerMovement.isCrouching = false;
                typeOfConcealment = type;
            }

        }
        Cover selectedPlayerCover = nearestPlayerPoint.GetComponentInParent<Cover>();

        if (selectedPlayerCover == null)
        {
            //Debug.Log("COVER BUTTON | CoverPoint has no parent Cover : " + nearestPlayerPoint.name);
            return;
        }
        playerMovement.currentCover = selectedPlayerCover;
        playerMovement.currentCoverPoint = nearestPlayerPoint;
        playerMovement.SetDestination(nearestPlayerPoint.position);

        //Debug.Log("COVER BUTTON | MOVE TO PLAYER COVER | "+
        //            "Cover: "+selectedPlayerCover.name+
        //            " | Point: "+ nearestPlayerPoint.name+" Type: "+ typeOfConcealment);

        //Debug.Log("COVER BUTTON | PLAYER COVER POINT FOUND: "+
        //    nearestPlayerPoint.name+
        //    " | DISTANCE: "+
        //    Vector3.Distance(
        //        transform.position,
        //        nearestPlayerPoint.position
        //        ));

    }
    private bool HasCoverInDirection(Vector3 origin, Vector3 direction) {
        float rayDistance = 1.0f;
        return Physics.Raycast(
            origin,
            direction,
            rayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
            
            );
    
    }
    private void DebugCoverPointDirections(Cover cover,CoverPoint point, float angle, float distance, float score)
    {
        if (point == null) return;
        Transform t = point.transform;

        float rayDistance = 1f;

        // ---------------------------------------------------------
        // RED AXIS
        // ---------------------------------------------------------
        bool redPositive = HasCoverInDirection(
            t.position,
            t.right
            );
        bool redNegative = HasCoverInDirection(
            t.position,
            -t.right
            );
        // ---------------------------------------------------------
        // BLUE AXIS
        // ---------------------------------------------------------
        bool bluePositive = HasCoverInDirection(
            t.position,
            t.forward
            );
        bool blueNegative = HasCoverInDirection(
            t.position,
            -t.forward
            );
        // ---------------------------------------------------------
        // DEBUG RAYS
        // ---------------------------------------------------------
        //Debug.DrawRay(
        //    t.position,
        //    t.right * rayDistance,
        //    Color.red,
        //    2f
        //    );
        //Debug.DrawRay(
        //    t.position,
        //    -t.right * rayDistance,
        //    Color.red,
        //    2f
        //    );
        //Debug.DrawRay(
        //    t.position,
        //    t.forward * rayDistance,
        //    Color.blue,
        //    2f
        //    );
        //Debug.DrawRay(
        //    t.position,
        //    -t.forward * rayDistance,
        //    Color.blue,
        //    2f
        //    );
        // ---------------------------------------------------------
        // DEBUG LOG
        // ---------------------------------------------------------
        DevLog.Log("COVER DIRECTION TEST | Cover: "+cover.name+" coverPoint: "+ point.name+
            " | +RED: "+(redPositive? "BLOCKED" : "OPEN")+
            " | -RED: " + (redNegative ? "BLOCKED" : "OPEN") +
            " | +BLUE: " + (bluePositive ? "BLOCKED" : "OPEN") +
            " | -BLUE: " + (blueNegative ? "BLOCKED" : "OPEN") +
            " | ANGLE: "+ angle +" | DISTANCE: "+distance+" | SCORE: "+score
            );
    }
    private Cover GetCoverFromPlayerFeetRay()
    {
        // ---------------------------------------------------------
        // 1. Prepare player-facing direction
        // ---------------------------------------------------------
        Vector3 direction = playerCamera.transform.forward;
        // We only want horizontal direction.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return null;

        direction.Normalize();
        float rayDistance = playerCoverSearchRadius;

        // ---------------------------------------------------------
        // 2. Test every player body tracer
        // ---------------------------------------------------------
        Transform[] tracers = GetPlayerCoverTracers();

        Cover bestCover = null;

        float bestHitDistance = Mathf.Infinity;

        Transform bestTracer = null;

        RaycastHit bestHit = default;

        foreach (
            Transform tracer in tracers
            ) 
        {
            if (tracer == null) continue;

            // -----------------------------------------------------
            // Raycast from this body location
            // -----------------------------------------------------
            if(Physics.Raycast(
                tracer.position,
                direction,
                out RaycastHit hit,
                rayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
                )) 
            {
                // -------------------------------------------------
                // Find Cover belonging to the collider
                // -------------------------------------------------
                Cover cover = hit.collider.GetComponentInParent<Cover>();
                if(cover == null)
                {
                    continue;
                }
                // -------------------------------------------------
                // Keep the closest valid Cover hit
                // -------------------------------------------------
                if(hit.distance < bestHitDistance)
                {
                    bestHitDistance = hit.distance;
                    bestCover = cover;
                    bestTracer = tracer;
                    bestHit = hit;
                }
            }
        }
        // ---------------------------------------------------------
        // 3. No body tracer found a Cover
        // ---------------------------------------------------------
        if(bestCover == null)
        {
            //Debug.Log(
            //"COVER BUTTON2 | MULTI TRACER | NO COVER HIT"
            //);

            return null;

        }
        // ---------------------------------------------------------
        // 4. Debug winning tracer
        // ---------------------------------------------------------
        //Debug.DrawLine(
        //    bestTracer.position,
        //    bestHit.point,
        //    Color.green,
        //    2f
        //    );
        //Debug.Log(
        //      "COVER BUTTON2 | MULTI TRACER | COVER FOUND: " +
        //        bestCover.name +
        //        " | TRACER: " +
        //        bestTracer.name +
        //        " | HIT: " +
        //        bestHit.collider.name +
        //        " | DISTANCE: " +
        //        bestHitDistance
        //    );
        return bestCover;
    }
    private Transform GetBestPointFromHitCover(Cover cover)
    {
        if (cover == null) return null;

        Transform bestPoint = null;

        float bestScore = Mathf.Infinity;

        foreach(Transform pointTransform in cover.coverPoints)
        {
            if (pointTransform == null) continue;
            

            CoverPoint point = pointTransform.GetComponent<CoverPoint>();
            
            if (point == null) continue;

            if (!point.CanPlayerUse()) continue;

            if (point.occupied) continue;

            // -----------------------------------------------------
            // Determine which sides of this CoverPoint are blocked
            // -----------------------------------------------------

            bool redPositive = HasCoverInDirection(
                   pointTransform.position,
                   pointTransform.right
               );

            bool redNegative = HasCoverInDirection(
                pointTransform.position,
                -pointTransform.right
            );

            bool bluePositive = HasCoverInDirection(
                pointTransform.position,
                pointTransform.forward
            );

            bool blueNegative = HasCoverInDirection(
                pointTransform.position,
                -pointTransform.forward
            );
            // -----------------------------------------------------
            // Point must have at least ONE blocking side.
            // -----------------------------------------------------
            bool hasBlockingSibling =
            redPositive ||
            redNegative ||
            bluePositive ||
            blueNegative;

            if (!hasBlockingSibling)continue;

            // -----------------------------------------------------
            // We will calculate the candidate's usable side next.
            // -----------------------------------------------------

            Vector3 bestCoverDirection = Vector3.zero;
            float bestAngle = Mathf.Infinity;
         
            CheckCoverDirection(
                pointTransform,
                pointTransform.right,
                redPositive,
                ref bestCoverDirection,
                ref bestAngle
                );
            CheckCoverDirection(
                pointTransform,
                -pointTransform.right,
                redNegative,
                ref bestCoverDirection,
                ref bestAngle
                );
            CheckCoverDirection(
                pointTransform,
                pointTransform.forward,
                bluePositive,
                ref bestCoverDirection,
                ref bestAngle
                );
            CheckCoverDirection(
                pointTransform,
                -pointTransform.forward,
                blueNegative,
                ref bestCoverDirection,
                ref bestAngle
                );

            if (bestCoverDirection == Vector3.zero) continue;

            // -----------------------------------------------------
            // Compare player's facing direction to this side.
            // -----------------------------------------------------

            Vector3 playerFacing = playerCamera.transform.forward;
            playerFacing.y = 0f;
            playerFacing.Normalize();

            float angle = Vector3.Angle(
                playerFacing,
                bestCoverDirection
                );

            // -----------------------------------------------------
            // Small distance influence
            // -----------------------------------------------------
            float distance = Vector3.Distance(
                transform.position,
                pointTransform.position
                );

            //float score = angle + distance * 0.05f;
            float score = distance;
            //Debug.Log("Cover Button2 | Cover Candidate | "+pointTransform.name
            //        +" | Angle: "+angle
            //        +" | Distance: "+distance
            //        +" | Score: "+score);
            DebugCoverPointDirections(cover, point, angle, distance, score);
            if (score < bestScore)
            {
                bestScore = score;
                bestPoint = pointTransform;
                
            }
            if (bestPoint != null)
            {
                //Debug.DrawLine(
                //    transform.position,
                //    bestPoint.position,
                //    Color.green,
                //    2f
                //    );
                //Debug.Log("Cover Button2 COVER POINT SELECTED: "+bestPoint.name);
            }
            else
            {
                //Debug.Log("Cover Button2 COVER POINT SELECTED: NONE");

            }
            
        }
        return bestPoint;
    }
    private void CheckCoverDirection(
        Transform point,
        Vector3 direction,
        bool blocked,
        ref Vector3 bestDirection,
        ref float bestAngle
        ) 
    {
        if (!blocked) return;

        Vector3 usableDirection = -direction;

        usableDirection.y = 0f;

        if (usableDirection.sqrMagnitude < 0.001f) return;
        
        usableDirection.Normalize();

        Vector3 playerFacing = playerCamera.transform.forward;

        float angle = Vector3.Angle
            (
            playerFacing,
            usableDirection
            );
        if(angle < bestAngle)
        {

            bestAngle = angle;
            bestDirection = usableDirection;
        }
    }
    private Transform[] GetPlayerCoverTracers()
    {
        return new Transform[]
        {
        feetTracer,

        leftLegTracer,
        rightLegTracer,

        abdomenTracer,

        chestTracer,

        headTracer,

        leftHandTracer,
        rightHandTracer
        };
    }
    private Transform GetMuzzleRayCoverPoint(Cover hitCover)
    {
        if (muzzle == null)
        {
            //Debug.Log("MUZZLE RAY | MUZZLE NULL");
            return null;
        }
        Ray ray = new Ray(
            muzzle.position,
            muzzle.forward
            );
        RaycastHit[] hits = Physics.RaycastAll
            (
            ray,
            muzzleRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
            );
        System.Array.Sort
            (
            hits,
            (a,b) => a.distance.CompareTo(b.distance )
            );
        foreach(RaycastHit hit in hits)
        {
            CoverPoint point = hit.collider.GetComponent<CoverPoint>();
            if (point == null)
            {
                continue;
            }

            // Make sure this Point belongs to
            // the Cover we are currently approaching.
            Cover pointCover = point.GetComponentInParent<Cover>();
            if(pointCover != hitCover)
            {
                continue;
            }
            // Make sure the point is actually one
            // of this Cover's registered cover points.
            if (!hitCover.coverPoints.Contains(point.transform))
            {
                continue;
            }

            if (!point.CanPlayerUse())
            {
                continue;
            }
            
            if (point.occupied)
            {
                continue;
            }

            //Debug.DrawLine(
            //    muzzle.position,
            //    hit.point,
            //    Color.green,
            //    2f
            //);

            //Debug.Log(
            //    "MUZZLE RAY | POINT FOUND | " +
            //    point.name 
            //    //+
            //    //" | DISTANCE: " +
            //    //hit.distance
            //);
            return point.transform;
        }
    //    Debug.DrawRay(
    //    muzzle.position,
    //    muzzle.forward * muzzleRayDistance,
    //    Color.red,
    //    2f
    //);

        //Debug.Log(
        //    "MUZZLE RAY | NO COVER POINT FOUND"
        //);

        return null;
    }
    private Transform GetPlayerInsideCoverPoint(Cover cover)
    {
        if (cover == null)
        {
            //Debug.Log("CoverPoint direction | cover IS NULL");
            return null;
        }
        foreach(Transform pointTransform in cover.coverPoints)
        {
            if (pointTransform == null)
            {
                //Debug.Log("CoverPoint direction | POINT transform IS NULL");
                continue;
            }
            CoverPoint point =
            pointTransform.GetComponent<CoverPoint>();

            if (point == null)
            {
                //Debug.Log("CoverPoint direction | POINT IS NULL");
                continue;
            }

            if (!point.CanPlayerUse())
            {
                //Debug.Log("CoverPoint direction | CanPlayerUse False");
                continue;
            }

            if (point.occupied)
            {
                //Debug.Log("CoverPoint direction | POINT IS OCCUPIED");
                continue;
            }

            if (!point.playerInside)
            {
                //Debug.Log("CoverPoint direction | CanPlayerInside False");
                continue;
            }
            DevLog.Log(
           " CoverPoint direction | COVER C "
           +cover.name+" | PLAYER INSIDE | FIRST PRIORITY | " +
           point.name
       );

            return pointTransform;
        }
        return null;
    }
    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Cover Button3 Cover: "+GetCoverFromPlayerFeetRay());
        GetBestCoverPointForC();
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (playerMovement.isInCover)
            {
                TrySwitchSiblingCover();
            }
            else
            {
                TryEnterNearestCover();
            }
                

        }
        if (Input.GetKey(KeyCode.Space))
        {

            preview.SetActive(true);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;

            if (Physics.Raycast(
                ray,
                out hit,
                Mathf.Infinity,
                Physics.DefaultRaycastLayers,
               QueryTriggerInteraction.Ignore
                ))
            {
                if (hit.collider.CompareTag("Ground")){
                    preview.transform.position = hit.point;
                    previewPosition = preview.transform.position;
                    currentPreviewType = PreviewType.Ground;

                    //Debug.Log("Preview2 : GROUND | "+hit.collider.name);
                    cursorManager.ShowFootCursor();
                }
                //else
                //{
                //    currentPreviewType = PreviewType.Invalid;

                //    Debug.Log("Preview2 : BLOCKED BY | "+
                //        hit.collider.name+
                //        " | TAG: "+hit.collider.tag);
                //}

            }
            else
            {
                currentPreviewType = PreviewType.Invalid;
                //Debug.Log("Preview2 : NO COLLIDER FOUND");
            }

            //===========================================================>
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //RaycastHit[] hits = Physics.RaycastAll(
            //    ray,
            //    Mathf.Infinity,
            //    Physics.DefaultRaycastLayers,
            //    QueryTriggerInteraction.Ignore
            //    );
            //System.Array.Sort(
            //    hits,
            //    (a,b) => a.distance.CompareTo(b.distance)
            //    );
            //bool foundGround = false;

            //foreach(RaycastHit currentHit in hits)
            //{
            //    // Ignore Grass logical colliders.
            //    if(currentHit.collider.gameObject.layer == LayerMask.NameToLayer("Grass"))
            //    {
            //        Debug.Log("Preview2 : IGNORE GRASS | "+
            //            currentHit.collider.name);
            //        continue;
            //    }
            //    // Accept actual Ground.
            //    if (currentHit.collider.CompareTag("Ground"))
            //    {
            //        preview.transform.position = currentHit.point;
            //        previewPosition = preview.transform.position;
            //        currentPreviewType = PreviewType.Ground;
            //        Debug.Log("Preview2 : GROUND | "+
            //            currentHit.collider.name);
            //        cursorManager.ShowFootCursor();
            //        foundGround = true;
            //        break;
            //    }
            //    // Anything else blocks the preview.
            //    Debug.Log("Preview2 : BLOCKED BY | "
            //        +currentHit.collider.name
            //        +" | TAG: "+currentHit.collider.tag);
            //    break;
            //}
            //=============================================>
            //if (!foundGround)
            //{
            //    currentPreviewType = PreviewType.Invalid;
            //    Debug.Log("Preview2 : NO GROUND FOUND");
            //}
            //===============================================>
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //RaycastHit hit;
            //if (Physics.Raycast(ray, out hit))
            //{
            //    if (hit.collider.CompareTag("Ground"))
            //    {
            //        preview.transform.position = hit.point;
            //        previewPosition = preview.transform.position;
            //        currentPreviewType = PreviewType.Ground;
            //        Debug.Log("Preview2 : "+ "Ground "+ hit.collider.tag);
            //        cursorManager.ShowFootCursor();

            //    }
            //    else
            //    {
            //        Debug.Log("Preview2 : " + hit.collider.tag);
            //    }
            //==========================
            //else if (hit.collider.CompareTag("Cover")) //ClickToCover (1st) (I don't want preview point I just want Press C at near cover Points and then will get some cover)
            //{
            //    preview.transform.position = hit.point;
            //    previewPosition = preview.transform.position;
            //    currentPreviewType = PreviewType.Cover;
            //    selectedCover = hit.collider.GetComponent<Cover>();


            //            float shortestDistance = Mathf.Infinity;
            //                foreach (Transform point in selectedCover.coverPoints) {
            //                   float distance = Vector3.Distance(hit.point, point.position);
            //                         if (distance < shortestDistance) {
            //                            shortestDistance = distance;
            //                             nearestPoint = point;
            //                        }             

            //                }
            //                if (nearestPoint != null) {
            //                      preview.transform.position = nearestPoint.position;
            //                      previewPosition = nearestPoint.position;
            //                }
            //    cursorManager.ShowCoverCursor();
            //}
            //else
            //{ 
            //    //cursorManager.ShowInvalidCursor();
            //}

            //}
            //else {
            //    cursorManager.ShowInvalidCursor();
            //}
            //Debug.Log("Preview Type:"+currentPreviewType);

            //===============================================>
            if (Input.GetMouseButtonDown(0))
            {


                //playerMovement.SetDestination(previewPosition); //Outside Player Movement
                //playerMovement.currentCover = selectedCover;
                //playerMovement.currentCoverPoint = nearestPoint;
                //Debug.Log("Destination Sent!");
                playerMovement.isInCover = false;
                if (currentPreviewType == PreviewType.Cover) //2nd COVER
                {
                    playerMovement.currentCover = selectedCover;
                    playerMovement.currentCoverPoint = nearestPoint; //nearestCoverPoint
                    playerMovement.SetDestination(previewPosition);

                    //Debug.Log("Destination Sent!");
                    //Debug.Log("Move To Cover");

                }
                else if (currentPreviewType == PreviewType.Ground)
                {
                    playerMovement.isAttachedToCover = false;
                    playerMovement.isInCover = false;
                    playerMovement.activeCoverPoint = null;
                    playerMovement.currentCover = null;
                    playerMovement.currentCoverPoint = null;
                    playerMovement.SetDestination(previewPosition);

                    //Debug.Log("Destination Sent!");

                    //Debug.Log("Move To  Ground");

                }

            }
        }
        else {
            preview.SetActive(false);
            
        
        }

        
    }
    
}
