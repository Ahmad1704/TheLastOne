using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerDash : MonoBehaviour, IPlayerInputHandler
{
    [Header("Dash Settings")]
    public float dashDistance = 5f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2f;

    private float lastDashTime;
    private bool isDashing;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Coroutine dashCoroutine;

    public void Register(PlayerInputActions inputActions)
    {
        this.inputActions = inputActions;
        inputActions.Gameplay.Dash.performed += OnDash;
        inputActions.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    public void Unregister(PlayerInputActions inputActions)
    {
        inputActions.Gameplay.Dash.performed -= OnDash;
        inputActions.Gameplay.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Move.canceled -= ctx => moveInput = Vector2.zero;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (Time.time - lastDashTime < dashCooldown || isDashing)
            return;

        Vector3 dashDirection = CalculateDashDirection();
        dashCoroutine = StartCoroutine(PerformDash(dashDirection));
        lastDashTime = Time.time;
    }

    private Vector3 CalculateDashDirection()
    {
        if (moveInput.magnitude > 0.1f)
        {
            return (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        }
        return transform.forward;
    }

    private IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true;
        float elapsed = 0f;
        float speed = dashDistance / dashDuration;

        while (elapsed < dashDuration)
        {
            controller.Move(direction * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }
}
