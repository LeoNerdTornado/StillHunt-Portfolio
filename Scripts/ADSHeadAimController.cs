using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ADSHeadAimController : MonoBehaviour
{
    // Start is called before the first frame update

    [Header("References")]
    public Transform head;
    public Transform headParent;
    public Transform adsRearSight;
    public Transform adsFrontSight;
    //public Transform adsCameraAimPoint;
    public Camera playerCamera;

    public float aimDownwardOffset = 0.8f;
    public float aimUpwardOffset = 0.55f;
    public float aimCenterOffset = 0.5f;

    [Header("Position Tuning")]
    public float positionSpeed = 10f;
    public float maxVerticalCorrection = 0.30f;

    [Header("Directional ADS Offset")]
    [Tooltip("Center / left / right aiming offset.")]
    public Vector3 centerAimOffset;  //0.5
    
    [Tooltip("Upward aiming offset.")]
    public Vector3 upwardAimOffset;//0.55

    [Tooltip("Downward aiming offset.")]
    public Vector3 downwardAimOffset;//0.8

    //public void Awake()
    //{
    //    centerAimOffset = new Vector3(0f, aimCenterOffset, 0f);
    //    upwardAimOffset = new Vector3(0f, aimUpwardOffset, 0f);
    //    downwardAimOffset = new Vector3(0f, aimDownwardOffset, 0f);
    //}

    [Header("Vertical Direction Detection")]
    [Tooltip("Camera pitch above this angle is considered upward.")]
    public float upwardThreshold = 5f;

    [Tooltip("Camera pitch below this angle is considered downward.")]
    public float downwardThreshold = -5f;

    [Header("Dynamic ADS Vertical Offset")]
    [Tooltip("Looking downward uses this Y offset.")]
    public float downAimOffsetY = 0.85f;

    [Tooltip("Looking upward uses this Y offset.")]
    public float upAimOffsetY = 0.50f;

    [Tooltip("Looking approximately level uses zero offset.")]
    public float centerAimOffsetY = 0f;



    [Tooltip("Pitch range considered 'center'.")]
    public float centerPitchThreshold = 5f;

    [Tooltip("How quickly the ADS offset changes.")]
    public float aimOffsetSpeed = 10f;

    private Vector3 currentAdsAimOffset = Vector3.zero;

    [Header("Ads Position")]
    public Vector3 adsHeadLocalPosition = new Vector3(0f,0.2f,-0.6f);


    [Header("Position Limits")]
    [Tooltip("Maximum automatic correction from the ADS zero position.")]
    public Vector3 maxPositionCorrection = new Vector3(
        0.20f,
        0.30f,
        0.20f
    );

    [Header("Eye Relief")]
    public float eyeRelief = 0.60f;

  
    //[Tooltip("Fine-tunes the ADS camera/head position after sight alignment.")]
    //public Vector3 adsAimOffset = new Vector3(0f, 0f, 0f);

    [Header("Head Position")]
    public float normalHeadY = 0f;


    


    [Header("Aim Settings")]
    public float rotationSpeed = 10f;

    [Header("Vertical ADS Behavior")]
    public bool preserveCameraHeight = false;
    public float verticalAimOffset = 0f;

    public bool isUpwardAimOffset = false;

    private bool isAiming = false;
    private bool wasAiming = false;
    void Start()
    {
        
    }
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }
    private void OnDrawGizmos()
    {
        if (adsRearSight == null ||
            adsFrontSight == null ||
            playerCamera == null)
        {
            return;
        }


        // =========================================================
        // MAGENTA = PHYSICAL REAR -> FRONT SIGHT AXIS
        // =========================================================

        Gizmos.color = Color.magenta;

        Gizmos.DrawLine(
            adsRearSight.position,
            adsFrontSight.position
        );


        // =========================================================
        // BLUE = CAMERA FORWARD
        // =========================================================

        Gizmos.color = Color.blue;

        Gizmos.DrawLine(
            playerCamera.transform.position,
            playerCamera.transform.position +
            playerCamera.transform.forward * 5f
        );


        // =========================================================
        // YELLOW = IDEAL EYE POSITION
        // =========================================================

        Vector3 sightDirection =
            (adsFrontSight.position -
             adsRearSight.position).normalized;

        Vector3 desiredCameraPosition =
            adsRearSight.position -
            sightDirection * eyeRelief;

        Gizmos.color = Color.yellow;

        Gizmos.DrawSphere(
            desiredCameraPosition,
            0.05f
        );


        Gizmos.DrawLine(
            desiredCameraPosition,
            adsRearSight.position
        );


        // =========================================================
        // CYAN = CURRENT CAMERA -> REAR SIGHT
        // =========================================================

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            playerCamera.transform.position,
            adsRearSight.position
        );
    }
    private void LateUpdate()
    {
        if (head == null ||
            adsRearSight == null ||
            adsFrontSight == null ||
            playerCamera == null)
        {
            return;
        }


        // =========================================================
        // ENTER ADS
        // =========================================================

        if (isAiming && !wasAiming)
        {
            // Always begin from the known ADS zero position.
            head.localPosition = adsHeadLocalPosition;
            
            // Reset dynamic offset.
            currentAdsAimOffset = Vector3.zero;
        }


        // =========================================================
        // EXIT ADS
        // =========================================================

        if (!isAiming)
        {
            wasAiming = false;
            currentAdsAimOffset = Vector3.zero;
            return;
        }

        wasAiming = true;


        // =========================================================
        // 1. CALCULATE PHYSICAL REAR -> FRONT SIGHT DIRECTION
        // =========================================================

        Vector3 sightDirection =
            adsFrontSight.position -
            adsRearSight.position;

        if (sightDirection.sqrMagnitude < 0.0001f)
            return;

        sightDirection.Normalize();


        // =========================================================
        // 2. CALCULATE WHERE THE EYE/CAMERA SHOULD BE
        // =========================================================
        //
        // Eye must sit behind the rear sight,
        // directly on the Rear -> Front sight axis.
        //
        //             Front
        //               X
        //              /
        //             /
        //            X Rear
        //           /
        //          /
        //        EYE
        //
        // eyeRelief = distance from eye to rear sight
        //

        Vector3 desiredCameraPosition =
            adsRearSight.position -
            sightDirection * eyeRelief;


        // =========================================================
        // 3. CALCULATE CAMERA POSITION ERROR
        // =========================================================

        Vector3 cameraPositionError =
            desiredCameraPosition -
            playerCamera.transform.position;


        // =========================================================
        // 4. MOVE THE HEAD BY THAT EXACT WORLD ERROR
        // =========================================================
        //
        // Translating the head moves the camera with it.
        //
        // Convert world-space movement into the head parent's
        // local coordinate system.
        //

        Transform parent = head.parent;

        if (parent == null)
            return;

        Vector3 localCorrection =
            parent.InverseTransformVector(cameraPositionError);


        // =========================================================
        // 5. LIMIT THE CORRECTION
        // =========================================================
        
        localCorrection.x = Mathf.Clamp(
            localCorrection.x,
            -maxPositionCorrection.x,
            maxPositionCorrection.x
        );

        localCorrection.y = Mathf.Clamp(
            localCorrection.y,
            -maxPositionCorrection.y,
            maxPositionCorrection.y
        );

        localCorrection.z = Mathf.Clamp(
            localCorrection.z,
            -maxPositionCorrection.z,
            maxPositionCorrection.z
        );


        // =========================================================
        // 6. CALCULATE FINAL HEAD POSITION
        // =========================================================
        
        Vector3 directionalAimOffset =
                GetDirectionalAimOffset();
     DevLog.Log("Camera→Head2 : "+ directionalAimOffset);
        Vector3 targetLocalPosition =
                adsHeadLocalPosition +
                localCorrection +
                directionalAimOffset;

        //    Vector3 targetLocalPosition =
        //adsHeadLocalPosition +
        //localCorrection +
        //adsAimOffset;


        // =========================================================
        // 7. SMOOTHLY MOVE HEAD
        // =========================================================

        head.localPosition = Vector3.Lerp(
            head.localPosition,
            targetLocalPosition,
            Time.deltaTime * positionSpeed
        );
    }

    private Vector3 CalculateDynamicAdsAimOffset()
    {
        if ((playerCamera == null) || (headParent == null)) return Vector3.zero;

        // Convert camera forward direction into the head parent's local space.
        Vector3 localCameraForward = headParent.InverseTransformDirection(playerCamera.transform.forward);

        // Calculate vertical pitch.
        // Positive = looking UP
        // Negative = looking DOWN

        float pitch = Mathf.Atan2(
            localCameraForward.y,
            new Vector2(
                localCameraForward.x,
                localCameraForward.z
                ).magnitude
            ) * Mathf.Rad2Deg;

        float targetY;

        // ---------------------------------------------------------
        // LOOKING DOWN
        // ---------------------------------------------------------
        
        if(pitch < -centerPitchThreshold)
        {
            targetY = downAimOffsetY;
        }
        // ---------------------------------------------------------
        // LOOKING UP
        // ---------------------------------------------------------
       else if (pitch > centerPitchThreshold)
        {
            targetY = upAimOffsetY;

        }
        // ---------------------------------------------------------
        // CENTER
        // ---------------------------------------------------------
        else
        {

            targetY = centerAimOffsetY;
        }
        Vector3 targetOffset = new Vector3(0f,
            targetY,
            0f
            );
        currentAdsAimOffset = Vector3.Lerp(
            currentAdsAimOffset,
            targetOffset,
            Time.deltaTime * aimOffsetSpeed
            );
        return currentAdsAimOffset;
    }
    private Vector3 GetDirectionalAimOffset()
    {
        if (playerCamera == null) return centerAimOffset;

        float verticalAngle =
            Mathf.Asin(playerCamera.transform.forward.y) *
            Mathf.Rad2Deg;

        if(verticalAngle > upwardThreshold)
        {
            isUpwardAimOffset = true;
            return upwardAimOffset;

        }
        if(verticalAngle < downwardThreshold)
        {
            isUpwardAimOffset = false;
            return downwardAimOffset;
        }
        isUpwardAimOffset = false;
        return centerAimOffset;
        
    }
    public void SetAimSettings(float center, float upward, float down)
    {// Store float values
        aimCenterOffset = center;
        aimUpwardOffset = upward;
        aimDownwardOffset = down;

        // Store dynamic Y values
        centerAimOffsetY = center;
        upAimOffsetY = upward;
        downAimOffsetY = down;

        // Store Vector3 values
        centerAimOffset = new Vector3(0f, center, 0f);
        upwardAimOffset = new Vector3(0f, upward, 0f);
        downwardAimOffset = new Vector3(0f, down, 0f);

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
