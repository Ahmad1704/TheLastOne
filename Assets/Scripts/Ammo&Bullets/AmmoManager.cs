using System;
using System.Collections.Generic;
using UnityEngine;
public class AmmoManager : MonoBehaviour
{
    public static AmmoManager Instance { get; private set; }

    [Header("Starting Ammo")]
    public List<AmmoInventoryItem> startingAmmo = new List<AmmoInventoryItem>();

    private Dictionary<AmmoType, int> ammoInventory = new Dictionary<AmmoType, int>();

    public event Action<AmmoType, int> OnAmmoChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAmmo();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAmmo()
    {
        // Initialize all ammo types to 0
        foreach (AmmoType type in Enum.GetValues(typeof(AmmoType)))
        {
            ammoInventory[type] = 0;
        }

        // Set starting ammo
        foreach (var item in startingAmmo)
        {
            ammoInventory[item.ammoType] = item.amount;
        }
    }

    public int GetAmmo(AmmoType type)
    {
        return ammoInventory.TryGetValue(type, out int amount) ? amount : 0;
    }

    public bool HasAmmo(AmmoType type, int amount = 1)
    {
        return GetAmmo(type) >= amount;
    }

    public bool UseAmmo(AmmoType type, int amount)
    {
        if (!HasAmmo(type, amount)) return false;

        ammoInventory[type] -= amount;
        OnAmmoChanged?.Invoke(type, ammoInventory[type]);
        return true;
    }

    public void AddAmmo(AmmoType type, int amount)
    {
        ammoInventory[type] += amount;
        OnAmmoChanged?.Invoke(type, ammoInventory[type]);
    }

    public void SetAmmo(AmmoType type, int amount)
    {
        ammoInventory[type] = Mathf.Max(0, amount);
        OnAmmoChanged?.Invoke(type, ammoInventory[type]);
    }
}
public class AmmoInventoryItem
{
    public AmmoType ammoType;
    public int amount;
}