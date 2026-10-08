using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform firePoints;
    public float fireDistance = 100f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = new Ray(
                firePoints.position,
                firePoints.forward
                );
            RaycastHit hit;
            if (Physics.Raycast(ray,out hit, fireDistance)) {
                DevLog.Log("Player Hit:"+hit.collider.name);
            }
        }
    }
}
