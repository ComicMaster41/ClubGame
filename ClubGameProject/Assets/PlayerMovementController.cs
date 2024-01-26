using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
{
    //Declare reference variables
    PlayerMovement playerInput;
    CharacterController characterController;
    Animator animator;

    int isWalkingHash;
    int isRunningHash;
    int isFallingHash;
    int isJumpingHash;

    //Variables to store player input values
    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 currentRunMovement;
    bool isMovementPressed;
    bool isRunPressed;
    [SerializeField] float runMultiplier = 3.0f;

    float rotationFactorPerFrame = 8.0f;

    //Gravity Variables
    float gravity = -9.8f;
    float groundGravity = -0.05f;

    //Jumping Variables
    bool isJumpPressed = false;
    float initialJumpVelocity;
    [SerializeField] float maxJumpHeight = 1.0f;
    [SerializeField] float maxJumpTime = 0.5f;
    bool isJumping = false;

    void Awake()
    {
        //initially set reference variables
        playerInput = new PlayerMovement();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isFallingHash = Animator.StringToHash("isFalling");
        isJumpingHash = Animator.StringToHash("isJumping");

        //Player input callbacks
        playerInput.CharacterControls.Move.started += onMovementInput;
        playerInput.CharacterControls.Move.canceled += onMovementInput;
        playerInput.CharacterControls.Move.performed += onMovementInput;
        playerInput.CharacterControls.Run.started += onRun;
        playerInput.CharacterControls.Run.canceled += onRun;
        playerInput.CharacterControls.Jump.started += onJump;
        playerInput.CharacterControls.Jump.canceled += onJump;

        setupJumpVariables();

    }

    void handleRotation()
    {
        Vector3 positionToLookAt;

        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = currentMovement.z;
        Quaternion currentRotation = transform.rotation;
        if(isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }    
    }

    //Sets up variables that will be used for jumping
    void setupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }

    //Checks if the jump button was pressed
    void onJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
    }

    //Makes the character jump
    void handleJump()
    {
        if(!isJumping && characterController.isGrounded && isJumpPressed)
        {
            animator.SetBool(isJumpingHash, true);
            animator.SetBool(isFallingHash, false);
            isJumping = true;
            currentMovement.y = initialJumpVelocity * .5f;
            currentRunMovement.y = initialJumpVelocity * .5f;
        }
        else if(!isJumpPressed && isJumping && characterController.isGrounded) 
        { 
            isJumping = false; 
        }
    }

    //Checks if run button is pressed
    void onRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    //Sets speeds of character movement
    void onMovementInput (InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;
        currentRunMovement.x = currentMovementInput.x * runMultiplier;
        currentRunMovement.z = currentMovementInput.y * runMultiplier;
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
    }

    //Handles the gravity and the falling
    void handleGravity()
    {
        bool isFalling = currentMovement.y <= 0.0f || !isJumpPressed;
        float fallMultiplier = 2.0f;


        if(characterController.isGrounded)
        {
            animator.SetBool(isJumpingHash, false);
            animator.SetBool(isFallingHash, false);
            currentMovement.y = groundGravity;
            currentRunMovement.y = groundGravity;
        }
        else if(isFalling)
        {
            animator.SetBool(isFallingHash, true);
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (gravity * fallMultiplier * Time.deltaTime);
            float nextYVelocity =Mathf.Max((previousYVelocity + newYVelocity) * .5f, -20.0f);
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
        else
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (gravity * Time.deltaTime);
            float nextYVelocity = (previousYVelocity + newYVelocity) * .5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
    }

    void handleAnimation()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);
        bool isFalling = animator.GetBool(isFallingHash);
        bool isJumping = animator.GetBool(isJumpingHash);

        if((isMovementPressed && !isWalking) && !isRunPressed)
        {
            animator.SetBool(isWalkingHash, true);
        }
        else if(!isMovementPressed && isWalking)
        {
            animator.SetBool(isWalkingHash, false);
        }

        if ((isMovementPressed && isRunPressed) && !isRunning)
        {
            animator.SetBool(isRunningHash, true);
        }
        else if((!isMovementPressed || !isRunPressed) && isRunning)
        {
            animator.SetBool(isRunningHash, false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        handleRotation();
        handleAnimation();
        if (isRunPressed)
        {
            characterController.Move(currentRunMovement * Time.deltaTime);
        }
        else
        {
            characterController.Move(currentMovement * Time.deltaTime);
        }
        handleGravity();
        handleJump();

    }

    void OnEnable()
    {
        //enables character control action map
        playerInput.CharacterControls.Enable();
    }

    void OnDisable() 
    {
        //disables character control action map
        playerInput.CharacterControls.Disable();
    }
}
