using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightHandAimFollower : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform weaponAimTarget;
    public Transform rightHandTarget;

    public float rotationSpeed = 5f;

    public WeaponSystem weaponSystem;



    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if ((weaponAimTarget == null) || 
            (rightHandTarget == null) ||
            (weaponSystem == null)) return;

        if (!weaponSystem.isAiming) return;

        Vector3 direction = weaponAimTarget.position - rightHandTarget.position;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        rightHandTarget.rotation =
            Quaternion.Slerp(
                rightHandTarget.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
                );
      
    }
}
