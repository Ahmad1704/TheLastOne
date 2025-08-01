using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    #region Inspector Variables

    [Header("Wave Settings")]
    [SerializeField] private int enemyIncreasePerWave = 10;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float arenaRadius = 40f;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private TextMeshProUGUI waveNumberTextB;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI enemyCountTextB;
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private TextMeshProUGUI fpsTextB;
    [SerializeField] private Button stopResumeButton;
    [SerializeField] private Button nextWaveButton;
    [SerializeField] private Button destroyWaveButton;

    #endregion

    #region Private Variables

    private TextMeshProUGUI stopResumeButtonText;
    private TextMeshProUGUI nextWaveButtonText;
    private TextMeshProUGUI destroyWaveButtonText;

    private int currentWave = 1;
    private int activeEnemyCount = 0;
    private bool wavesPaused = false;
    private bool waitingForNextWave = false;
    private EnemyPool enemyPool;
    private readonly List<Enemy> activeEnemies = new List<Enemy>();
    private Coroutine waveCoroutine;

    // FPS calculation
    private float deltaTime;
    private float lastFpsUpdateTime;
    private const float fpsUpdateInterval = 0.5f;
    private int cachedFps = 60;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeEnemyPool();
        CacheUIButtonTexts();
        SetupButtonListeners();
    }

    private void Start()
    {
        waveCoroutine = StartCoroutine(WaveLoop());
    }

    private void Update()
    {
        UpdateUI();
        UpdateFPS();
        HandleKeyboardInput();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, arenaRadius);
    }

    #endregion

    #region Initialization Methods

    private void InitializeEnemyPool()
    {
        enemyPool = GetComponent<EnemyPool>() ?? gameObject.AddComponent<EnemyPool>();
        enemyPool.Initialize(enemyPrefabs);
    }

    private void CacheUIButtonTexts()
    {
        stopResumeButtonText = stopResumeButton.GetComponentInChildren<TextMeshProUGUI>();
        nextWaveButtonText = nextWaveButton.GetComponentInChildren<TextMeshProUGUI>();
        destroyWaveButtonText = destroyWaveButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void SetupButtonListeners()
    {
        stopResumeButton.onClick.AddListener(ToggleWaves);
        nextWaveButton.onClick.AddListener(SpawnNextWave);
        destroyWaveButton.onClick.AddListener(DestroyCurrentWave);
    }

    #endregion

    #region UI and FPS Updates

    private void UpdateUI()
    {
        waveNumberText.text = $"Wave: {currentWave}";
        waveNumberTextB.text = $"Wave: {currentWave}";
        enemyCountText.text = $"Enemies: {activeEnemyCount}";
        enemyCountTextB.text = $"Enemies: {activeEnemyCount}";
        fpsText.text = $"FPS: {cachedFps}";
        fpsTextB.text = $"FPS: {cachedFps}";
        stopResumeButtonText.text = wavesPaused ? "Resume Waves" : "Stop Waves";
        nextWaveButtonText.text = waitingForNextWave ? "Waiting..." : "Next Wave";
        destroyWaveButtonText.text = activeEnemyCount > 0 ? "Destroy Wave" : "No Enemies";
    }

    private void UpdateFPS()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        if (Time.time - lastFpsUpdateTime >= fpsUpdateInterval)
        {
            cachedFps = Mathf.CeilToInt(1.0f / deltaTime);
            lastFpsUpdateTime = Time.time;
        }
    }

    #endregion

    #region Input Handling

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleWaves();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnNextWave();
        if (Input.GetKeyDown(KeyCode.Alpha3)) DestroyCurrentWave();
    }

    #endregion

    #region Wave Logic

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            if (!wavesPaused)
            {
                yield return StartCoroutine(SpawnWaveCoroutine());

                if (!waitingForNextWave)
                {
                    yield return new WaitUntil(() => activeEnemyCount == 0);
                }

                if (!wavesPaused && !waitingForNextWave)
                {
                    waitingForNextWave = true;
                    yield return new WaitForSeconds(timeBetweenWaves);
                    waitingForNextWave = false;
                    currentWave++;
                }
            }

            yield return null;
        }
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        int totalEnemies = CalculateEnemiesForWave(currentWave);
        int batchSize = 5;

        for (int spawned = 0; spawned < totalEnemies;)
        {
            int currentBatch = Mathf.Min(batchSize, totalEnemies - spawned);

            for (int i = 0; i < currentBatch; i++)
            {
                SpawnRandomEnemy();
                spawned++;
            }

            yield return null;
        }
    }

    private int CalculateEnemiesForWave(int wave)
    {
        return wave switch
        {
            1 => 30,
            2 => 50,
            3 => 70,
            _ => 70 + (wave - 3) * enemyIncreasePerWave
        };
    }

    private void SpawnRandomEnemy()
    {
        Vector2 pos2D = Random.insideUnitCircle.normalized * arenaRadius;

        int typeIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject enemyObj = enemyPool.GetEnemy(typeIndex);

        if (!enemyObj) return;

        float yOffset = CalculateYOffset(enemyObj);
        Vector3 spawnPos = new Vector3(pos2D.x, yOffset, pos2D.y);

        enemyObj.transform.position = spawnPos;
        enemyObj.SetActive(true);

        if (enemyObj.TryGetComponent(out Enemy enemy))
        {
            enemy.Initialize(this);
            activeEnemies.Add(enemy);
            activeEnemyCount++;
        }
    }

    private float CalculateYOffset(GameObject enemyObj)
    {
        Renderer renderer = enemyObj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            float bottomOffset = renderer.bounds.center.y - renderer.bounds.min.y;
            return bottomOffset;
        }

        return 1f;
    }

    #endregion

    #region Public Methods

    public void OnEnemyDestroyed(Enemy enemy)
    {
        if (!activeEnemies.Remove(enemy)) return;
        activeEnemyCount--;
    }

    public void OnEnemyReadyForPool(Enemy enemy, GameObject enemyGameObject)
    {
        enemyPool.ReturnEnemy(enemyGameObject);
    }

    public void ToggleWaves()
    {
        wavesPaused = !wavesPaused;

        if (!wavesPaused && waveCoroutine == null)
        {
            waveCoroutine = StartCoroutine(WaveLoop());
        }
    }

    public void SpawnNextWave()
    {
        if (waveCoroutine != null) StopCoroutine(waveCoroutine);

        currentWave++;
        waitingForNextWave = false;
        wavesPaused = false;
        waveCoroutine = StartCoroutine(WaveLoop());
    }

    public void DestroyCurrentWave()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Health h = activeEnemies[i].GetComponent<Health>();
            if (h != null) h.TakeDamage(h.GetCurrentHealth());
        }
    }

    #endregion
}
