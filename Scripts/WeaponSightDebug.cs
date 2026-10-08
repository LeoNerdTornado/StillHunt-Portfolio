using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSightDebug : MonoBehaviour
{
    // Start is called before the first frame update
    public float debugLength = 100f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine( transform.position,(transform.position + transform.forward * debugLength));


    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
