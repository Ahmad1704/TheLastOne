using UnityEngine;

public class PlayerLook : MonoBehaviour, IPlayerInputHandler
{
    [Header("Look Settings")]
    public Transform cameraHolder;
    public float sensitivity = 2f;
    public float pitchClamp = 80f;

    private Vector2 lookInput;
    private float pitch = 0f;

    private void Awake()
    {
        // Auto-assign main camera if not set
        if (cameraHolder == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null) cameraHolder = mainCam.transform;
        }
        
        // Initialize cursor state
        
        // Initialize pitch from current rotation
        if (cameraHolder != null)
            pitch = cameraHolder.localEulerAngles.x;
    }

    public void Register(PlayerInputActions inputActions)
    {
        inputActions.Gameplay.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    public void Unregister(PlayerInputActions inputActions)
    {
        inputActions.Gameplay.Look.performed -= ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Gameplay.Look.canceled -= ctx => lookInput = Vector2.zero;
    }

    private void Update()
    {
        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        // Rotate player horizontally (yaw)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically (pitch)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);
        
        if (cameraHolder != null)
            cameraHolder.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }
}