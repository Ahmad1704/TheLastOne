using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    private CharacterController characterController;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private PlayerJump playerJump;  

    private List<IPlayerInputHandler> inputHandlers = new();

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
        playerJump = GetComponent<PlayerJump>();  
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;

        // Register modular inputs
        GetComponents<IPlayerInputHandler>().ToList().ForEach(handler =>
        {
            handler.Register(inputActions);
            inputHandlers.Add(handler);
        });
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled -= ctx => moveInput = Vector2.zero;
        inputActions.Gameplay.Disable();

        foreach (var handler in inputHandlers)
        {
            handler.Unregister(inputActions);
        }
        inputHandlers.Clear();
    }

    private void Update()
    {
        // Get movement direction relative to player's facing
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y);
        move *= moveSpeed;
        
        // Apply vertical velocity from jump/gravity
        if (playerJump != null)
        {
            move.y = playerJump.VerticalVelocity;
        }
        
        // Apply final movement
        characterController.Move(move * Time.deltaTime);
    }
}