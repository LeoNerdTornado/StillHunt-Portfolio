using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponADSAim : MonoBehaviour
{
    // Start is called before the first frame update
    

    [Header("References")]
    public Transform weaponAimTarget;

    [Header("Aim Sensitivity")]
    public float horizontalSensitivity = 3f;
    public float verticalSensitivity = 3f;

    [Header("Vertical Limits")]
    public float minPitch = -30f;
    public float maxPitch = 30f;

    private float pitch;
    private float yaw;

    private bool isAiming;

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }


    void Start()
    {
        if(weaponAimTarget != null)
        {
            Vector3 angles = weaponAimTarget.localEulerAngles;

            yaw = angles.y;
            pitch = angles.x;

            if (pitch > 180f) pitch = -360;


        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAiming) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * horizontalSensitivity;
        pitch -= mouseY * verticalSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        weaponAimTarget.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        
    }
}
