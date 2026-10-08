using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Start is called before the first frame update
    public float moveSpeed = 5f;
    private CharacterController controller;
    public bool isAlive;
    public bool isHidden;
    public bool isInCover;
    public float verticalVelocity = 0f;
    public float gravity = -12f;
    public Cover currentCover;
    public int health = 100;

    public GuardAI guardAI;
    public GuardMovement guardMovement;
    public GuardVision guardVision;
    public Transform guardTarget;
    

    public void TakeDamage(int damage)
    {
        health -= damage;
        //Debug.Log("fCertainty Player Hit! Health:"+health);
        if (health <= 0)
        {
            isAlive = false;
            //Debug.Log("fCertainty Player Died!");

        }
        
    }
    //public Weapon currentWeapon;
    
    void Start()
    {
        isAlive = true;
        controller = GetComponent<CharacterController>();
        guardAI = guardTarget.GetComponent<GuardAI>();
        guardMovement = guardTarget.GetComponent<GuardMovement>();
        guardVision = guardTarget.GetComponent<GuardVision>();
        //Debug.Log("Player Spawned");
    }
    void DebugPlayerGeometry()
    {
        Bounds controllerBounds = controller.bounds;

        Renderer bodyRenderer = GetComponentInChildren<Renderer>();

        //Debug.Log(
        //    "GEOMETRY DEBUG \n" +
        //    "Player Y: " + transform.position.y + "\n" +
        //    "Controller Bottom Y: " + controllerBounds.min.y + "\n" +
        //    "Controller Top Y: " + controllerBounds.max.y + "\n" +
        //    "Controller Center Y: " + controllerBounds.center.y + "\n" +
        //    "Body Bottom Y: " +
        //    (bodyRenderer != null ? bodyRenderer.bounds.min.y.ToString() : "NO RENDERER") + "\n" +
        //    "Body Top Y: " +
        //    (bodyRenderer != null ? bodyRenderer.bounds.max.y.ToString() : "NO RENDERER")
        //);
    }
    public void RespawnAt(Vector3 spawnPosition)
    {
        controller.enabled = false;
        transform.position = spawnPosition;
        verticalVelocity = 0f;
        isAlive = true;
        isHidden = false;
        isHidden = false;
        isInCover = false;
        currentCover = null;
        controller.enabled = true;

  //      Debug.Log(
  //    "PLAYER RESPAWNED | Position: " +
  //    transform.position +
  //    " | VerticalVelocity: " +
  //    verticalVelocity
  //);

    }
    void DebugBodyTransform()
    {
        Transform body = transform.Find("Body");

        if (body == null)
        {
            //Debug.Log("BODY DEBUG | Body not found");
            return;
        }

        Renderer renderer = body.GetComponentInChildren<Renderer>();

        //Debug.Log(
        //    "BODY DEBUG \n" +
        //    "Local Position: " + body.localPosition + "\n" +
        //    "World Position: " + body.position + "\n" +
        //    "Local Scale: " + body.localScale + "\n" +
        //    "World Scale: " + body.lossyScale + "\n" +
        //    "Renderer Bottom Y: " +
        //    (renderer != null ? renderer.bounds.min.y.ToString() : "NO RENDERER") + "\n" +
        //    "Renderer Top Y: " +
        //    (renderer != null ? renderer.bounds.max.y.ToString() : "NO RENDERER")
        //);
    }
    void DebugBodyHierarchy()
    {
        Transform current = transform.Find("Body");

        if (current == null)
        {
            //Debug.Log("BODY HIERARCHY | Body not found");
            return;
        }

        while (current != null)
        {
            //Debug.Log(
            //    "BODY HIERARCHY | " +
            //    "Name: " + current.name +
            //    " | LocalPos: " + current.localPosition +
            //    " | WorldPos: " + current.position +
            //    " | LocalRot: " + current.localEulerAngles +
            //    " | WorldRot: " + current.eulerAngles +
            //    " | LocalScale: " + current.localScale +
            //    " | WorldScale: " + current.lossyScale
            //);

            current = current.parent;
        }
    }
    // Update is called once per frame
    void Update()
    {
        DebugPlayerGeometry();
        DebugBodyTransform();
        DebugBodyHierarchy();
        //bool grounded = controller.isGrounded;

        //    if (grounded)
        //    {
        //        verticalVelocity = 0f;
        //    }
        //    else
        //    {
        //        verticalVelocity += gravity * Time.deltaTime;
        //        verticalVelocity = Mathf.Max(verticalVelocity, -20f);
        //    }

        //    Vector3 movement = Vector3.zero;

        //    movement.y = verticalVelocity;

        //    controller.Move(movement * Time.deltaTime);

        //      Debug.Log(
        //"PLAYER TEST | " +
        //"Y: " + transform.position.y +
        //" | GroundedBeforeMove: " + grounded +
        //" | GroundedAfterMove: " + controller.isGrounded +
        //" | VerticalVelocity: " + verticalVelocity
        //" | VerticalVelocity: " + verticalVelocity
        //);
        //============================
        //float horizontal = Input.GetAxis("Horizontal");
        //float vertical = Input.GetAxis("Vertical");
        //if (controller.isGrounded)
        //{
        //    verticalVelocity = -1f;
        //}
        //else
        //{
        //    verticalVelocity += gravity * Time.deltaTime;
        //}
        //Vector3 movement = transform.right * horizontal + transform.forward * vertical;
        //movement = movement * moveSpeed;
        //movement.y = verticalVelocity;
        //controller.Move(movement * Time.deltaTime);
    }
}
