using UnityEngine;

public class CameraManager : MonoBehaviour
{
    #region Fields & Settings

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float moveSpeed = 5f;
    public float fastMoveSpeed = 10f;
    public float mouseSensitivity = 2f;
    public bool invertMouseY = false;

    [Header("Player Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    public Vector3 defaultSpawnPosition = Vector3.zero;
    public string cameraHolderName = "CameraHolder";
    public bool useParenting = true;
    public Vector3 firstPersonOffset = new Vector3(0, 1.6f, 0);
    public float firstPersonSmoothSpeed = 5f;

    [Header("Controls")]
    public KeyCode toggleModeKey = KeyCode.F;
    public KeyCode fastMoveKey = KeyCode.LeftShift;

    private bool isFirstPerson = false;
    private GameObject currentPlayer;
    private PlayerController playerController;
    private Transform cameraHolder;
    private Transform originalCameraParent;
    private BoxCollider movementBounds;

    // Free camera state
    private Vector3 freeCameraPosition;
    private Quaternion freeCameraRotation;
    private float yaw;
    private float pitch;
    private bool cursorLocked = true;

    // First person state
    private Vector3 firstPersonVelocity;

    #endregion

    #region Unity Events

    private void Awake()
    {
        movementBounds = GetComponent<BoxCollider>();
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        originalCameraParent = mainCamera.transform.parent;

        InitializeFreeCamera();
    }

    void Update()
    {
        HandleInput();

        if (isFirstPerson && currentPlayer != null)
        {
            UpdateFirstPersonCamera();
        }
        else
        {
            UpdateFreeCamera();
        }
    }

    void OnGUI()
    {
        if (isFirstPerson) return;
        DrawDebugGUI();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && cursorLocked)
        {
            LockCursor(true);
        }
    }

    #endregion

    #region Camera Modes

    void InitializeFreeCamera()
    {
        freeCameraPosition = mainCamera.transform.position;
        freeCameraRotation = mainCamera.transform.rotation;

        Vector3 eulerAngles = mainCamera.transform.eulerAngles;
        yaw = eulerAngles.y;
        pitch = NormalizePitch(eulerAngles.x);

        isFirstPerson = false;

        Debug.Log("Free Camera Mode: Initialized");
    }

    void ToggleMode()
    {
        isFirstPerson = !isFirstPerson;

        if (isFirstPerson)
        {
            SpawnPlayer();
            LockCursor(true);
            Debug.Log("FIRST PERSON MODE: Player spawned and camera attached");
        }
        else
        {
            DestroyPlayer();
            DetachCamera();
            mainCamera.transform.position = freeCameraPosition;
            mainCamera.transform.rotation = freeCameraRotation;

            Vector3 eulerAngles = freeCameraRotation.eulerAngles;
            yaw = eulerAngles.y;
            pitch = NormalizePitch(eulerAngles.x);

            Debug.Log("FREE CAMERA MODE: Player destroyed, free camera restored");
        }
    }

    #endregion

    #region Input Handling

    void HandleInput()
    {
        if (Input.GetKeyDown(toggleModeKey))
        {
            ToggleMode();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(!cursorLocked);
        }

        if (cursorLocked && !isFirstPerson)
        {
            HandleMouseLook();
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (invertMouseY) mouseY = -mouseY;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
    }

    #endregion

    #region Free Camera Logic

    void UpdateFreeCamera()
    {
        if (currentPlayer != null)
        {
            DestroyPlayer();
        }

        if (mainCamera.transform.parent != originalCameraParent)
        {
            DetachCamera();
        }

        if (cursorLocked)
        {
            mainCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }

        Vector3 moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveInput += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) moveInput += Vector3.back;
        if (Input.GetKey(KeyCode.A)) moveInput += Vector3.left;
        if (Input.GetKey(KeyCode.D)) moveInput += Vector3.right;
        if (Input.GetKey(KeyCode.Q)) moveInput += Vector3.down;
        if (Input.GetKey(KeyCode.E)) moveInput += Vector3.up;

        float currentMoveSpeed = Input.GetKey(fastMoveKey) ? fastMoveSpeed : moveSpeed;
        Vector3 movement = mainCamera.transform.TransformDirection(moveInput) * currentMoveSpeed * Time.deltaTime;
        Vector3 newPosition = mainCamera.transform.position + movement;

        if (movementBounds != null)
        {
            Bounds bounds = movementBounds.bounds;

            newPosition.x = Mathf.Clamp(newPosition.x, bounds.min.x, bounds.max.x);
            newPosition.y = Mathf.Clamp(newPosition.y, bounds.min.y, bounds.max.y);
            newPosition.z = Mathf.Clamp(newPosition.z, bounds.min.z, bounds.max.z);
        }

        mainCamera.transform.position = newPosition;

        freeCameraPosition = mainCamera.transform.position;
        freeCameraRotation = mainCamera.transform.rotation;
    }

    #endregion

    #region First Person Logic

    void UpdateFirstPersonCamera()
    {
        if (currentPlayer == null || cameraHolder == null)
        {
            Debug.LogWarning("Player or CameraHolder not found! Switching back to free camera.");
            ToggleMode();
            return;
        }

        if (!useParenting)
        {
            Vector3 targetPosition = cameraHolder.position;

            mainCamera.transform.position = Vector3.SmoothDamp(
                mainCamera.transform.position,
                targetPosition,
                ref firstPersonVelocity,
                firstPersonSmoothSpeed * Time.deltaTime
            );
        }
    }

    void SpawnPlayer()
    {
        if (currentPlayer != null)
        {
            DestroyPlayer();
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab not assigned! Cannot spawn player.");
            isFirstPerson = false;
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();

        currentPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        playerController = currentPlayer.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        FindCameraHolder();

        if (cameraHolder != null)
        {
            AttachCamera();
        }
        else
        {
            Debug.LogWarning($"CameraHolder '{cameraHolderName}' not found in player! Using fallback positioning.");
            mainCamera.transform.position = currentPlayer.transform.position +
                                            currentPlayer.transform.TransformDirection(firstPersonOffset);
        }

        yaw = currentPlayer.transform.eulerAngles.y;

        Debug.Log($"Player spawned at: {spawnPosition}");
        Debug.Log($"Camera attached to: {(cameraHolder != null ? cameraHolder.name : "Manual positioning")}");
    }

    void DestroyPlayer()
    {
        if (currentPlayer != null)
        {
            DetachCamera();

            Destroy(currentPlayer);
            currentPlayer = null;
            playerController = null;
            cameraHolder = null;

            Debug.Log("Player destroyed and camera detached");
        }
    }

    #endregion

    #region Camera Parenting & Helpers

    void FindCameraHolder()
    {
        if (currentPlayer == null) return;

        cameraHolder = FindChildByName(currentPlayer.transform, cameraHolderName);

        if (cameraHolder == null)
        {
            Debug.LogWarning($"CameraHolder with name '{cameraHolderName}' not found in player hierarchy!");

            string[] commonNames = { "Head", "Camera", "CameraPosition", "Eyes", "FirstPersonCamera" };
            foreach (string name in commonNames)
            {
                cameraHolder = FindChildByName(currentPlayer.transform, name);
                if (cameraHolder != null)
                {
                    Debug.Log($"Found alternative CameraHolder: {name}");
                    break;
                }
            }
        }
    }

    Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildByName(parent.GetChild(i), name);
            if (result != null)
                return result;
        }
        return null;
    }

