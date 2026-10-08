using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAimTargetFollower : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Transform weaponAimTarget;

    [Header("Aim Rotation Offset")]
    [SerializeField] private Vector3 rotationOffset;

    [Header("Local Position")]
    [SerializeField] private float normalLocalY = 0f;
    [SerializeField] private float aimLocalY = 1f;

    
    

    [Header("Position Offset")]
   
    [SerializeField] private float forwardOffset = 0.5f;

    [Header("Ads Transition")]
    [SerializeField] private float transitionSpeed = 10f;

    private bool isAiming;

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }
    private void LateUpdate()
    {
        if (weaponAimTarget == null) return;

        if (transform.parent == null) return;

        

        //Debug.Log("is rightAiming rightHand : "+ targetOffset+" aiming? "+ isAiming);
        //Vector3 desiredPosition =
        //    weaponAimTarget.position
        //    + weaponAimTarget.right * targetOffset.x
        //    + weaponAimTarget.up * targetOffset.y
        //    + weaponAimTarget.forward * targetOffset.z;

        // ------------------------------------
        // 1. Get the desired WORLD position
        //    from WeaponAimTarget
        // ------------------------------------
        Vector3 desiredWorldLocation = weaponAimTarget.position + weaponAimTarget.forward * forwardOffset;


        // ------------------------------------
        // 2. Convert WORLD position into
        //    RightHandTarget's PARENT space
        // ------------------------------------
        Vector3 desiredLocalPosition = transform.parent.InverseTransformPoint(desiredWorldLocation);


        // ------------------------------------
        // 3. Force ONLY the local Y
        // ------------------------------------
        
        float targetLocalY = isAiming ? aimLocalY : normalLocalY;

        // ------------------------------------
        // 4. Move RightHandTarget smoothly
        // ------------------------------------

        desiredLocalPosition.y = targetLocalY;

        // ------------------------------------
        // 5. Rotation still follows
        //    WeaponAimTarget
        // ------------------------------------

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            desiredLocalPosition,
            Time.deltaTime * transitionSpeed
            );
        transform.rotation = weaponAimTarget.rotation * Quaternion.Euler(rotationOffset);
        //transform.rotation = weaponAimTarget.rotation;
    //    Debug.Log(
    //        "is rightAiming rightHand : " +
    //    "Aiming: " + isAiming +
    //    " | Offset: " +
    //    " | Desired World Position: " + desiredWorldLocation
    //);
        //transform.position = Vector3.Lerp(
        //    transform.position,
        //    desiredPosition,
        //    Time.deltaTime * transitionSpeed
        //    );
        //transform.rotation = weaponAimTarget.rotation * Quaternion.Euler(rotationOffset);

        //transform.position = weaponAimTarget.position + weaponAimTarget.forward * 0.5f; 
        //transform.rotation = weaponAimTarget.rotation * Quaternion.Euler(rotationOffset);

    }
    void Start()
    {
        
    }
    public void SetAimSettings(Vector3 rotationOffset, float localY, float forwardOffset)
    {
        this.rotationOffset = rotationOffset;
        this.aimLocalY = localY;
        this.forwardOffset = forwardOffset;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
