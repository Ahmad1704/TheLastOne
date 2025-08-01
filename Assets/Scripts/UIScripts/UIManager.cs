using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject freeCameraPanel;
    public GameObject firstPersonPanel;

    [Header("Reload UI")]
    public GameObject reloadPanel;
    public TextMeshProUGUI reloadText;
    [Range(0.5f, 5f)] public float blinkSpeed = 2f;
    [Range(0.1f, 1f)] public float minAlpha = 0.3f;

    private CameraManager cameraManager;
    private WeaponHandler weaponHandler;
    private bool isFirstPersonMode = false;
    private bool showReloadUI = false;
    private float blinkTimer = 0f;

    // Singleton pattern for easy access
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[UIManager] Multiple UIManager instances found!");
        }
        
        cameraManager = FindFirstObjectByType<CameraManager>();
        if (!cameraManager)
        {
            Debug.LogError("UIManager: CameraManager not found!");
        }
    }

    private void Start()
    {
        UpdatePanels();
        HideReloadUI();
        
        // Try to find WeaponHandler, but don't worry if it's not there yet
        TryConnectToWeaponHandler();
    }

    private void Update()
    {
        if (!cameraManager) return;

        bool currentMode = GetCameraMode();
        if (currentMode != isFirstPersonMode)
        {
            isFirstPersonMode = currentMode;
            UpdatePanels();
            if (!isFirstPersonMode) HideReloadUI();
        }

        // Continuously try to connect to WeaponHandler if not connected
        if (weaponHandler == null)
        {
            TryConnectToWeaponHandler();
        }

        if (showReloadUI && reloadPanel?.activeInHierarchy == true)
        {
            UpdateReloadBlink();
        }

        // Debug key for testing
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleReloadUI();
        }
    }

    private void TryConnectToWeaponHandler()
    {
        WeaponHandler foundWeaponHandler = FindFirstObjectByType<WeaponHandler>();
        if (foundWeaponHandler != null && foundWeaponHandler != weaponHandler)
        {
            // Disconnect from old weapon handler if exists
            if (weaponHandler != null)
            {
                weaponHandler.OnAmmoChanged -= OnWeaponAmmoChanged;
                weaponHandler.OnWeaponEmpty -= OnWeaponEmpty;
            }

            // Connect to new weapon handler
            weaponHandler = foundWeaponHandler;
            weaponHandler.OnAmmoChanged += OnWeaponAmmoChanged;
            weaponHandler.OnWeaponEmpty += OnWeaponEmpty;
            
            // Force initial UI update
            if (weaponHandler.currentWeapon != null)
            {
                OnWeaponAmmoChanged(weaponHandler.currentAmmoInMag, 
                    AmmoManager.Instance?.GetAmmo(weaponHandler.currentWeapon.ammoType) ?? 0);
            }
        }
    }

    // Public method for WeaponHandler to register itself
    public void RegisterWeaponHandler(WeaponHandler handler)
    {
        if (handler == null) return;
        
        // Disconnect from old weapon handler if exists
        if (weaponHandler != null)
        {
            weaponHandler.OnAmmoChanged -= OnWeaponAmmoChanged;
            weaponHandler.OnWeaponEmpty -= OnWeaponEmpty;
        }

        // Connect to new weapon handler
        weaponHandler = handler;
        weaponHandler.OnAmmoChanged += OnWeaponAmmoChanged;
        weaponHandler.OnWeaponEmpty += OnWeaponEmpty;
        
        // Force initial UI update
        if (weaponHandler.currentWeapon != null)
        {
            OnWeaponAmmoChanged(weaponHandler.currentAmmoInMag, 
                AmmoManager.Instance?.GetAmmo(weaponHandler.currentWeapon.ammoType) ?? 0);
        }
    }

    private bool GetCameraMode()
    {
        var field = typeof(CameraManager).GetField("isFirstPerson",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool result = field != null && (bool)field.GetValue(cameraManager);
        return result;
    }

    private void UpdatePanels()
    {
        freeCameraPanel?.SetActive(!isFirstPersonMode);
        firstPersonPanel?.SetActive(isFirstPersonMode);
    }

    private void UpdateReloadBlink()
    {
        blinkTimer += Time.deltaTime * blinkSpeed;
        float alpha = Mathf.Lerp(minAlpha, 1f, (Mathf.Sin(blinkTimer) + 1f) * 0.5f);

        if (reloadText != null)
        {
            var color = reloadText.color;
            color.a = alpha;
            reloadText.color = color;
        }
    }

    public void ShowReloadUI()
    {
        showReloadUI = true;

        if (reloadPanel != null)
        {
            reloadPanel.SetActive(true);
            blinkTimer = 0f;
            
            // Set the reload text
            if (reloadText != null)
            {
                reloadText.text = "RELOAD";
            }
        }
    }

    public void HideReloadUI()
    {
        showReloadUI = false;

        if (reloadPanel != null)
        {
            reloadPanel.SetActive(false);
        }
    }

    public void ToggleReloadUI()
    {
        if (showReloadUI)
            HideReloadUI();
        else
            ShowReloadUI();
    }

    private void OnWeaponAmmoChanged(int currentAmmoInMag, int reserveAmmo)
    {
        if (currentAmmoInMag <= 0)
        {
            ShowReloadUI();
        }
        else
        {
            HideReloadUI();
        }
    }

    private void OnWeaponEmpty()
    {
        ShowReloadUI();
    }

    private void OnDestroy()
    {
        Debug.Log("[UIManager] OnDestroy called");
        if (weaponHandler != null)
        {
            weaponHandler.OnAmmoChanged -= OnWeaponAmmoChanged;
            weaponHandler.OnWeaponEmpty -= OnWeaponEmpty;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}