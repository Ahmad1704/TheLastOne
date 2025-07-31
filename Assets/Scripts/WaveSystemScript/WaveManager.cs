using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int enemyIncreasePerWave = 10;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float arenaRadius = 40f;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private Button stopResumeButton;
    [SerializeField] private Button nextWaveButton;
    [SerializeField] private Button destroyWaveButton;
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
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleWaves();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SpawnNextWave();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            DestroyCurrentWave();
        }
    }
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

    private void UpdateUI()
    {
        waveNumberText.text = $"Wave: {currentWave}";
        enemyCountText.text = $"Enemies: {activeEnemyCount}";
        fpsText.text = $"FPS: {cachedFps}";
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

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            if (!wavesPaused)
            {
                yield return StartCoroutine(SpawnWaveCoroutine());
                yield return new WaitUntil(() => activeEnemyCount == 0);

                if (!wavesPaused)
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
        Vector3 spawnPos = new Vector3(pos2D.x, 0, pos2D.y);

        int typeIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject enemyObj = enemyPool.GetEnemy(typeIndex);

        if (!enemyObj) return;

        enemyObj.transform.position = spawnPos;
        enemyObj.SetActive(true);

        if (enemyObj.TryGetComponent(out Enemy enemy))
        {
            enemy.Initialize(this);
            activeEnemies.Add(enemy);
            activeEnemyCount++;
        }
    }

    public void OnEnemyDestroyed(Enemy enemy)
    {
        if (!activeEnemies.Remove(enemy)) return;

        activeEnemyCount--;
        enemy.ResetForPool();
        enemyPool.ReturnEnemy(enemy.gameObject);
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
        if (waitingForNextWave || wavesPaused)
        {
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
            }

            currentWave++;
            waveCoroutine = StartCoroutine(WaveLoop());
        }
    }
    public void DestroyCurrentWave()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Health h = activeEnemies[i].GetComponent<Health>();
            if (h != null)
                h.TakeDamage(h.GetCurrentHealth());
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, arenaRadius);
    }
}
