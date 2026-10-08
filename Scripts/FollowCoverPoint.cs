using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCoverPoint : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Transform followCoverPoint;
    [Header("Position Offset")]
    public Vector3 positionOffset = Vector3.zero;
    void Start()
    {
        
    }
    private void LateUpdate()
    {
        if (followCoverPoint == null)
        {
            return;
        }
        transform.position = followCoverPoint.position + positionOffset;
        //transform.forward = guardHead.forward;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
