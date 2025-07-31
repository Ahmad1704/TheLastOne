using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float moveSpeed = 5f;
    public EnemyType enemyType;

    [Header("Visual Effects")]
    public Material eyeGlowMaterial;
    public Transform[] eyeTransforms;

    [Header("NavMesh Settings")]
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private int navMeshAreaMask = -1;

    // Cached components - avoid GetComponent calls
    private WaveManager waveManager;
    private NavMeshAgent navAgent;
    private Health healthComponent;
    private Transform cachedTransform;
    
    private bool isDead = false;
    private float originalMoveSpeed;
    private Vector3 originalScale;
    
    // State Machine with object pooling
    private EnemyStateMachine stateMachine;
    
    // Individual state instances for each enemy (fix for shared state bug)
    private BasicMovementState basicState;
    private FastZigzagState fastState;
    private HeavyChargeState heavyState;
    private DeadState deadState;
    private SpawningState spawningState;

    public enum EnemyType { Basic, Fast, Heavy }

    // Properties with cached references
    public EnemyStateMachine StateMachine => stateMachine;
    public NavMeshAgent NavAgent => navAgent;
    public float MoveSpeed => moveSpeed;
    public Vector3 OriginalScale => originalScale;
    public Transform CachedTransform => cachedTransform;

    void Awake()
    {
        // Cache transform reference
        cachedTransform = transform;
        
        // Get or add NavMeshAgent
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        healthComponent = GetComponent<Health>();
        stateMachine = new EnemyStateMachine(this);

        // Create individual state instances for this enemy
        basicState = new BasicMovementState();
        fastState = new FastZigzagState();
        heavyState = new HeavyChargeState();
        deadState = new DeadState();
        spawningState = new SpawningState();

        // Store original values
        originalMoveSpeed = moveSpeed;
        originalScale = cachedTransform.localScale;

        SetupNavMeshAgent();
        SetupGlowingEyes();
    }

    void SetupNavMeshAgent()
    {
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.areaMask = navMeshAreaMask;
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Reduced for web

            // Optimized settings based on enemy type
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
    }

    void SetupGlowingEyes()
    {
        if (eyeGlowMaterial != null && eyeTransforms.Length > 0)
        {
            foreach (Transform eyeTransform in eyeTransforms)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.transform.parent = eyeTransform;
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
    }

    public void Initialize(WaveManager manager)
    {
        waveManager = manager;
        isDead = false;

        // Reset values
        moveSpeed = originalMoveSpeed;
        cachedTransform.localScale = originalScale;

        // Apply enemy type modifications
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

        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.enabled = false;
        }

        if (healthComponent != null)
        {
            healthComponent.ResetHealth();
        }
        
        // Start with individual spawning state
        stateMachine.ChangeState(spawningState);
    }

    void Update()
    {
        if (!isDead)
        {
            // Check if enemy died
            if (healthComponent != null && healthComponent.gameObject == null)
            {
                stateMachine.ChangeState(deadState);
            }
            else
            {
                stateMachine.Update();
            }
        }
    }

    public IEnemyState GetMovementState()
    {
        // Return individual state instances for this enemy
        return enemyType switch
        {
            EnemyType.Fast => fastState,
            EnemyType.Heavy => heavyState,
            _ => basicState
        };
    }

    public void OnEnemyDied()
    {
        if (isDead) return;

        isDead = true;
        if (waveManager != null)
        {
            waveManager.OnEnemyDestroyed(this);
        }
    }

    public void ResetForPool()
    {
        if (healthComponent != null)
            healthComponent.ResetHealth();

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        cachedTransform.position = Vector3.zero;
        cachedTransform.rotation = Quaternion.identity;
        gameObject.SetActive(false);
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
}