using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFire : MonoBehaviour, IPlayerInputHandler
{
    private PlayerInputActions inputActions;
    private WeaponHandler weaponHandler;
    private float lastFireTime;
    private bool isFiring; // Track if fire button is held

    private void Awake()
    {
        weaponHandler = GetComponent<WeaponHandler>();
    }

    public void Register(PlayerInputActions actions)
    {
        inputActions = actions;
        // Changed to handle button press/release states
        inputActions.Gameplay.Fire.started += OnFireStarted;
        inputActions.Gameplay.Fire.canceled += OnFireCanceled;
        inputActions.Gameplay.Reload.performed += OnReload;
    }

    public void Unregister(PlayerInputActions actions)
    {
        inputActions.Gameplay.Fire.started -= OnFireStarted;
        inputActions.Gameplay.Fire.canceled -= OnFireCanceled;
        inputActions.Gameplay.Reload.performed -= OnReload;
    }

    private void OnFireStarted(InputAction.CallbackContext context)
    {
        isFiring = true;
        TryFire(); // Fire immediately on initial press
    }

    private void OnFireCanceled(InputAction.CallbackContext context)
    {
        isFiring = false;
    }

    private void Update()
    {
        // Handle continuous firing while button is held
        if (isFiring)
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (weaponHandler.currentWeapon == null) return;
        
        // Check fire rate cooldown
        if (Time.time - lastFireTime >= weaponHandler.currentWeapon.fireRate)
        {
            weaponHandler.Fire();
            lastFireTime = Time.time;
        }
    }

    private void OnReload(InputAction.CallbackContext context)
    {
        if (weaponHandler.isReloading && weaponHandler.currentWeapon.canCancelReload)
        {
            weaponHandler.CancelReload();
        }
        else
        {
            weaponHandler.StartReload();
        }
    }
}