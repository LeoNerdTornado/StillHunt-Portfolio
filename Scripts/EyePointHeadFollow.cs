using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyePointHeadFollow : MonoBehaviour
{

    [Header("Position Offset")]
    public Vector3 positionOffset = Vector3.zero;
    // Start is called before the first frame update
    [SerializeField] private Transform guardHead;
    [SerializeField] private Transform rifleRearAim;
    [SerializeField] private Transform rifleFrontAim;
    void Start()
    {
        
    }

    private void LateUpdate()
    {
        if (guardHead == null)
        {
            return;
        }
        transform.position = guardHead.position + positionOffset;
        Vector3 rifleDirection = rifleFrontAim.position - rifleRearAim.position;
        rifleDirection.y = 0f;

        if (rifleDirection.sqrMagnitude > 0.001f)
        {
            float yaw =
            Mathf.Atan2
            (
                rifleDirection.x,
                rifleDirection.z
                ) * Mathf.Rad2Deg;
            Vector3 currentRotation = transform.eulerAngles;

            transform.eulerAngles =
                new Vector3(
                    currentRotation.x,
                    yaw,
                    currentRotation.z
                    );
            //transform.rotation =
            //    Quaternion.LookRotation(
            //        rifleDirection.normalized,
            //        Vector3.up
            //        );
        }
        //transform.forward = guardHead.forward;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
