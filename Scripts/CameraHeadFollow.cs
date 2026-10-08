using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraHeadFollow : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform head;

    [SerializeField] private Transform headAnchor;

    [Header("Camera Height")]
    [SerializeField] private float standingHeight = 1.6f;
    [SerializeField] private float crouchHeight = 1f;

    [Header("Crouch Detection")]
    [SerializeField] private float crouchThreshhold = 1.3f;

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 20f;

    [Header("Position Offset")]
    public Vector3 positionOffset = Vector3.zero;

    private void LateUpdate()
    {

        if ( (head == null)) return;

        ApplyPositionOffset();

        //Vector3 position = transform.position;

        ////KEEP CAMERA X AND Z COMPLETELY INDEPENDENT

        //position.x = player.position.x;
        //position.z = player.position.z;

        ////Only use the head to determine whether we are standing or crouching
        //float headHeight = head.position.y - player.position.y;
        //if(headHeight < crouchHeight)
        //{
        //    position.y = player.position.y + crouchHeight;

        //}
        //else
        //{
        //    position.y = player.position.y + standingHeight;

        //}
        //transform.position = position;


        //Vector3 targetPosition = head.position;

        //transform.position = Vector3.Lerp(
        //    transform.position,
        //    targetPosition,
        //    followSpeed * Time.deltaTime
        //    );

    }
    private void OnValidate()
    {
        if(head == null) { return; }

        transform.position = head.position + positionOffset;
    }
    [ContextMenu("Apply Position Offset")]
    private void ApplyPositionOffset()
    {
        if (head == null) return;
        transform.position = head.position + positionOffset;
    }
    public void SetHead(Transform newHead)
    {
        head = newHead;
    }
    void Start()
    {
        //SetHead(head);
    }

    // Update is called once per frame
    void Update()
    {
        //LateUpdate();
    }
}
