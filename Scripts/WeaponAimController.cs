using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAimController : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("Aim References")]
    public Transform weaponAimTarget;
    public Transform weaponSight;
    public Transform rightHandTarget;

    [Header("Aim Settings")]
    public float rotationSpeed = 10f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if ((weaponAimTarget == null) || (weaponSight == null)) return;

        Vector3 direction = weaponAimTarget.position - weaponSight.position;
        if (direction.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
            );

    }
}
