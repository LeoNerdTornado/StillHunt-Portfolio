using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static UnityEngine.GraphicsBuffer;

public class WeaponADSController : MonoBehaviour
{
    // Start is called before the first frame update


    [Header("Head Aim")]
    public ADSHeadAimController headAimController;

    [Header("Aim Debug")]
    public Transform headAimReference;
    public Transform weaponSight;

    [Header("Aim Reference")]
    
    public Transform weaponAimTarget;

    [Header("Camera")]
    public Camera playerCamera;

    [Header("Head")]
    public Transform head;

    [Header("Weapon Rig")]
    public TwoBoneIKConstraint weaponRig;

    [Header("Right Hand Target")]
    public WeaponAimTargetFollower rightHandTargetFollower;

   

    [Header("Ads Setting")]
    public float normalFOV = 60f;
    public float aimFOV = 15f;

    public Vector3 normalHeadPosition = new Vector3(0f, 0.2f, -0.6f);
    public Vector3 aimHeadPosition = Vector3.zero;

    //public float normalHandY;
    //public float aimHandY = 0.85f;
    public float transitionSpeed = 10f;

    private bool isAiming;
    private bool tempBool;

    void Start()
    {
       
        if (playerCamera != null) { playerCamera.fieldOfView = normalFOV; }

        if (head != null) { head.localPosition = normalHeadPosition; }

        if (weaponRig != null) { weaponRig.data.targetRotationWeight = 1f; }
        if (rightHandTargetFollower != null) {
            rightHandTargetFollower.SetAiming(false);
        }
        if (headAimController!=null)
        {
            headAimController.SetAiming(false);
        }


    }
    private void UpdateAimDebug()
    {
        if (playerCamera == null ||
            headAimReference == null ||
            weaponSight == null)
            return;

        float cameraHeadAngle = Vector3.Angle(
            playerCamera.transform.forward,
            headAimReference.forward
        );

        float cameraSightAngle = Vector3.Angle(
            playerCamera.transform.forward,
            weaponSight.forward
        );

        DevLog.Log(
            "Camera→Head : " +
            cameraHeadAngle.ToString("F2") +
            " | Camera→Sight: " +
            cameraSightAngle.ToString("F2")
        );
    }
    void UpdateAds()
    {


        if (playerCamera != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * transitionSpeed
                );
        }
        //if (head != null)
        //{
        //    Vector3 targetPosition = isAiming ? aimHeadPosition : normalHeadPosition;
        //    Debug.Log("is rightAiming head position: " + targetPosition);
        //    head.localPosition = Vector3.Lerp(
        //        head.localPosition,
        //        targetPosition,
        //        Time.deltaTime * transitionSpeed
        //        );
        //}
        if (rightHandTargetFollower != null)
        {
            rightHandTargetFollower.SetAiming(isAiming);
        }
        if (headAimController != null)
        {
            headAimController.SetAiming(isAiming);
        }

        if (weaponRig != null)
        {
            float targetWeight = 1f;
            //float targetWeight = isAiming ? 0f : 1f;
            //Debug.Log("is rightAiming targetWeight : " + targetWeight);
            weaponRig.data.targetRotationWeight = targetWeight;
            //weaponRig.data.targetRotationWeight = Mathf.Lerp(
            //    weaponRig.data.targetRotationWeight,
            //    targetWeight,
            //    Time.deltaTime * transitionSpeed
            //    );

        }

        
    }
    private void OnDrawGizmos()
    {
        if (playerCamera == null) return;

        //CAMERA AIM LINE
        //Gizmos.DrawLine(
        //    playerCamera.transform.position,
        //    playerCamera.transform.position + 
        //    playerCamera.transform.forward * 5f
        //    );
        //WEAPON SIGHT AIM LINE
        //if(weaponSight!= null)
        //{
        //    Gizmos.DrawLine(
        //        weaponSight.position,
        //        weaponSight.position +
        //        weaponSight.forward * 10f
        //        );
        //}


    }
    private void UpdateAimDifference()
    {
        if (playerCamera == null) return;

        WeaponSightDebug sightDebug =
            FindObjectOfType<WeaponSightDebug>();

        if (sightDebug == null) return;

        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 sightForward = sightDebug.transform.forward;

        float angleDifference =
            Vector3.Angle(cameraForward, sightForward);

        //Debug.Log(
        //    "ADS ANGLE DIFFERENCE2 : " +
        //    angleDifference.ToString("F2") +
        //    " degrees"
        //);
    }
    // Update is called once per frame
    void Update()
    {
        
        isAiming = Input.GetMouseButton(1);
        //if (isAiming || tempBool)
        //{
        //    tempBool = true;
        //    isAiming = true;
        //}
        if (isAiming && playerCamera != null && weaponSight != null)
        {
            float angleDifference = Vector3.Angle(
                playerCamera.transform.forward,
                weaponSight.forward
            );

            //Debug.Log("ADS ANGLE DIFFERENCE : " + angleDifference);
            UpdateAimDifference();
            UpdateAimDebug();
        }
        //Debug.Log("is rightAiming : " + isAiming);
        //if (!isAiming) return;
        
        UpdateAds();
       
    }

}
