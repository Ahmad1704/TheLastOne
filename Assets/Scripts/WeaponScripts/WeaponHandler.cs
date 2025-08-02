using UnityEngine;
using System.Collections;
using TMPro; 
public class WeaponHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource audioSource;

    [Header("Ammo Display")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private AmmoType currentWeaponAmmoType = AmmoType.Rifle;
    [SerializeField] private TextMeshProUGUI reserveText;



    [Header("Current State")]
    public WeaponData currentWeapon;
    public int currentAmmoInMag;
    public bool isReloading;
    public bool isEmpty;

    private GameObject currentWeaponInstance;
    private WeaponPrefabData currentWeaponPrefabData;
    private Transform currentFirePoint;

    private Coroutine reloadCoroutine;
    private Coroutine shellReloadCoroutine;

    public System.Action<int, int> OnAmmoChanged;
    public System.Action<float> OnReloadProgress;
    public System.Action OnReloadStart;
    public System.Action OnReloadComplete;
    public System.Action OnWeaponEmpty;

    private void Start()
{
    if (playerCamera == null)
        playerCamera = Camera.main; 

    if (currentWeapon != null)
        InitializeWeapon();
    
    // Register with UIManager
    if (UIManager.Instance != null)
    {
        UIManager.Instance.RegisterWeaponHandler(this);
        Debug.Log("[WeaponHandler] Registered with UIManager");
    }
    else
    {
        Debug.LogWarning("[WeaponHandler] UIManager not found!");
    }
}

    public void InitializeWeapon()
    {
        currentAmmoInMag = currentWeapon.magazineSize;
        isEmpty = false;
        SetupWeaponInstance();
        UpdateAmmoUI();
    }
    private Vector3 GetTargetPoint()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        return Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range)
            ? hit.point
            : ray.GetPoint(currentWeapon.range);
    }
    private void Update()
    {
        UpdateAmmoDisplay(currentAmmoInMag, currentWeaponAmmoType, currentWeapon?.magazineSize ?? -1);
    }
    private void SetupWeaponInstance()
    {
        if (currentWeaponInstance != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(currentWeaponInstance);
#else
            Destroy(currentWeaponInstance);
#endif
            currentWeaponPrefabData = null;
            currentFirePoint = null;
        }

        if (currentWeapon?.weaponPrefab == null) return;

        Transform parent = weaponHolder != null ? weaponHolder : transform;
        currentWeaponInstance = Instantiate(currentWeapon.weaponPrefab, parent);
        currentWeaponInstance.transform.localPosition = Vector3.zero;
        currentWeaponInstance.transform.localRotation = Quaternion.identity;

        SetupFirePoint();
    }

    private void SetupFirePoint()
    {
        if (currentWeaponInstance.TryGetComponent(out WeaponPrefabData prefabData))
        {
            currentWeaponPrefabData = prefabData;
            currentFirePoint = prefabData.GetFirePoint();
        }
        else
        {
            currentWeaponPrefabData = null;
            currentFirePoint = FirePointFinder.FindFirePoint(currentWeaponInstance.transform, currentWeapon.firePointName);
        }
    }

    public bool CanFire()
    {
        return currentWeapon != null &&
               currentAmmoInMag > 0 &&
               !isReloading &&
               !isEmpty &&
               currentFirePoint != null;
    }

    public void Fire()
    {
        if (!CanFire())
        {
            HandleEmptyFire();
            return;
        }

        Vector3 targetPoint = GetTargetPoint();
        Vector3 direction = (targetPoint - currentFirePoint.position).normalized;
        Quaternion fireRotation = Quaternion.LookRotation(direction);

        SpawnBullet(currentFirePoint.position, fireRotation);
        currentAmmoInMag--;

        PlaySound(currentWeapon.fireSound);

        if (currentAmmoInMag <= 0)
            isEmpty = true;

        UpdateAmmoUI();
    }
    private void SpawnBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, position, rotation);
        if (bullet.TryGetComponent(out Bullet bulletScript))
        {
            bulletScript.Initialize(currentWeapon.damage, currentWeapon.range, currentWeapon.bulletSpeed);
        }
    }

    private void HandleEmptyFire()
    {
        if (currentAmmoInMag <= 0 && currentWeapon.emptySound != null)
        {
            PlaySound(currentWeapon.emptySound);
            OnWeaponEmpty?.Invoke();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    public bool CanReload()
    {
        if (currentWeapon == null || isReloading) return false;
        if (!AmmoManager.Instance.HasAmmo(currentWeapon.ammoType)) return false;
        if (!currentWeapon.canReloadPartially && currentAmmoInMag > 0) return false;

        return currentAmmoInMag < currentWeapon.magazineSize;
    }

    public void StartReload()
    {
        if (!CanReload()) return;

        CancelReload();

        if (currentWeapon.reloadType == ReloadType.Magazine)
            reloadCoroutine = StartCoroutine(MagazineReload());
        else
            shellReloadCoroutine = StartCoroutine(ShellByShellReload());
    }

    public void CancelReload()
    {
        if (!isReloading || !currentWeapon.canCancelReload) return;

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        if (shellReloadCoroutine != null)
        {
            StopCoroutine(shellReloadCoroutine);
            shellReloadCoroutine = null;
        }

        isReloading = false;
        OnReloadProgress?.Invoke(0f);
        UpdateAmmoUI();
    }

    private IEnumerator MagazineReload()
    {
        isReloading = true;
        OnReloadStart?.Invoke();
        PlaySound(currentWeapon.reloadSound);

        float timer = 0f;
        while (timer < currentWeapon.reloadTime)
        {
            timer += Time.deltaTime;
            OnReloadProgress?.Invoke(timer / currentWeapon.reloadTime);
            yield return null;
        }

        CompleteReload();
    }

    private IEnumerator ShellByShellReload()
    {
        isReloading = true;
        OnReloadStart?.Invoke();

        int shellsToLoad = currentWeapon.magazineSize - currentAmmoInMag;

        while (shellsToLoad > 0 &&
               AmmoManager.Instance.HasAmmo(currentWeapon.ammoType) &&
               currentAmmoInMag < currentWeapon.magazineSize)
        {
            float elapsed = 0f;
            while (elapsed < currentWeapon.shellReloadTime)
            {
                elapsed += Time.deltaTime;
                float totalProgress = (currentAmmoInMag + (elapsed / currentWeapon.shellReloadTime)) / currentWeapon.magazineSize;
                OnReloadProgress?.Invoke(totalProgress);
                yield return null;
            }

            if (AmmoManager.Instance.UseAmmo(currentWeapon.ammoType, 1))
            {
                currentAmmoInMag++;
                isEmpty = false;
                shellsToLoad--;

                PlaySound(currentWeapon.shellInsertSound);
                UpdateAmmoUI();
            }
            else
            {
                break;
            }
        }

        isReloading = false;
        OnReloadProgress?.Invoke(1f);
        OnReloadComplete?.Invoke();
        shellReloadCoroutine = null;
    }

    private void CompleteReload()
    {
        int needed = currentWeapon.magazineSize - currentAmmoInMag;
        int available = AmmoManager.Instance.GetAmmo(currentWeapon.ammoType);
        int toLoad = Mathf.Min(needed, available);

        if (AmmoManager.Instance.UseAmmo(currentWeapon.ammoType, toLoad))
        {
            currentAmmoInMag += toLoad;
            isEmpty = false;
        }

        isReloading = false;
        OnReloadProgress?.Invoke(1f);
        OnReloadComplete?.Invoke();
        UpdateAmmoUI();
        reloadCoroutine = null;
    }

    private void UpdateAmmoUI()
    {
        OnAmmoChanged?.Invoke(
            currentAmmoInMag,
            AmmoManager.Instance.GetAmmo(currentWeapon.ammoType)
        );
    }

    public void SwitchWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;
        if (newWeapon != null)
            InitializeWeapon();
    }

    public void RefreshWeapon()
    {
        if (currentWeapon != null)
        {
            SetupWeaponInstance();
            UpdateAmmoUI();
        }
    }

    private void OnDestroy()
    {
        if (currentWeaponInstance != null)
#if UNITY_EDITOR
            DestroyImmediate(currentWeaponInstance);
#else
            Destroy(currentWeaponInstance);
#endif
    }

    public void UpdateAmmoDisplay(int magazineAmmo, AmmoType weaponAmmoType, int magazineCapacity = -1)
    {
        currentAmmoInMag = magazineAmmo;
        currentWeaponAmmoType = weaponAmmoType;

        if (magazineCapacity > 0)
        {
            currentWeapon.magazineSize = magazineCapacity;
        }

        // Get reserve ammo from AmmoManager
        int reserveAmmo = 0;
        if (AmmoManager.Instance != null)
        {
            reserveAmmo = AmmoManager.Instance.GetAmmo(weaponAmmoType);
        }

        // Update the main ammo text (common format: "30 / 120")
        if (ammoText != null)
        {
            ammoText.text = $"{magazineAmmo} / {currentWeapon.magazineSize}";

            // Optional: Change color based on ammo status
            if (magazineAmmo == 0)
            {
                ammoText.color = Color.red; // Empty magazine
            }
            else if (magazineAmmo <= currentWeapon.magazineSize * 0.3f)
            {
                ammoText.color = Color.yellow; // Low ammo warning
            }
            else
            {
                ammoText.color = Color.white; // Normal
            }
        }
        // Update separate reserve text if you have one
        if (reserveText != null)
        {
            reserveText.text = $"{reserveAmmo}";
            reserveText.color = reserveAmmo > 0 ? Color.white : Color.gray;
        }
    }

    // Optional debug UI
    private void OnGUI()
    {
        if (currentWeapon == null) return;
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Label($"Weapon: {currentWeapon.weaponName}");
        GUILayout.Label($"Prefab: {(currentWeaponInstance != null ? "Loaded" : "None")}");
        GUILayout.Label($"Fire Point: {(currentFirePoint != null ? "Found" : "Missing")}");
        GUILayout.Label($"Mag: {currentAmmoInMag}/{currentWeapon.magazineSize}");
        GUILayout.Label($"Reserve: {AmmoManager.Instance.GetAmmo(currentWeapon.ammoType)}");
        GUILayout.Label($"Reloading: {isReloading}");
        GUILayout.Label($"Empty: {isEmpty}");
        GUILayout.EndArea();
    }
}