    void AttachCamera()
    {
        if (cameraHolder != null && useParenting)
        {
            mainCamera.transform.SetParent(cameraHolder);
            mainCamera.transform.localPosition = firstPersonOffset;
            mainCamera.transform.localRotation = Quaternion.identity;

            Debug.Log($"Camera attached to {cameraHolder.name}");
        }
    }

    void DetachCamera()
    {
        if (mainCamera.transform.parent != originalCameraParent)
        {
            mainCamera.transform.SetParent(originalCameraParent);
            Debug.Log("Camera detached from player");
        }
    }

    #endregion

    #region Utility & External Methods

    Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform closestSpawn = null;
            float closestDistance = float.MaxValue;

            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    float distance = Vector3.Distance(mainCamera.transform.position, spawnPoint.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSpawn = spawnPoint;
                    }
                }
            }

            if (closestSpawn != null)
            {
                return closestSpawn.position;
            }
        }

        return mainCamera.transform.position.y > 1f ?
            new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, mainCamera.transform.position.z) :
            defaultSpawnPosition;
    }

    float NormalizePitch(float angle)
    {
        angle = (angle + 180) % 360 - 180;
        return Mathf.Clamp(angle, -90, 90);
    }

    void LockCursor(bool lockCursor)
    {
        cursorLocked = lockCursor;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    public void ForceSpawnPlayer(Vector3 position)
    {
        if (!isFirstPerson)
        {
            ToggleMode();
        }

        if (currentPlayer != null)
        {
            currentPlayer.transform.position = position;
        }
    }

    public void SetFreeCamera(Vector3 position, Quaternion rotation)
    {
        if (isFirstPerson)
        {
            ToggleMode();
        }

        mainCamera.transform.position = position;
        mainCamera.transform.rotation = rotation;
        freeCameraPosition = position;
        freeCameraRotation = rotation;
    }

    void DrawDebugGUI()
    {
      

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        string mode = isFirstPerson ? "First Person (FPS)" : "Free Camera (Spectator)";
        GUI.Label(new Rect(10, 10, 300, 30), $"Mode: {mode}", style);
        GUI.Label(new Rect(10, 30, 300, 30), $"Press '{toggleModeKey}' to toggle mode between first person and free Camera", style);

        if (isFirstPerson)
        {
            GUI.Label(new Rect(10, 50, 300, 30), "Input handled by PlayerController", style);

            style.normal.textColor = Color.green;
            GUI.Label(new Rect(10, 70, 300, 30), $"Player: SPAWNED", style);

            if (cameraHolder != null)
            {
                GUI.Label(new Rect(10, 90, 300, 30), $"Camera: ATTACHED to {cameraHolder.name}", style);
            }
            else
            {
                style.normal.textColor = Color.yellow;
                GUI.Label(new Rect(10, 90, 300, 30), "Camera: MANUAL POSITIONING", style);
            }
        }
        else
        {
            GUI.Label(new Rect(10, 50, 300, 30), "WASD - Fly around", style);
            GUI.Label(new Rect(10, 70, 300, 30), "QE - Up/Down", style);
            GUI.Label(new Rect(10, 90, 300, 30), $"Hold '{fastMoveKey}' - Fast move", style);
            GUI.Label(new Rect(10, 110, 300, 30), "Mouse - Look around", style);

            style.normal.textColor = Color.cyan;
            GUI.Label(new Rect(10, 130, 300, 30), "Player: NOT SPAWNED", style);
            GUI.Label(new Rect(10, 150, 300, 30), "Camera: FREE FLYING", style);
        }

        style.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 170, 300, 30), "ESC - Toggle cursor lock", style);

        if (!cursorLocked)
        {
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(10, 190, 300, 30), "Cursor Unlocked - Click to lock", style);
        }

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            style.normal.textColor = Color.gray;
            style.fontSize = 12;
            GUI.Label(new Rect(10, 220, 300, 20), $"Spawn Points: {spawnPoints.Length} available", style);
        }
    }

    #endregion
}
