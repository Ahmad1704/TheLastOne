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
    private float currentSmoothedSpeed;
    #endregion

    #region Properties
    public EnemyStateMachine StateMachine => stateMachine;
    public NavMeshAgent NavAgent => navAgent;
    public float MoveSpeed => moveSpeed;
    public Vector3 OriginalScale => originalScale;
    public Transform CachedTransform => cachedTransform;
    public Animator Animator => animator;
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
            if (navAgent != null && animator != null)
            {
                UpdateMovementAnimation();
            }

            // Let the state machine handle updates
            stateMachine.Update();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (navAgent != null && navAgent.hasPath)
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
    }

    public void Initialize(WaveManager manager)
    {
        waveManager = manager;
        isDead = false;
        moveSpeed = originalMoveSpeed;
        cachedTransform.localScale = originalScale;

        ApplyTypeModifications();
        
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.enabled = false;
        }

        if (healthComponent != null)
        {
            healthComponent.ResetHealth();
        }
        
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
        if (eyeGlowMaterial == null || eyeTransforms.Length == 0) return;

        foreach (Transform eyeTransform in eyeTransforms)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.transform.SetParent(eyeTransform, false);
            eye.transform.localPosition = Vector3.zero;
            eye.transform.localScale = Vector3.one * 0.2f;

            Renderer eyeRenderer = eye.GetComponent<Renderer>();
            eyeRenderer.material = eyeGlowMaterial;
            eyeRenderer.material.EnableKeyword("_EMISSION");
            
            Color eyeColor = enemyType switch
            {
                EnemyType.Fast => Color.yellow,
                EnemyType.Heavy => Color.blue,
                _ => Color.red
            };
            
            eyeRenderer.material.SetColor("_EmissionColor", eyeColor * 2f);
            Destroy(eye.GetComponent<Collider>());
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
            Debug.Log($"Health depleted for {gameObject.name}, changing to dead state");
            
            // Stop NavMesh movement immediately
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
            }
            
            // Trigger death animation directly here as well as a backup
            if (animator != null)
            {
                Debug.Log($"Triggering death animation directly for {gameObject.name}");
                animator.SetTrigger("Die");
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
        Debug.Log($"Enemy {gameObject.name} died");
        
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
        yield return new WaitForSeconds(2f); // adjust based on death animation length
        ResetForPool();
        waveManager?.OnEnemyReadyForPool(this, gameObject);
    }

    public void ResetForPool()
    {
        isDead = false; // Reset the dead flag for pooling
        
        if (healthComponent != null)
            healthComponent.ResetHealth();

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        // Reset animator state
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.SetFloat("Speed", 0f);
        }

        cachedTransform.position = Vector3.zero;
        cachedTransform.rotation = Quaternion.identity;
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