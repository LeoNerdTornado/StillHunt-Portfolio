using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Start is called before the first frame update

    public LayerMask groundLayer;

    public bool isInCover = false;
    public bool isAttachedToCover = false;
    public bool hasDestination = false;
    public bool isCrouching = false;
    public bool isInGrass = false;
    public bool madeNoise = false;

    private bool firstMatchPerRound = false;

    

    [Header("Soldier Visual")]
    [SerializeField]private Transform soldierModel;
    [SerializeField]private float standingModelY = 0.43f;
    [SerializeField]private float crouchingModelY = 1.0f;
    [SerializeField]private float modelCrouchSpeed = 8f;
    [SerializeField] private float crouchModelOffset = 0.57f; 

           CharacterController controller;
    [Header("Body Visual Position")]
    [SerializeField] private float standingBodyY = 0.43f; 
    [SerializeField]private float crouchingBodyY =0.43f;
    [SerializeField]public float bodyCrouchTransitionSpeed = 8f;

    [Header("Posture")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 2; //1.2f 

    [SerializeField] private float standingCenterY = 1f;
    [SerializeField] private float crouchingCenterY = 1f; //0.6f

    [SerializeField] private float postureSpeed = 8f;

    [SerializeField] private float crouchAmount = 0f;

    [SerializeField] private float cameraCrouchOffset = 0.4f;
    [SerializeField] private float cameraCrouchTransitionSpeed = 8f;

    [SerializeField] private CursorManager cursorManager;

    [SerializeField] private Animator soldierAnimator;
    //=======================================================
    [Header("Aim Offset References")]
    [SerializeField] private Transform weaponAimTarget;
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private WeaponAimTargetFollower rightHandTargetFollower;
    [SerializeField] private ADSHeadAimController adsHeadAimController;

    [Header("Stand Aim Offset")]
    [SerializeField] private Vector3 standWeaponAimPosition = new Vector3(0.2f, 0f, 0.5f);

    [SerializeField] private float standAimLocalY = 1f;
    [SerializeField] private float standForwardOffset = 0.8f;

    [SerializeField]
    private Vector3 standAimRotationOffset =
        new Vector3(255.5f, 69.5f, 14f);

    [SerializeField] private float standAdsCenter = 0.5f;
    [SerializeField] private float standAdsUpward = 0.55f;
    [SerializeField] private float standAdsDown = 0.8f;
    public float standMuzzlePitchOffset = 2f;
    public float standMuzzleYawOffset = 3f;


    [Header("Crouch Aim Offset")]
    [SerializeField] private Vector3 crouchWeaponAimPosition = new Vector3(-1.1f, 0.1f, 0.5f);

    [SerializeField] private float crouchAimLocalY = 0.18f;
    [SerializeField] private float crouchForwardOffset = 1f;

    [SerializeField]
    private Vector3 crouchAimRotationOffset =
        new Vector3(255.5f, 69.5f, 14f);
    
    [SerializeField] private float crouchAdsCenter = 0.6f;
    [SerializeField] private float crouchAdsUpward = 0.65f;
    [SerializeField] private float crouchAdsDown = 0.6f;
    public float crouchMuzzlePitchOffset = 1f;
    public float crouchMuzzleYawOffset =3.2f;

    
    //================================================================
    [Header("Aim Posture Transition")]
    [SerializeField] private float aimCrouchTransitionSpeed = 8f;



    public float moveSpeeds = 1.5f;
    public float walkSpeed = 1.5f;
    public float crouchSpeed = 1f;
    public float gravity = -12f;
    public float noiseRadius = 6f;

    //public float crouchHeight = 1.2f;
    //public float crouchCenterY = 0.6f;
    public float crouchTransitionSpeed = 6f;
    public float bodyCrouchOffset = 0.4f;
    public float groundCheckDistance = 0.5f;
    public float groundSnapDistance = 0.15f;
    public float groundPenetrationTolerance = 0.05f;

    public Transform body;

    
    private float verticalVelocity = 0f;
    private float aimCrouchAmount = 0f;

    public Vector3 destination;
           Vector3 direction;
           Vector3 movement;

    private Vector3 bodyStandingLocalPosition;
    private Vector3 standingCenter;
    private Vector3 bodyStandingScale;
    private Vector3 cameraStandingLocalPosition;
    private Vector3 soldierStandingLocalPosition;

    public Cover currentCover;
    
    public Transform currentCoverPoint;
    public Transform activeCoverPoint;
    public Transform cameraPivot;

    private CoverStanceSystem coverStance;
    void Start()
    {
        controller = GetComponent<CharacterController>();

        if(soldierAnimator == null)
        {
            soldierAnimator = GetComponentInChildren<Animator>();

        }
        soldierStandingLocalPosition = soldierModel.localPosition;
        standingHeight = 2f;
        standingCenterY = 1f;

        bodyStandingLocalPosition = body.localPosition;
        bodyStandingScale = body.localScale;
        cameraStandingLocalPosition = cameraPivot.localPosition;
        //coverStance = GetComponent<CoverStanceSystem>();
        //standingHeight = controller.height;
        standingCenter = new Vector3(
            0,
            standingCenterY,
            0);


        //standingCenterY = standingCenter.y;

       
       
        //controller.Move(Vector3.down * 0.05f);
        verticalVelocity = -1f;
        firstMatchPerRound = true;
        if(cursorManager == null)
        {

            cursorManager = FindObjectOfType<CursorManager>();
        }
        soldierAnimator.SetBool("IsCrouching", isCrouching);
        SetAimOffset();
    }
    public void SetDestination(Vector3 newDestination) { // 1st

        
        activeCoverPoint = currentCoverPoint;
        //Debug.Log("Attach to:" + activeCoverPoint);
        destination = newDestination;
        hasDestination = true; //KEY
    }
    public void EnterCover(Cover cover) { //4RTH
        currentCover = cover;
        isInCover = true;
        isAttachedToCover = true;
        //Debug.Log("COVER BUTTON | Attach To Cover!");
        

    }
    public void UpdateCrouch()
    {
        
        float targetHeight;
        float targetCenterY;

        if (isCrouching)
        {
            targetHeight = crouchingHeight; //crouchingHeight
            targetCenterY = crouchingCenterY; //crouching Center Y

        }
        else
        {
            targetHeight = standingHeight;
            targetCenterY = standingCenterY;
        }
        //controller.height = Mathf.Lerp(
        //    controller.height,
        //    targetHeight,
        //    Time.deltaTime * crouchTransitionSpeed
        //    );
        float smooth = 1f - Mathf.Exp(-postureSpeed * Time.deltaTime);
        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            smooth);

        Vector3 targetCenter = new Vector3(
            standingCenter.x,
            targetCenterY,
            standingCenter.z
            );
        controller.center = Vector3.Lerp(
            controller.center,
            targetCenter,
            smooth
            );
        //Vector3 targetCenter = new Vector3(
        //    standingCenter.x,
        //    targetCenterY,
        //    standingCenter.z
        //    );
        //controller.center = Vector3.Lerp(
        //    controller.center,
        //    targetCenter,
        //    Time.deltaTime * postureSpeed
        //    );
        //float targetCenterY;

        //if (isCrouching)
        //{
        //    targetCenterY = crouchHeight * 0.5f;
        //}
        //else
        //{
        //    targetCenterY = standingCenter.y;
        //}
        //Vector3 targetCenter = new Vector3(
        //    standingCenter.x,
        //    targetCenterY,
        //    standingCenter.z
        //    );

        //controller.center = Vector3.Lerp(
        //    controller.center,
        //    targetCenter,
        //    Time.deltaTime * crouchTransitionSpeed
        //    );
    }
    public void UpdateBodyCrouch()
    {
        float targetY = isCrouching
            ? crouchingBodyY
            : standingBodyY;

        //float targetY = bodyStandingLocalPosition.y;

        Vector3 targetPosition = new Vector3(
            body.localPosition.x,
            targetY,
            body.localPosition.z);
        //Standing = 100% body height
        //Crouching = 60% body height

        //float targetScaleY = isCrouching ? 0.6f : 1f;
        //Vector3 targetScale = new Vector3(
        //    bodyStandingScale.x,
        //    bodyStandingScale.y * targetScaleY,
        //    bodyStandingScale.z
        //    );
        float smooth = 1f - Mathf.Exp(-bodyCrouchTransitionSpeed * Time.deltaTime);

        body.localPosition = Vector3.Lerp(
            body.localPosition,
            targetPosition,
            smooth
            );
       
        //body.localScale = Vector3.Lerp(
        //    body.localScale,
        //    targetScale,
        //    bodyCrouchTransitionSpeed * Time.deltaTime
        //    );
    }
    public void UpdateCameraCrouch()
    {
        float targetY = isCrouching
                ? cameraStandingLocalPosition.y - cameraCrouchOffset
                : cameraStandingLocalPosition.y;
        Vector3 targetPosition = new Vector3(
            cameraStandingLocalPosition.x,
            targetY,
            cameraStandingLocalPosition.z
            );
        cameraPivot.localPosition = Vector3.Lerp(
            cameraPivot.localPosition,
            targetPosition,
            cameraCrouchTransitionSpeed * Time.deltaTime
            );
    }
    void CheckGroundHeight()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.2f;

        RaycastHit hit;

        if (Physics.Raycast(
            rayStart,
            Vector3.down,
            out hit,
            3f
        ))
        {
            float difference =
                transform.position.y - hit.point.y;

            float slopeAngle = Vector3.Angle(
                hit.normal,
                Vector3.up
            );

            //Debug.Log(
            //    "PLAYER PARTS | GROUND CHECK | " +
            //    "Root Y: " + transform.position.y +
            //    " | Hit Y: " + hit.point.y +
            //    " | Difference: " + difference +
            //    " | Hit Object: " + hit.collider.name +
            //    " | Slope Angle: " + slopeAngle
            //);

            //Debug.DrawRay(
            //    rayStart,
            //    Vector3.down * 3f,
            //    Color.yellow,
            //    0f
            //);
        }
        else
        {
            //Debug.Log(
            //    "PLAYER PARTS | GROUND CHECK | NO GROUND FOUND"
            //);
        }
    }
    void DrawPlayerGroundDebug()
    {
        Vector3 controllerBottom =
            transform.position +
            controller.center -
            Vector3.up * (controller.height * 0.5f);

//        Debug.DrawLine(
//    controllerBottom,
//    controllerBottom + Vector3.up * 0.5f,
//    Color.red,
//    0f
//);

//        Debug.DrawLine(
//            controllerBottom + Vector3.right * 0.15f,
//            controllerBottom + Vector3.right * 0.15f + Vector3.up * 0.5f,
//            Color.red,
//            0f
//        );

//        Debug.DrawLine(
//            transform.position,
//            transform.position + Vector3.up * 2f,
//            Color.green,
//            5f
//        );

//        Debug.DrawRay(
//            controllerBottom + Vector3.up * 0.05f,
//            Vector3.down * 2f,
//            Color.yellow,
//            5f
//        );
    }
    void CheckPlayerParts()
    {
        //Debug.Log(
        //    "PLAYER PARTS | " +
        //    "ROOT Y: " + transform.position.y +
        //    " | BODY Y: " + body.position.y +
        //    " | CAMERA PIVOT Y: " + cameraPivot.position.y
        //);
    }
    
    float GetGroundCorrection()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;

        RaycastHit hit;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 5f))
        {
            float desiredRootY =
                hit.point.y
                - controller.center.y
                + controller.height * 0.5f;

            return desiredRootY - transform.position.y;
        }

        return 0f;
    }
    void FollowGround()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;

        RaycastHit hit;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 5f))
        {
            float desiredRootY =
                hit.point.y
                - controller.center.y
                + controller.height * 0.5f;

            float difference = desiredRootY - transform.position.y;

            if (Mathf.Abs(difference) > 0.001f)
            {
                controller.Move(Vector3.up * difference);
            }
        }
    }
    private bool CheckGround()
    {
        Vector3 origin = transform.position;

        float radius = 0.5f;
        float distance = 3.6f;

        if (Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out RaycastHit hit,
            distance,
            groundLayer))
        {
            //Debug.Log("HOPYA2 "+
            //    $"Ground: TRUE | " +
            //    $"Distance: {hit.distance} | " +
            //    $"Hit: {hit.collider.name} | " +
            //    $"HitY: {hit.point.y} | " +
            //    $"PlayerY: {transform.position.y}"
            //);

            return true;
        }

        //Debug.Log("HOPYA2 Ground: FALSE");

        return false;
    }
    bool CheckGroundDistance(out RaycastHit groundHit, out float groundDistance)
    {
        Vector3 center = transform.TransformPoint(controller.center);

        float worldHeight = controller.height * transform.lossyScale.y;

        Vector3 rayStart =
            center + Vector3.up * (worldHeight * 0.5f + 0.2f);

        float rayLength =
            worldHeight + groundCheckDistance + 0.5f;

        if (Physics.Raycast(
            rayStart,
            Vector3.down,
            out groundHit,
            rayLength,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            Vector3 localBottom =
                controller.center -
                Vector3.up * (controller.height * 0.5f);

            Vector3 worldBottom =
                transform.TransformPoint(localBottom);

            groundDistance =
                groundHit.point.y - worldBottom.y;

            return true;
        }

        groundDistance = Mathf.Infinity;
        return false;
    }
    void OnDrawGizmos()
    {
        if (controller == null)
            return;

        Vector3 localBottom =
            controller.center -
            Vector3.up * (controller.height * 0.5f);

        Vector3 worldBottom =
            transform.TransformPoint(localBottom);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(worldBottom, 0.08f);
    }
    void DebugControllerPosition()
    {
        Vector3 controllerBottom =
            transform.position +
            controller.center -
            Vector3.up * (controller.height / 2f);

        //Debug.Log(
        //    "CONTROLLER DEBUG | " +
        //    "PlayerY: " + transform.position.y +
        //    " | BottomY: " + controllerBottom.y +
        //    " | CenterY: " + controller.center.y +
        //    " | Height: " + controller.height +
        //    " | Scale: " + transform.localScale
        //);

    }
    private void ResetPosture()
    {
        isCrouching = false;
        controller.height = standingHeight;
        controller.center = new Vector3(
            0f,
            standingCenterY,
            0f);
        
    }
    private void UpdateSoldierCrouch()
    {
        float targetY = isCrouching
            ? soldierStandingLocalPosition.y + crouchModelOffset
            : soldierStandingLocalPosition.y;

        Vector3 targetPosition = new Vector3(
            soldierStandingLocalPosition.x,
            targetY,
            soldierStandingLocalPosition.z
            );
        float smooth = 1f - Mathf.Exp(
            -modelCrouchSpeed * Time.deltaTime
            );
        soldierModel.localPosition = Vector3.Lerp(
            soldierModel.localPosition,
            targetPosition,
            smooth
            
            );
    }
   
    public void SetAimOffset()
    {
        if (isCrouching)
        {
            //CROUCH
            Vector3 targetWeaponAim;
            bool isAiming = Input.GetMouseButton(1);
            if (isAiming) {targetWeaponAim = crouchWeaponAimPosition;  }
            else { targetWeaponAim = standWeaponAimPosition; }

                weaponAimTarget.localPosition = targetWeaponAim;
            rightHandTargetFollower.SetAimSettings(
                crouchAimRotationOffset,
                crouchAimLocalY,
                crouchForwardOffset
                );
            adsHeadAimController.SetAimSettings(
                crouchAdsCenter,
                crouchAdsUpward,
                crouchAdsDown
                );

            //weaponSystem.SetMuzzleOffsets(1.5f, 4f);

            //weaponSystem.SetMuzzleOffsets(1f, 2.7f);
            if (adsHeadAimController.isUpwardAimOffset)
            {
                weaponSystem.SetMuzzleOffsets(-crouchMuzzlePitchOffset, crouchMuzzleYawOffset);
                return;
            }
            weaponSystem.SetMuzzleOffsets(crouchMuzzlePitchOffset, crouchMuzzleYawOffset);
        }
        else
        {
            
            weaponAimTarget.localPosition = standWeaponAimPosition;
            rightHandTargetFollower.SetAimSettings(
                standAimRotationOffset,
                standAimLocalY,
                standForwardOffset
                );
            adsHeadAimController.SetAimSettings(
                standAdsCenter,
                standAdsUpward,
                standAdsDown
                );
            //weaponSystem.SetMuzzleOffsets(2f, 2.5f);
            if (adsHeadAimController.isUpwardAimOffset) { 
                weaponSystem.SetMuzzleOffsets(-standMuzzlePitchOffset, standMuzzleYawOffset);
                return;
            }
            weaponSystem.SetMuzzleOffsets(standMuzzlePitchOffset, standMuzzleYawOffset);

        }

    }
    private void UpdateAimOffset()
    {
        float target = isCrouching ? 1f : 0f;

        float smooth = 1f - Mathf.Exp(
            -aimCrouchTransitionSpeed * Time.deltaTime
            );
        aimCrouchAmount = Mathf.Lerp(
            aimCrouchAmount,
            target,
            smooth
            );
        //Weapon Aim Target
        //weaponAimTarget.localPosition = Vector3.Lerp(
        //    standWeaponAimPosition,
        //    crouchWeaponAimPosition,
        //    aimCrouchAmount
        //    );

        //Right Hand
        Vector3 aimRotation = Vector3.Lerp(
            standAimRotationOffset,
            crouchAimRotationOffset,
            aimCrouchAmount
            );

        float aimLocalY = Mathf.Lerp(
            standAimLocalY,
            crouchAimLocalY,
            aimCrouchAmount
            );
        float forwardOffset = Mathf.Lerp(
            standForwardOffset,
            crouchForwardOffset,
            aimCrouchAmount
            );
        rightHandTargetFollower.SetAimSettings(
            aimRotation,
            aimLocalY,
            forwardOffset
            );

        //ADS Head
        float center = Mathf.Lerp(
            standAdsCenter,
            crouchAdsCenter,
            aimCrouchAmount
            );
        float upward = Mathf.Lerp(
            standAdsUpward,
            crouchAdsUpward,
            aimCrouchAmount
            );
        float down = Mathf.Lerp(
            standAdsDown,
            crouchAdsDown,
            aimCrouchAmount
            );
        adsHeadAimController.SetAimSettings(
           center,
           upward,
           down
       );
    }
    // Update is called once per frame
    void Update()
    {
       
        //RaycastHit groundHit;
        //float groundDistance;

        //bool hasGround =
        //    CheckGroundDistance(out groundHit, out groundDistance);

        //bool grounded =
        //    hasGround &&
        //    Mathf.Abs(groundDistance) <= groundSnapDistance;

        if (controller.isGrounded)
        {
            verticalVelocity = 0f;
        }
        else
        {
            //verticalVelocity = 0f;
            verticalVelocity += gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -20f);
        }
//        Debug.Log(
//    "HOPYA" +
//    " | Ground: " + hasGround +
//    " | Distance: " + groundDistance +
//    " | Hit: " + (hasGround ? groundHit.collider.name : "NONE") +
//    " | HitY: " + (hasGround ? groundHit.point.y.ToString() : "NONE") +
//    " | PlayerY: " + transform.position.y
//);
        //CheckGround();
        //Debug.Log("isGrounded : "+ controller.isGrounded+" vertical velocity: "+ verticalVelocity);
        //Debug.Log(
    //"GROUND DEBUG " +
    //" | CustomGrounded: " + grounded +
    //" | ControllerGrounded: " + controller.isGrounded +
    //" | GroundDistance: " + groundDistance +
    //" | VerticalVelocity: " + verticalVelocity
//);
        DebugControllerPosition();
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {

            isCrouching = !isCrouching;

           

            //Debug.Log("PLAYER CONTROLLER Is Crouching:(T/F)" + isCrouching);
            

        }

        //if (isInCover) return;
        SetAimOffset();
        UpdateCrouch(); //SMOOTH SHAKING
        UpdateSoldierCrouch();
        UpdateAimOffset();
        soldierAnimator.SetBool("IsCrouching", isCrouching);
        //UpdateBodyCrouch();

        //UpdateBodyCrouch();//SMOOTH SHAKING HOW DO YOU UPDATE THIS CROUCH WHEN CROUCH IN RIGS AND WHAT MAKES THESE 2 CODES SMOOTH WHEN RIGHT CLICK AIMING? HOW DO YOU UPDATE IT NOW?
        //UpdateCameraCrouch();

        DrawPlayerGroundDebug();
        //Debug.Log("PLAYER CONTROLLER | Height: "+controller.height+" | center: "+controller.center);
        //Debug.Log("PLAYER CONTROLLER BODY | POSITION: " + body.localPosition);

        if (isCrouching) {
           
            moveSpeeds = crouchSpeed; }
        else { moveSpeeds = walkSpeed; }
        //moveSpeed = isCrouching ? crouchSpeed : walkSpeed;
        //Debug.Log("Move Speed 23:" + moveSpeeds);
        if (hasDestination)
        {
            direction = destination - transform.position;
            direction.y = 0f;
            
            if (direction.magnitude < 1f)
            {
                hasDestination = false;
                if (currentCover != null)//3RD
                {
                    EnterCover(currentCover);
                }
                direction = Vector3.zero;
                //return;
            }
            //else
            //{
                
            //    direction = direction * walkSpeed;

            //}
            madeNoise = true;
        }
        else
        {
            direction = Vector3.zero;
            madeNoise = false;
        }
        direction.Normalize();
        movement = direction * moveSpeeds;

        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);

        bool moving = hasDestination & direction.sqrMagnitude > 0.001f;
        soldierAnimator.SetBool("IsMoving",moving);


            //if (firstMatchPerRound)
            //{

            //    verticalVelocity = -1;
            
            //}
            //if (controller.isGrounded)
            //{
            //    firstMatchPerRound = false;
            //}
            //Debug.Log("isGrounded : verticalVelocity " + verticalVelocity);
            //movement.y = 0;
            //movement.y = verticalVelocity;
            //float groundCorrection = GetGroundCorrection();

            //if (Mathf.Abs(groundCorrection) > 0.001f)
            //{
            //    movement.y = groundCorrection / Time.deltaTime;
            //}
            //else if (controller.isGrounded)
            //{
            //    movement.y = -1f;
            //}
            //else
            //{

            //}

            // Horizontal movement
            //movement.y = 0f;

            // Move horizontally
            //controller.Move(movement * Time.deltaTime);

            // Check ground AFTER horizontal movement
            //RaycastHit afterMoveHit;
            //float afterMoveDistance;

            //bool hasGroundAfterMove =
            //    CheckGroundDistance(
            //        out afterMoveHit,
            //        out afterMoveDistance
            //    );

            //if (hasGroundAfterMove)
            //{
            //    if (Mathf.Abs(afterMoveDistance) <= groundSnapDistance)
            //    {
            //        Vector3 localBottom =
            //            controller.center -
            //            Vector3.up * (controller.height * 0.5f);

            //        Vector3 worldBottom =
            //            transform.TransformPoint(localBottom);

            //        float correction =
            //            afterMoveHit.point.y - worldBottom.y;

            //        controller.Move(
            //            Vector3.up * correction
            //        );

            //        verticalVelocity = -1f;
            //    }
            //}
            //movement.y = verticalVelocity;
            //float groundCorrection = GetGroundCorrection();
            //movement.y = verticalVelocity + (groundCorrection / Time.deltaTime);

            //movement.y = verticalVelocity;
            //movement.y = 0;
            //controller.Move(movement * Time.deltaTime);
            //FollowGround();
            //     CheckGroundHeight();
            //     Debug.Log(
            //    "PLAYER ROOTS | Root Y: " + transform.position.y +
            //    " | Grounded: " + controller.isGrounded +
            //    " | Body Y: " + body.position.y
            //);
            
        
        
        //else
        //    {
        //        movement = Vector3.zero;
        //    }




        //        if (hasDestination) //2ND STEP
        //        {
        //            direction = destination - transform.position;

        //            direction.y = 0f;

        //            if (direction.magnitude < 0.5f)
        //            {
        //                hasDestination = false;

        //                if (currentCover != null)//3RD
        //                {
        //                    EnterCover(currentCover); 
        //                }
        //                return;
        //            }


        //            direction.Normalize();
        //            movement = direction * moveSpeeds;
        //            movement.y = verticalVelocity;
        //            Debug.Log(
        //    "PLAYER PARTS BEFORE MOVE | Root Y: " +
        //    transform.position.y +
        //    " | Grounded: " +
        //    controller.isGrounded +
        //    " | VerticalVelocity: " +
        //    verticalVelocity
        //);

        //            controller.Move(movement * Time.deltaTime);
        //            RaycastHit afterMoveHit;

        //            Vector3 afterMoveRayStart =
        //                transform.position + Vector3.up * 0.2f;

        //            if (Physics.Raycast(
        //                afterMoveRayStart,
        //                Vector3.down,
        //                out afterMoveHit,
        //                3f
        //            ))
        //            {
        //                Debug.Log(
        //                    "PLAYER PARTS AFTER MOVE GROUND | " +
        //                    "Root Y: " + transform.position.y +
        //                    " | Ground Y: " + afterMoveHit.point.y +
        //                    " | Difference: " +
        //                    (transform.position.y - afterMoveHit.point.y) +
        //                    " | Hit: " +
        //                    afterMoveHit.collider.name
        //                );
        //            }

        //            CheckGroundHeight();
        //            CheckPlayerParts();

        //            Debug.Log(
        //    "HERO AFTER MOVE | Root Y: " +
        //    transform.position.y +
        //    " | Grounded: " +
        //    controller.isGrounded
        //);
        //            madeNoise = true;

        //            Debug.Log("Made Noise Is On!");
        //        }
        //        else 
        //        {
        //            madeNoise = false;
        //        }






    }
}
