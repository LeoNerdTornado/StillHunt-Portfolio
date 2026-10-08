using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickToMove : MonoBehaviour
{
    // Start is called before the first frame update
    PlayerMovement playerMovement;
    CharacterController controller;

    Vector3 targetPosition;
    bool hasTarget = false;
    public float moveSpeed = 3f;
    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {

        if (playerMovement.isAttachedToCover) return;

        //if (Input.GetMouseButtonDown(0)) {
        //    /*Debug.Log("Mouse Clicked To Floor!");*/
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        //    RaycastHit hit;
        //    if (Physics.Raycast(ray,out hit))
        //    {
        //        //Debug.Log(hit.collider.name);
        //        //Debug.Log(hit.point);
        //        if (hit.collider.CompareTag("Ground")) {
        //            playerMovement.SetDestination(hit.point);
        //        }
        //    }
        //}
        //if (hasTarget)
        //{
        //    Vector3 direction = targetPosition - transform.position;

        //    direction.y = 0f;

        //    if (direction.magnitude < 0.1f)
        //    {
        //        hasTarget = false;

        //        return;
        //    }
        //    if (Vector3.Distance(transform.position, targetPosition) < 2.0f)
        //    {
        //        hasTarget = false;
        //        return;

        //    }


        //    direction.Normalize();
        //    controller.Move(direction * moveSpeed * Time.deltaTime);
        //    Debug.Log("YEAH:"+Vector3.Distance(transform.position, targetPosition));
        //    //transform.forward = direction;
            


        //}
    }
}
