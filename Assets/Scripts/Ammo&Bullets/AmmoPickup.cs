using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    public AmmoType ammoType = AmmoType.Rifle;
    public int ammoAmount = 30;
    
    [Header("Visual")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;
    
    [Header("Pickup Settings")]
    public float pickupRadius = 2f;
    
    private bool hasBeenPickedUp = false;
    private GameObject playerObject;
    private float lastProximityCheckTime;

    void Start()
    {
        SetupCollider();
        
        // Try to find player immediately
        FindPlayer();
        
        // If not found, subscribe to player spawn event or use coroutine
        if (playerObject == null)
        {
            StartCoroutine(WaitForPlayerSpawn());
        }
        
        // Auto-destroy after 20 seconds if not picked up
        Invoke(nameof(AutoDestroy), 20f);
    }

    void OnEnable()
    {
        // Subscribe to player spawn events if you have an event system
        // PlayerSpawner.OnPlayerSpawned += OnPlayerSpawned;
        // GameManager.OnPlayerReady += OnPlayerSpawned;
    }

    void OnDisable()
    {
        // Unsubscribe from events
        // PlayerSpawner.OnPlayerSpawned -= OnPlayerSpawned;
        // GameManager.OnPlayerReady -= OnPlayerSpawned;
    }

    // Event handler for when player spawns
    private void OnPlayerSpawned(GameObject player)
    {
        if (playerObject == null)
        {
            playerObject = player;
            Debug.Log($"AmmoPickup: Player spawned and registered: {player.name}");
        }
    }

    // Coroutine to wait for player spawn (alternative to Update searching)
    private System.Collections.IEnumerator WaitForPlayerSpawn()
    {
        while (playerObject == null)
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
            FindPlayer();
        }
        
        Debug.Log($"AmmoPickup: Found player after waiting: {playerObject.name}");
    }

    private void SetupCollider()
    {
        Collider pickupCollider = GetComponent<Collider>();
        
        if (pickupCollider == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = pickupRadius;
            sphereCollider.isTrigger = true;
            pickupCollider = sphereCollider;
        }
        else
        {
            pickupCollider.isTrigger = true;
        }

        // Ensure minimum size for WebGL
        if (pickupCollider is SphereCollider sphere)
        {
            sphere.radius = Mathf.Max(0.5f, pickupRadius);
        }
    }

    void Update()
    {
        if (hasBeenPickedUp) return;

        // Only check proximity if we have a player reference
        // No more player searching in Update!
        if (playerObject != null && Time.time - lastProximityCheckTime >= 0.1f)
        {
            CheckPlayerProximity();
            lastProximityCheckTime = Time.time;
        }
    }

    private void FindPlayer()
    {
        // Try multiple methods to find the player
        playerObject = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObject == null)
        {
            // Fallback: look for common player components
            PlayerController controller = FindFirstObjectByType<PlayerController>();
            if (controller != null)
            {
                playerObject = controller.gameObject;
            }
        }

        if (playerObject == null)
        {
            // Another fallback: look for FirstPersonController or similar
            var fpController = FindFirstObjectByType<PlayerController>();
            if (fpController != null)
            {
                playerObject = fpController.gameObject;
            }
        }
    }

    private void CheckPlayerProximity()
    {
        if (playerObject == null) return;

        float distance = Vector3.Distance(transform.position, playerObject.transform.position);
        if (distance <= pickupRadius)
        {
            PickupAmmo();
        }
    }

    // Primary pickup method - works with triggers
    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenPickedUp) return;

        // Check if it's the player (multiple ways)
        bool isPlayer = other.CompareTag("Player") || 
                       other.gameObject == playerObject ||
                       other.GetComponent<PlayerController>() != null ||
                       other.GetComponentInParent<PlayerController>() != null;

        if (isPlayer)
        {
            // Cache the player reference if we didn't have it
            if (playerObject == null)
            {
                playerObject = other.gameObject;
                Debug.Log($"AmmoPickup: Player found via trigger: {other.gameObject.name}");
            }
            
            PickupAmmo();
        }
    }

    // Backup trigger method for WebGL
    private void OnTriggerStay(Collider other)
    {
        if (hasBeenPickedUp) return;

        bool isPlayer = other.CompareTag("Player") || 
                       other.gameObject == playerObject ||
                       other.GetComponent<PlayerController>() != null;

        if (isPlayer)
        {
            if (playerObject == null)
            {
                playerObject = other.gameObject;
                Debug.Log($"AmmoPickup: Player found via trigger stay: {other.gameObject.name}");
            }
            
            PickupAmmo();
        }
    }

    private void PickupAmmo()
    {
        if (hasBeenPickedUp) return;
        hasBeenPickedUp = true;

        // Cancel the auto-destroy since we're being picked up
        CancelInvoke(nameof(AutoDestroy));

        // Double-check AmmoManager exists
        if (AmmoManager.Instance == null)
        {
            Debug.LogError("AmmoManager.Instance is null when trying to pickup ammo!");
            return;
        }

        // Add ammo to inventory
        int beforeAmount = AmmoManager.Instance.GetAmmo(ammoType);
        AmmoManager.Instance.AddAmmo(ammoType, ammoAmount);
        int afterAmount = AmmoManager.Instance.GetAmmo(ammoType);
        
        Debug.Log($"Picked up {ammoAmount} {ammoType} ammo ({beforeAmount} → {afterAmount})");
        
        // Visual/Audio feedback
        ShowPickupFeedback();
        
        // Destroy with slight delay for WebGL stability
        Invoke(nameof(DestroyPickup), 0.1f);
    }

    private void ShowPickupFeedback()
    {
        // Visual effect
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, transform.rotation);
            Destroy(effect, 3f);
        }
        
        // Audio feedback (WebGL-optimized)
        if (pickupSound != null)
        {
            try
            {
                // Create temporary audio source for WebGL compatibility
                GameObject audioGO = new GameObject("AmmoPickupAudio");
                audioGO.transform.position = transform.position;
                AudioSource audioSource = audioGO.AddComponent<AudioSource>();
                audioSource.clip = pickupSound;
                audioSource.volume = 0.7f;
                audioSource.spatialBlend = 0f; // 2D audio for pickups
                audioSource.Play();
                
                Destroy(audioGO, pickupSound.length + 0.5f);
            }
            catch
            {
                // Simple fallback
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.7f);
            }
        }
    }

    private void DestroyPickup()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void AutoDestroy()
    {
        if (!hasBeenPickedUp && gameObject != null)
        {
            Debug.Log($"AmmoPickup auto-destroyed after 20 seconds: {ammoAmount} {ammoType}");
            Destroy(gameObject);
        }
    }

    // Visual debug
    void OnDrawGizmos()
    {
        Gizmos.color = hasBeenPickedUp ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        
        // Show connection to player if found
        if (playerObject != null)
        {
            float distance = Vector3.Distance(transform.position, playerObject.transform.position);
            Gizmos.color = distance <= pickupRadius ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(transform.position, playerObject.transform.position);
        }
        else
        {
            // Show that we're looking for player
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
        }
    }

    // Debug info for WebGL builds
    void OnGUI()
    {
        // Only show in development builds
        if (Debug.isDebugBuild)
        {
            string playerStatus = playerObject != null ? $"Found: {playerObject.name}" : "Searching...";
            GUI.Box(new Rect(10, 10, 200, 60), "");
            GUI.Label(new Rect(15, 15, 190, 20), $"Player: {playerStatus}");
            
            if (playerObject != null)
            {
                float dist = Vector3.Distance(transform.position, playerObject.transform.position);
                GUI.Label(new Rect(15, 35, 190, 20), $"Distance: {dist:F2}");
                GUI.Label(new Rect(15, 50, 190, 20), dist <= pickupRadius ? "CAN PICKUP" : "Too far");
            }
        }
    }
}