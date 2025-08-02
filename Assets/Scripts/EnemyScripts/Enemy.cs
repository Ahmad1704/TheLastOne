using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    #region Enums and Constants
    public enum EnemyType { Basic, Fast, Heavy }
    #endregion

    #region Inspector Variables
    [Header("Enemy Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private EnemyType enemyType;

    [Header("Visual Effects")]
    [SerializeField] private Material eyeGlowMaterial;
    [SerializeField] private Transform[] eyeTransforms;
    [SerializeField] private GameObject eyeSpherePrefab; // NEW: Prefab reference instead of dynamic creation

    [Header("NavMesh Settings")]
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private int navMeshAreaMask = -1;
    #endregion

    #region Cached Components
    private WaveManager waveManager;
    private NavMeshAgent navAgent;
    private Health healthComponent;
    private Transform cachedTransform;
    private Animator animator;
    #endregion

    #region State Instances
    private EnemyStateMachine stateMachine;
    private BasicMovementState basicState;
    private FastZigzagState fastState;
    private HeavyChargeState heavyState;
    private DeadState deadState;
    private SpawningState spawningState;
    #endregion

    #region Private Variables
    private bool isDead = false;
    private float originalMoveSpeed;
    private Vector3 originalScale;
    private Vector3 originalPosition; // Store original local position
    private Quaternion originalRotation; // Store original local rotation
    private float currentSmoothedSpeed;
    private bool isNavAgentReady = false;
    #endregion

    #region Properties
    public EnemyStateMachine StateMachine => stateMachine;
    public NavMeshAgent NavAgent => navAgent;
    public float MoveSpeed => moveSpeed;
    public Vector3 OriginalScale => originalScale;
    public Transform CachedTransform => cachedTransform;
    public Animator Animator => animator;
    public bool IsNavAgentReady => isNavAgentReady;
    #endregion

    #region Unity Lifecycle Methods
    void Awake()
    {
        GetComponents();
        CreateStateInstances();
        CacheOriginalValues();
        
        SetupNavMeshAgent();
        SetupGlowingEyes();
        
        // Subscribe to health events
        if (healthComponent != null)
        {
            healthComponent.OnDeath += OnHealthDepleted;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from health events to prevent memory leaks
        if (healthComponent != null)
        {
            healthComponent.OnDeath -= OnHealthDepleted;
        }
    }

    void Update()
    {
        if (!isDead)
        {
            if (navAgent != null && animator != null && isNavAgentReady)
            {
                UpdateMovementAnimation();
            }

            // Let the state machine handle updates
            stateMachine.Update();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (navAgent != null && navAgent.hasPath && isNavAgentReady)
        {
            Gizmos.color = Color.yellow;
            Vector3[] pathCorners = navAgent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
            }
        }
    }
    #endregion

    #region Initialization Methods

    private void GetComponents()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        healthComponent = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();
    }
    
    private void CreateStateInstances()
    {
        basicState = new BasicMovementState();
        fastState = new FastZigzagState();
        heavyState = new HeavyChargeState();
        deadState = new DeadState();
        spawningState = new SpawningState();
    }

    private void CacheOriginalValues()
    {
        cachedTransform = transform;
        currentSmoothedSpeed = 0f;
        stateMachine = new EnemyStateMachine(this);
        originalMoveSpeed = moveSpeed;
        originalScale = cachedTransform.localScale;
        originalPosition = cachedTransform.localPosition;
        originalRotation = cachedTransform.localRotation;
    }

    public void Initialize(WaveManager manager)
    {
        waveManager = manager;
        isDead = false;
        moveSpeed = originalMoveSpeed;
        
        // Reset scale and rotation, but NOT position (WaveManager handles positioning)
        cachedTransform.localScale = originalScale;
        cachedTransform.localRotation = originalRotation;
        // Don't reset position here - it's set by WaveManager after this call

        ApplyTypeModifications();
        
        // Reset animator state properly
        if (animator != null)
        {
            animator.applyRootMotion = false; // Disable root motion during setup
            animator.ResetTrigger("Die");
            animator.SetFloat("Speed", 0f);
            // Force animator to update to default state
            animator.Update(0f);
        }
        
        // Setup NavMesh agent properly
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.enabled = true; // Enable first
            
            // Wait a frame for NavMesh to be ready, then start spawning
            StartCoroutine(WaitForNavMeshAndStart());
        }
        else
        {
            // If no NavMesh agent, start immediately
            StartSpawning();
        }

        if (healthComponent != null)
        {
            healthComponent.ResetHealth();
        }
    }

   private IEnumerator WaitForNavMeshAndStart()
{
    // Wait a frame to ensure NavMesh is ready
    yield return null;
    
    // Check if agent is on NavMesh
    if (navAgent.isOnNavMesh)
    {
        isNavAgentReady = true;
        navAgent.isStopped = true; // Start stopped
        StartSpawning();
    }
    else
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(cachedTransform.position, out hit, 10f, NavMesh.AllAreas))
        {
            navAgent.Warp(hit.position);
            isNavAgentReady = true;
            navAgent.isStopped = true; // Start stopped
            StartSpawning();
        }
        else
        {
            isNavAgentReady = false;
            // Try to find a better spawn position
            Vector3 randomPos = cachedTransform.position + Random.insideUnitSphere * 5f;
            randomPos.y = cachedTransform.position.y;
            
            if (NavMesh.SamplePosition(randomPos, out hit, 10f, NavMesh.AllAreas))
            {
                cachedTransform.position = hit.position;
                navAgent.Warp(hit.position);
                isNavAgentReady = true;
                navAgent.isStopped = true;
                StartSpawning();
            }
        }
    }
}

    private void StartSpawning()
    {
        stateMachine.ChangeState(spawningState);
    }
    #endregion

    #region Setup Methods
    private void SetupNavMeshAgent()
    {
        if (navAgent == null) return;

        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.areaMask = navMeshAreaMask;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        switch (enemyType)
        {
            case EnemyType.Fast:
                navAgent.radius = 0.4f;
                navAgent.height = 1.6f;
                navAgent.baseOffset = 1f;
                break;
            case EnemyType.Heavy:
                navAgent.radius = 0.8f;
                navAgent.height = 2.2f;
                navAgent.baseOffset = 1f;
                break;
            default:
                navAgent.radius = 0.5f;
                navAgent.height = 2f;
                navAgent.baseOffset = 1f;
                break;
        }
    }

    private void SetupGlowingEyes()
    {
        // Return early if no prefab is assigned or no eye transforms
        if (eyeSpherePrefab == null || eyeTransforms.Length == 0) return;

        foreach (Transform eyeTransform in eyeTransforms)
        {
            // Instantiate the prefab instead of creating from scratch
            GameObject eye = Instantiate(eyeSpherePrefab, eyeTransform);
            eye.name = "Eye";
            eye.transform.localPosition = Vector3.zero;
            eye.transform.localScale = Vector3.one;

            // Get the renderer from the prefab (should already be set up)
            MeshRenderer renderer = eye.GetComponent<MeshRenderer>();
            if (renderer != null && eyeGlowMaterial != null)
            {
                // Create a new material instance to avoid modifying shared material
                Material eyeMaterial = new Material(eyeGlowMaterial);
                renderer.material = eyeMaterial;

                // Choose eye color based on enemy type
                Color eyeColor = enemyType switch
                {
                    EnemyType.Fast => Color.blue,
                    EnemyType.Heavy => Color.yellow,
                    _ => Color.red
                };

                // URP Material Setup
                // Set base color (URP standard)
                eyeMaterial.SetColor("_BaseColor", eyeColor);

                // Enable emission
                eyeMaterial.EnableKeyword("_EMISSION");
                eyeMaterial.SetColor("_EmissionColor", eyeColor * 3f); // Boost glow intensity

                // Alternative URP emission property names (try both for compatibility)
                if (eyeMaterial.HasProperty("_EmissiveColor"))
                {
                    eyeMaterial.SetColor("_EmissiveColor", eyeColor * 3f);
                }

                // Set emission intensity if available
                if (eyeMaterial.HasProperty("_EmissiveIntensity"))
                {
                    eyeMaterial.SetFloat("_EmissiveIntensity", 3f);
                }

                // Ensure real-time emission (important for baked/global illumination systems)
                eyeMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                // URP-specific: Enable HDR emission if supported
                if (eyeMaterial.HasProperty("_UseEmissiveIntensity"))
                {
                    eyeMaterial.SetFloat("_UseEmissiveIntensity", 1f);
                }
            }
        }
    }
    #endregion

    #region State Machine Methods
    public IEnemyState GetMovementState()
    {
        return enemyType switch
        {
            EnemyType.Fast => fastState,
            EnemyType.Heavy => heavyState,
            _ => basicState
        };
    }
    #endregion

    #region Health Event Handlers
    private void OnHealthDepleted()
    {
        if (!isDead)
        {
            // Stop NavMesh movement immediately
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
            }
            
            stateMachine.ChangeState(deadState);
        }
    }
    #endregion

    #region Public Methods
    public void OnEnemyDied()
    {
        if (isDead) return;
        isDead = true;
        
        // Notify wave manager immediately to decrement counter
        waveManager?.OnEnemyDestroyed(this);
        
        // Start the death sequence
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedDeactivate());
        }
        else
        {
            // If already inactive, just handle cleanup immediately
            ResetForPool();
            waveManager?.OnEnemyReadyForPool(this, gameObject);
        }
    }

    private IEnumerator DelayedDeactivate()
    {
        yield return new WaitForSeconds(2f); // Adjust based on death animation length
        ResetForPool();
        waveManager?.OnEnemyReadyForPool(this, gameObject);
    }

   public void ResetForPool()
{
    isDead = false;
    isNavAgentReady = false;

    // Reset Health
    if (healthComponent != null)
        healthComponent.ResetHealth();

    // CRITICAL: Reset Animator BEFORE resetting transform
    if (animator != null)
    {
        // Force animator to exit any current state
        animator.ResetTrigger("Die");
        animator.SetFloat("Speed", 0f);
        
        // Disable root motion to prevent it from affecting transform
        animator.applyRootMotion = false;
        
        // Force animator to update and go to default state
        animator.Update(0f);
        
        // Rebind the animator to reset any internal state
        animator.Rebind();
    }

    // Reset NavMeshAgent
    if (navAgent != null)
    {
        if (navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        navAgent.enabled = false; // Important to fully reset nav state
    }

    // Reset Transform - only reset rotation and scale, position will be set by WaveManager
    cachedTransform.localRotation = originalRotation;
    cachedTransform.localScale = originalScale;
    // Don't reset position here - it causes spawning at 0,0,0

    gameObject.SetActive(false);
}
    #endregion

    #region Helper Methods
    private void ApplyTypeModifications()
    {
        switch (enemyType)
        {
            case EnemyType.Fast:
                moveSpeed *= 1.8f;
                if (healthComponent != null) healthComponent.maxHealth *= 0.7f;
                cachedTransform.localScale = originalScale * 0.8f;
                break;
            case EnemyType.Heavy:
                moveSpeed *= 0.5f;
                if (healthComponent != null) healthComponent.maxHealth *= 1.8f;
                cachedTransform.localScale = originalScale * 1.3f;
                break;
        }
    }

    private void UpdateMovementAnimation()
    {
        float normalizedSpeed = 0f;
        if (navAgent.speed > 0)
        {
            normalizedSpeed = navAgent.velocity.magnitude / navAgent.speed;
            normalizedSpeed = Mathf.Clamp01(normalizedSpeed);
        }
        
        float smoothFactor = enemyType == EnemyType.Fast ? 10f : 5f;
        currentSmoothedSpeed = Mathf.Lerp(
            currentSmoothedSpeed, 
            normalizedSpeed, 
            smoothFactor * Time.deltaTime
        );
        
        animator.SetFloat("Speed", currentSmoothedSpeed);
    }
    #endregion
}