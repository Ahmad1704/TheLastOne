using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;
    public GameObject weaponPrefab;
    public GameObject bulletPrefab;

    [Header("Combat Stats")]
    public float damage = 10f;
    public float fireRate = 0.5f;
    public float bulletSpeed = 40f;
    public float range = 100f;

    [Header("Ammo Settings")]
    public AmmoType ammoType = AmmoType.Rifle;
    public int magazineSize = 30;
    public float reloadTime = 2f;
    public bool canReloadPartially = true;
    
    [Header("Reload Settings")]
    public ReloadType reloadType = ReloadType.Magazine;
    public int shellsPerReload = 1;
    public float shellReloadTime = 0.8f;
    public bool canCancelReload = true;

    [Header("Fire Point Settings")]
    public string firePointName = "FirePoint"; // Name to search for in prefab
    public Vector3 firePointOffset = Vector3.zero; // Offset from weapon root if no named point found
    
    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip shellInsertSound;
}

// ==== AMMO TYPES ====
public enum AmmoType
{
    Pistol,
    Rifle,
    Shotgun,
    Sniper,
    Rocket,
    Energy
}

public enum ReloadType
{
    Magazine,    // Full magazine swap
    ShellByShell // Individual shell loading (shotguns)
}