using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    public AmmoType ammoType = AmmoType.Rifle;
    public int ammoAmount = 30;
    
    [Header("Visual")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Add ammo to inventory
            AmmoManager.Instance.AddAmmo(ammoType, ammoAmount);
            
            // Visual/Audio feedback
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, transform.rotation);
            }
            
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            
            // Destroy pickup
            Destroy(gameObject);
        }
    }
}