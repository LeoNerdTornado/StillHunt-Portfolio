using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponLeftHandTargetFollower : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform rifleLeftGrip;

    private void LateUpdate()
    {
        if (rifleLeftGrip == null) return;

        transform.position = rifleLeftGrip.position;
        transform.rotation = rifleLeftGrip.rotation;
        
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
