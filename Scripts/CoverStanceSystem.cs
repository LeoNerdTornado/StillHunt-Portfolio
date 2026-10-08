using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverStanceSystem : MonoBehaviour
{
    // Start is called before the first frame update
    public enum CoverState { 
       None,
       Entering,
       Attached,
       Leaving
    }

    

    [Header("Lean")]
    public float leanDistance = 1f;
    public float leanSpeed = 1f;

    [Header("Peek")]
    public float peakHeight = 1.5f;
 
    public CoverState currentState = CoverState.None;

    //private Vector3 neutralLocalPosition;
    private Vector3 bodyNeutralLocalPosition;
    PlayerMovement playerMovement;
    //public bool isCrouching = false;
    Transform coverPoint;
    [SerializeField] private Transform bodyStanceAnchor;




    void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        //neutralLocalPosition = transform.localPosition;
        if (bodyStanceAnchor != null)
        {
            bodyNeutralLocalPosition = bodyStanceAnchor.localPosition;

        }
        //Debug.Log("COVER STANCE | Started | Neutral Position: "+neutralLocalPosition);

    }
    private void UpdateBodyStance(Vector3 targetOffset)
    {
        if (bodyStanceAnchor == null) return;

        Vector3 targetPosition = bodyNeutralLocalPosition + targetOffset;

        float smooth =
            1f - Mathf.Exp(-leanSpeed * Time.deltaTime);
        bodyStanceAnchor.localPosition = Vector3.Lerp(
            bodyStanceAnchor.localPosition,
            targetPosition,
            smooth
            );
    }

    // Update is called once per frame
    void Update()
    {
        if (playerMovement == null) return;
        Vector3 targetOffset = Vector3.zero;
    //    Debug.Log(
    //    "COVER BUTTON | COVER STANCE RUNNING | " +
    //    "isInCover = " + playerMovement.isInCover
    //);

        //Vector3 targetPosition = neutralLocalPosition;
        if (playerMovement.isAttachedToCover) //1ST
        {
            currentState = CoverState.Attached;
        }
        else {
            currentState = CoverState.None;
            
        }
        //Debug.Log("currentCoverState:"+currentState);
            coverPoint = playerMovement.currentCoverPoint;
        //if (Input.GetKeyDown(KeyCode.LeftShift))
        //{
        //    isCrouching = !isCrouching;



        //}


        //if (isCrouching)
        //{

        //    targetPosition += Vector3.down * 0.6f;
        //    Debug.Log("COVER BUTTON IS INCROUCH:" + targetPosition);

        //}


        //if (coverPoint == null) return;
      
        if (playerMovement.isInCover) //2ND
        {
            if (Input.GetKey(KeyCode.A))
            {
                targetOffset += Vector3.left * leanDistance;
                //targetPosition += Vector3.left * leanDistance;
                //Debug.Log("COVERBUTTON | HOLD A | LEAN LEFT");
            }
            else if (Input.GetKey(KeyCode.D))
            {
                targetOffset += Vector3.right * leanDistance;
                //targetPosition += Vector3.right * leanDistance;
                //Debug.Log("COVERBUTTON | HOLD D | LEAN RIGHT");
            }
            else if (Input.GetKey(KeyCode.W))
            {
                targetOffset += Vector3.up * peakHeight;
                //targetPosition += Vector3.up * peakHeight;
                //Debug.Log("COVERBUTTON | Peak Up | HOLD W");
            }

            

        }
        UpdateBodyStance(targetOffset);
        //Vector3 targetPosition = neutralLocalPosition + targetOffset;

        //transform.localPosition = Vector3.Lerp( //3RD
        //       transform.localPosition,
        //       targetPosition,
        //       Time.deltaTime * leanSpeed
        //       );

        //if(bodyStanceAnchor!= null)
        //{
        //    Vector3 bodyTargetPosition = bodyNeutralLocalPosition + targetOffset;
        //    bodyStanceAnchor.localPosition = Vector3.Lerp(
        //        bodyStanceAnchor.localPosition,
        //        bodyTargetPosition,
        //        Time.deltaTime * leanSpeed
        //        );
        //}
    }
    //private void OnDrawGizmos()
    //{
    //    if (playerMovement == null) {
    //        return;
    //    }
    //    if (playerMovement.activeCoverPoint == null) {
    //        return;
    //    }
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawSphere(
    //        playerMovement.activeCoverPoint.position,
    //        0.15f
    //        );
    //}

}
