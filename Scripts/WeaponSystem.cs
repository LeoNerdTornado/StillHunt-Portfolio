using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{

    public LayerMask hitMask;

   
    // Start is called before the first frame update
    public Transform playerTarget;
    public Transform guardTarget;

    [Header("Muzzle")]
    public Transform muzzle;

    [Header("Weapon Effects")]
    public GameObject bulletTracerPrefab;
    public GameObject muzzleFlashPrefab;

    [Header("Muzzle Zero Distance")]
    public float muzzleZeroDistance;

    [Header("Muzzle Direction Tuning")]
    public float muzzlePitchOffset = 2f;
    public float muzzleYawOffset = 2.5f;
    public float muzzleRollOffset = 0f;

    [Header("Weapon Zeroing")]
    public float zeroingDistance = 100f;

    [Header("Weapon Ads Sight")]
    public Transform weaponSight;
    public Transform adsRearSight;
    public Transform adsFrontSight;

    public Camera playerCamera;
    public Player player;
    public float normalFLOV = 60F;
    public float zoomFOV = 20f;
    public float zoomSpeed = 10f;
    public float normalSensitivity = 2f;
    public float zoomSensitivity = 0.5f;
    public float maxBreathTime = 4f;
    public float breathRecoverySpeed = 2f;
    public float swayAmount = 0.03f;
    public float swaySpeed = 1f;
    public float maxShotDistance = 100f;


    PlayerMovement playerMovement;

    private float currentBreathTime;

    public bool isHoldingBreath = false;
    public bool isAiming = false;

    private Vector3 originalLocalPosition;
    private int shotsFromCurrentPosition = 0;

    [SerializeField] private int fallbackBodyDamage = 15;

    public GuardVision guardVision;
    public GuardAI guardAI;
    public GuardCover guardCover;

    public GuardMovement guardMovement;
    Player plaYer;
    void Start()
    {
        playerCamera = GetComponent<Camera>();
        playerCamera.fieldOfView = normalFLOV;
        currentBreathTime = maxBreathTime;
        originalLocalPosition = transform.localPosition;
        player = playerTarget.GetComponent<Player>();
        guardVision = guardTarget.GetComponent<GuardVision>();
        guardCover = guardTarget.GetComponent<GuardCover>();
        guardAI = guardTarget.GetComponent<GuardAI>();
        //plaYer = guardAI.player.GetComponent<Player>();
        //playerMovement = plaYer.GetComponent<PlayerMovement>();
        guardMovement = guardTarget.GetComponent<GuardMovement>();

    }
    
    private Ray GetAdsRay()
    {
        if((adsRearSight == null)||(adsFrontSight == null))
        {
            //Debug.LogWarning("ADS Rear Sight or ADS Front Sight is missing.");
            return new Ray(transform.position, transform.forward);
        }
        Vector3 direction = adsFrontSight.position - adsRearSight.position;

        if(direction.sqrMagnitude < 0.0001f)
        {
            //Debug.LogWarning("ADS Rear Sight and ADS Front Sight are too close.");
            return new Ray(
                adsRearSight.position,
                adsRearSight.forward
                );
        }
        direction.Normalize();

        return new Ray(
            adsRearSight.position,
            direction
            );

    }
    private Vector3 CalculateMuzzleDirection()
    {
        if (muzzle == null)
            return Vector3.zero;

        if (adsRearSight == null || adsFrontSight == null)
            return muzzle.forward;
        Ray adsRay = GetAdsRay();

        // Find the point where the ADS line reaches the zeroing distance.
        Vector3 zeroPoint = adsRay.GetPoint(zeroingDistance);

        // Base physical muzzle direction toward the zero point.

        Vector3 direction =
            (zeroPoint -
            muzzle.position).normalized;

        // Apply manual tuning.
        Quaternion offsetRotation =
            Quaternion.Euler(
                muzzlePitchOffset,
                muzzleYawOffset,
                muzzleRollOffset
            );

        direction = offsetRotation * direction;

        return direction.normalized;
    }
    private Vector3 GetAdsTargetPoint(float distance)
    {
        Ray adsRay = GetAdsRay();
        return adsRay.origin + adsRay.direction * distance;

    }
    private void SpawnMuzzleFlash()
    {
        if ((muzzle == null) || (muzzleFlashPrefab == null)) return;

        GameObject flash = Instantiate(
            muzzleFlashPrefab,
            muzzle.position,
            muzzle.rotation
            );
        Destroy(flash,0.15f);

    }
   private void SpawnBulletTracer(Vector3 startPosition, Vector3 endPosition)
    {
        if (bulletTracerPrefab == null) return;

        GameObject tracerObject = Instantiate(
            bulletTracerPrefab,
            startPosition,
            Quaternion.identity
            );
        PlayerBulletTracer playerBulletTracer = tracerObject.GetComponent<PlayerBulletTracer>();


        if (playerBulletTracer != null)
        {
            playerBulletTracer.Initialize(
                startPosition,
                endPosition,
                300f
                );
        }
    }
    public void SetMuzzleOffsets(float pitch, float yaw)
    {
        muzzlePitchOffset = pitch;
        muzzleYawOffset = yaw;

    }
    private void UpdatePlayerAimThreat()
    {
        if ((guardVision == null)||(guardAI == null))
        {
            return;
        }
        if (muzzle == null)
        {
            return;
        }
        Vector3 muzzleDirection = CalculateMuzzleDirection();

        float suppressionLevel = guardVision.EvaluatePlayerAimThreat(
            muzzle.position,
            muzzleDirection
            );

        if (suppressionLevel > 0) {
            guardAI.suppressionAimTime += Time.deltaTime;
                if (guardAI.suppressionAimTime >= guardAI.suppressionAimTimer)
                {
                    guardAI.suppressionAimTime = 0f;
                    //Debug.Log(
                    //    "PLAYER AIM THREAT | " +
                    //    "SUPPRESSION: " +
                    //    suppressionLevel
                    //    );
                    guardAI.AddSuppression(guardAI.suppressionAimLevel);
                }
            }
        }

    // Update is called once per frame
    void Update()
    {

        //if (weaponSight != null)
        //{
        //    Debug.DrawLine(
        //        weaponSight.position,
        //        weaponSight.position + 
        //        weaponSight.forward * 10f,
        //        Color.red
        //        );

        //}

        UpdatePlayerAimThreat();

        Vector3 sway = new Vector3(
           Mathf.Sin(Time.time * swaySpeed),
           Mathf.Cos(Time.time * swaySpeed),
           0f
           ) * swayAmount;
        float currentSway = swayAmount;
        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftShift) && currentBreathTime > 0f)
        {
            isHoldingBreath = true;
        }
        else {
            isHoldingBreath = false;
        }
        //Debug.Log("Is Holding Breath? "+ isHoldingBreath);
        if (isHoldingBreath)
        {
            currentBreathTime -= Time.deltaTime;
            currentSway *= 0.2f;
        }
        else if (!isHoldingBreath) {

            currentBreathTime += breathRecoverySpeed * Time.deltaTime;
            currentBreathTime = Mathf.Clamp(
                currentBreathTime,
                0f,
                maxBreathTime
                );
            
        }
            //Debug.Log("current Breath Time? " + currentBreathTime);


        //if (Input.GetMouseButton(1))
        //{
        //    isAiming = true;

        //    playerCamera.fieldOfView = Mathf.Lerp(
        //        playerCamera.fieldOfView,
        //        zoomFOV,
        //        Time.deltaTime * zoomSpeed
        //        );
        //    transform.localPosition =
        //originalLocalPosition + sway;
        //}
        //else
        //{
        //    isAiming = false;

        //}
        //Debug.Log("isAiming : "+ isAiming);
        //else
        //{
        //    playerCamera.fieldOfView = Mathf.Lerp(
        //        playerCamera.fieldOfView,
        //        normalFLOV,
        //        Time.deltaTime * zoomSpeed
        //        );
        //    transform.localPosition = originalLocalPosition;
        //}

        if ((Input.GetMouseButtonDown(0)) && (Input.GetKey(KeyCode.Space)))
        {


        }

        else if (Input.GetMouseButtonDown(0))
        {
            guardAI = FindObjectOfType<GuardAI>();

            
            if (guardAI == null) { return; }
            if (!guardAI.gameObject.activeInHierarchy) { return; }
            if (guardAI != null)
            {

                guardVision.HearGunshot(transform.position);

            }
            //Ray ray = playerCamera.ScreenPointToRay(
            //      new Vector3(
            //          Screen.width / 2,
            //          Screen.height / 2,
            //          0
            //          )
            //    );
            //Ray ray = new Ray(weaponSight.position, weaponSight.forward);

            Ray adsRay = GetAdsRay();
            SpawnMuzzleFlash();
          

            Vector3 adsTargetPoint = GetAdsTargetPoint(100);

            //Vector3 targetPoint = adsRay.origin +
            //                    adsRay.direction * muzzleZeroDistance;

            Vector3 muzzleDirection = CalculateMuzzleDirection();

            Ray ray = new Ray(
                muzzle.position,
                muzzleDirection
                );
            //if (adsRearSight != null && adsFrontSight != null)
            //{
            //                    Debug.DrawRay(
            //           ray.origin,
            //           ray.direction * 100f,
            //           Color.magenta,
            //           10f
            //       );
            //}
            Vector3 rawMuzzleDirection =
             (adsTargetPoint - muzzle.position).normalized;

            Debug.DrawLine(
                muzzle.position,
                muzzle.position + rawMuzzleDirection * 100f,
                Color.green,
                10f
            );


            // MAGENTA = physical ADS sight line
            // =========================================================
            // MAGENTA = PHYSICAL ADS REAR -> FRONT SIGHT DIRECTION
            // =========================================================

            if (adsRearSight != null && adsFrontSight != null)
            {
                Debug.DrawRay(
                    ray.origin,
                    ray.direction * 100f,
                    Color.magenta,
                    10f
                );
            }


            // =========================================================
            // YELLOW = MUZZLE -> ADS TARGET
            // =========================================================

            Debug.DrawRay(
            muzzle.position,
            muzzleDirection * maxShotDistance,
            Color.yellow,
            10f
            );


            // =========================================================
            // BLUE = ACTUAL BULLET / MUZZLE DIRECTION
            // =========================================================

            Debug.DrawLine(
                muzzle.position,
                muzzle.position + muzzleDirection * 100f,
                Color.blue,
                10f
            );

            float suppressionAmount = guardAI.CalculateBulletSuppression(ray.origin, ray.direction);


            RaycastHit[] hits = Physics.RaycastAll(ray, maxShotDistance, hitMask);

            Vector3 tracerEndPoint = ray.origin + ray.direction * maxShotDistance;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            bool foundHitZone = false;
            bool foundGuardBody = false;
            RaycastHit hit = default;
            RaycastHit missHit = default;
            bool hasMissHit = false;

            foreach (RaycastHit currentHit in hits)
            {

                //HitZone hitZone = currentHit.collider.GetComponentInParent<HitZone>();
                HitZone hitZone = currentHit.collider.GetComponent<HitZone>();
                if (hitZone != null)
                {
                    hit = currentHit;
                    foundHitZone = true;
                    tracerEndPoint = currentHit.point;
                    //Debug.Log("FOUND: " + currentHit.collider.name);
                    break;
                }
                GuardAI hitGuard = currentHit.collider.GetComponentInParent<GuardAI>();
                if (hitGuard != null)
                {
                    foundGuardBody = true;
                    hit = currentHit;
                    tracerEndPoint = currentHit.point;
                    //Debug.Log("Ignoring Guard parent collider: " + currentHit.collider.name);
                    continue;
                }
                //Debug.Log("Bullet Blocked by: " + currentHit.collider.name);
                missHit = currentHit;
                hasMissHit = true;
                tracerEndPoint = currentHit.point;
                break;
            }
            if (foundHitZone)
            {
                HitZone hitZone = hit.collider.GetComponent<HitZone>();
                float damage = hitZone.GetDamage();
                DevLog.Log("hitBox hit ZONE: " + hitZone.zoneType + " | Damage: " + damage + " guardMovement: " + guardMovement);
                if (guardMovement != null)
                {
                    suppressionAmount = 200f;
                    guardMovement.TakeDamage(Mathf.RoundToInt(damage));

                }
            }
            else if (foundGuardBody)
            {
                DevLog.Log("hitbox hit GUARD BODY HIT - FALLBACK DAMAGE: " + fallbackBodyDamage);
                if (guardMovement != null)
                {
                    suppressionAmount = 200f;
                    guardMovement.TakeDamage(fallbackBodyDamage);
                }
            }
            else
            {
                DevLog.Log("hitbox hit SHOT MISS GUARD");

            }
            SpawnBulletTracer(ray.origin, tracerEndPoint);

            if ((guardAI != null) && (suppressionAmount > 0f))
            {
                guardAI.AddSuppression(suppressionAmount);
                //Debug.Log("SHOT SUPPRESSION APPLIED: " + suppressionAmount);
            }

            if ((!foundHitZone) && (!foundGuardBody) && (hasMissHit))
            {
                guardAI.AddSuppression(suppressionAmount);
                if (guardCover != null)
                {
                    guardCover.AddSideSuppression(missHit.point, 20f);
                    //Debug.Log("NEAR MISS COVER SUPPRESSION APPLIED " + missHit.point);

                }
            }
            //guardVision.PlayerRepeatedShot(player.transform.position); //INVESTIGATE BEFORE CONCEAL
            //RaycastHit hit;


            //if (Physics.Raycast(ray, out hit))
            //{


            //HitZone hitZone = hit.collider.GetComponent<HitZone>();

            //if(hitZone != null)
            //{
            //    float damage = hitZone.GetDamage();


            //}
            //if (hit.collider.CompareTag("Enemy"))
            //{
            //HitZone hitZone = hit.collider.GetComponent<HitZone>();
            //Debug.Log("hitbox Ray Hit: " + hit.collider.name);
            //guardAI = hit.collider.GetComponent<GuardAI>();
            //if (hitZone != null)
            //{
            //    float damage = hitZone.GetDamage();
            //    //guardMovement.TakeDamage(1);
            //Debug.Log("hitbox HIT ZONE : " + hitZone.zoneType+" | Damage: "+damage);
            //}


            //}
            //else
            //{
            //    //Debug.Log("Observe2 Decision Dial ");
            //    float amount = guardAI.CalculateBulletSuppression(ray.origin, ray.direction);
            //    //Debug.Log("Observe2 Decision LAG amount:"+ amount);
            //    guardAI.AddSuppression(amount);
            //    guardCover.AddSideSuppression(hit.point, 20f);
            //}



            //}

        }
        //if ((Input.GetMouseButtonDown(0)) && (Input.GetKey(KeyCode.Space)))
        //{

        //    shotsFromCurrentPosition = 0;
        //    Debug.Log("Prev Shots From Current Position is " + shotsFromCurrentPosition);
        //}

    }
    private void OnDrawGizmos()
    {
        if (muzzle == null)
            return;

        Vector3 muzzleDirection = CalculateMuzzleDirection();

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            muzzle.position,
            muzzle.position + muzzleDirection * 500f
        );

        Gizmos.DrawSphere(
            muzzle.position + muzzleDirection * 5f,
            0.025f
        );
    }
}
