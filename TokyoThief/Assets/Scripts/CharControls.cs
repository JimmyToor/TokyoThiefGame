﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharControls : MonoBehaviour
{

    [SerializeField]
    float moveSpeed = 4f;
    float runSpeed = 8f;
    float crouchSpeed = 3f;

    float horzMovement = 0;
    float vertMovement = 0;

    public float gravity = -20.0f;
    public float jumpHeight = 3f;

    float groundSlopeLimit = 45f;
    float jumpSlopeLimit = 90f;

    public LayerMask groundMask;

    bool isCrouched = false;
    bool noStand = false;
    bool isGrounded = true;

    Vector3 velocity;
    Vector3 forward, right;
    Vector3 currDir;

    CharacterController controller;

    public Animator animator;

    SpriteRenderer hiroRenderer;

    Interactable currentInteractable;

    AudioSource footsteps;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        Reorient();
        hiroRenderer = controller.GetComponentInChildren<SpriteRenderer>();
        footsteps = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // How much to offset my raycasts from center of controller
        Vector3 capsuleOffset = new Vector3(controller.radius, 0, 0);

        Ray centerRayUp = new Ray(transform.position, Vector3.up); // Ray up the center of controller
        Ray frontRayUp = new Ray(transform.position + capsuleOffset, Vector3.up); // Ray up the outside front of controller
        Ray backRayUp = new Ray(transform.position - capsuleOffset, Vector3.up); // Ray up the outside back of controller
        Ray centerRay = new Ray(transform.position, -Vector3.up); // Ray down the center of controller
        Ray frontRay = new Ray(transform.position + capsuleOffset, -Vector3.up); // Ray down the outside front of controller
        Ray backRay = new Ray(transform.position - capsuleOffset, -Vector3.up); // Ray down the outside back of controller

        float rayLength = (controller.radius); // Ray starts at middle of controller, so half the height + some extra for the skin, etc

        // Check center, front, and back to see if they are all grounded before applying gravity
        if (!Physics.Raycast(centerRay, rayLength, groundMask))
        {
            if (!Physics.Raycast(frontRay, rayLength, groundMask))
            {
                if (!Physics.Raycast(backRay, rayLength, groundMask))
                {
                    isGrounded = false;
                }
                else
                {
                    isGrounded = true;
                }
            }
            else
            {
                isGrounded = true;
            }
        }
        else
        {
            isGrounded = true;
        }

        // Check to make sure there's room to stand
        if (!Physics.Raycast(centerRayUp, rayLength + 3f, groundMask))
        {
            if (!Physics.Raycast(frontRayUp, rayLength + 3f, groundMask))
            {
                if (!Physics.Raycast(backRayUp, rayLength + 3f, groundMask))
                {
                    noStand = false;
                }
                else
                {
                    noStand = true;
                }
            }
            else
            {
                noStand = true;
            }
        }
        else
        {
            noStand = true;
        }



        // Debug draw grounding raycasts
        /*
        Debug.DrawRay(transform.position, -transform.up * rayLength, Color.white);
        Debug.DrawRay(transform.position + capsuleOffset, -transform.up * rayLength, Color.white);
        Debug.DrawRay(transform.position - capsuleOffset, -transform.up * rayLength, Color.white);
        */

        // reset velocity if we hit the ground or ceiling
        if ((isGrounded && velocity.y <= 0) || ((controller.collisionFlags & CollisionFlags.Above) != 0))
        {
            controller.slopeLimit = groundSlopeLimit;
            velocity.y = 0f;
        }
        else if (!isGrounded)
        {
            // in the air, so add downward y velocity
            velocity.y += gravity * Time.deltaTime;
        }

        // Only allow movement if game is not over
        if(!(FindObjectOfType<GameManager>().gameHasEnded))
        {
            // apply gravity (if there is any)
            controller.Move(velocity * Time.deltaTime);
            //Vector3 direction = new Vector3(Input.GetAxis("HorizontalKey"), 0, Input.GetAxis("VerticalKey"));
            horzMovement = Input.GetAxis("HorizontalKey");
            vertMovement = Input.GetAxis("VerticalKey");
            animator.SetFloat("Magnitude", Mathf.Abs(horzMovement) + Mathf.Abs(vertMovement));
            if (((Mathf.Abs(horzMovement) + Mathf.Abs(vertMovement)) > 0) && !footsteps.isPlaying && !isCrouched && isGrounded)
            {
                footsteps.Play();
            }
            else if (((Mathf.Abs(horzMovement) + Mathf.Abs(vertMovement)) == 0) || isCrouched || !isGrounded || Time.timeScale < 1f)
            {
                footsteps.Stop();
            }
            if (Input.anyKey)
            {
                if (Input.GetKeyDown(KeyCode.F) && currentInteractable != null)
                {
                    currentInteractable.Interact();
                }
                Move();
            }
        }

        	
    }
	
	void Move()
	{
        Vector3 rightMovement = right * moveSpeed * Time.deltaTime * Input.GetAxis("HorizontalKey");
		Vector3 upMovement = forward * moveSpeed * Time.deltaTime * Input.GetAxis("VerticalKey");

        // maintain sprite direction if we didn't move
        if (!(rightMovement == Vector3.zero && upMovement == Vector3.zero))
        {
            animator.SetFloat("Horizontal", horzMovement);
            animator.SetFloat("Vertical", vertMovement);
        }
  
        Vector3 heading = Vector3.Normalize(rightMovement + upMovement);
        if (heading != Vector3.zero)
        {
            transform.forward = heading;
        }

        Reorient();

        //crouch
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded == true)
        {
            if (isCrouched == false)
            {
                controller.height = controller.height / 2;
                isCrouched = true;
                moveSpeed = crouchSpeed;
                controller.center = new Vector3(0, 0.75f, 0);
                animator.SetBool("Crawling", true);
            }
            else
            {
                if (!noStand)
                {
                    controller.center = new Vector3(0, 2.4f, 0);
                    controller.height = controller.height * 2;
                    isCrouched = false;
                    moveSpeed = runSpeed;
                    animator.SetBool("Crawling", false);
                }
            }
        }

        //jump
        if (Input.GetButtonDown("Jump") && isGrounded && isCrouched == false)
        {
            controller.slopeLimit = jumpSlopeLimit;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // horizontal movement
        heading *= moveSpeed;
        controller.Move(heading * Time.deltaTime);
        
	}

    void Reorient()
    {
        // reorient controls if camera rotated
        if (currDir != Camera.main.transform.forward)
        {
            forward = Camera.main.transform.forward;
            currDir = Camera.main.transform.forward;
            forward.y = 0;
            forward = Vector3.Normalize(forward);
            right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;
        }
    }

    public void setInteractable(Interactable interactable)
    {
        if (interactable == null)
        {
            currentInteractable = null;
        }
        else
        {
            currentInteractable = interactable;
        }
        
    }
}
