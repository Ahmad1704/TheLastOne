using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJump : MonoBehaviour, IPlayerInputHandler
{
    public float jumpForce = 8f;
    public float gravity = -20f;

    private float verticalVelocity;
    private CharacterController characterController;

    public float VerticalVelocity => verticalVelocity; // Expose for movement use

    public void Register(PlayerInputActions inputActions)
    {
        inputActions.Gameplay.Jump.performed += OnJump;
    }

    public void Unregister(PlayerInputActions inputActions)
    {
        inputActions.Gameplay.Jump.performed -= OnJump;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f; // Ground stick
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }

    // Call this in PlayerController to apply vertical motion
    public Vector3 GetVerticalMovement()
    {
        return Vector3.up * verticalVelocity;
    }
}
