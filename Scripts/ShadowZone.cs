using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowZone : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            GuardAI guardAI = other.GetComponent<GuardAI>();
            if (guardAI != null) {
                guardAI.isInShadow = true;
                DevLog.Log("Guard Entered Shadow.");
            }
            
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy")) {

            GuardAI guardAI = other.GetComponent<GuardAI>();
            if (guardAI != null)
            {
                guardAI.isInShadow = false;
                DevLog.Log("Guard Entered left shadow.");
            }
            
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
