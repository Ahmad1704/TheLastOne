using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    private Dictionary<int, Queue<GameObject>> enemyPools = new Dictionary<int, Queue<GameObject>>();
    private Dictionary<GameObject, int> enemyTypeMap = new Dictionary<GameObject, int>(); // Track enemy types
    private GameObject[] enemyPrefabs;
    private int poolSize = 200;
    
    public void Initialize(GameObject[] prefabs)
    {
        enemyPrefabs = prefabs;
        
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            enemyPools[i] = new Queue<GameObject>();
            
            int enemiesPerType = poolSize / enemyPrefabs.Length;
            for (int j = 0; j < enemiesPerType; j++)
            {
                GameObject enemy = Instantiate(enemyPrefabs[i]);
                enemy.SetActive(false);
                
                // Store the enemy type mapping
                enemyTypeMap[enemy] = i;
                
                enemyPools[i].Enqueue(enemy);
            }
        }
    }
    
    public GameObject GetEnemy(int type)
    {
        // Validate type index
        if (type < 0 || type >= enemyPrefabs.Length)
        {
            Debug.LogError($"Invalid enemy type: {type}. Valid range: 0-{enemyPrefabs.Length - 1}");
            return null;
        }
        
        GameObject enemy;
        
        if (enemyPools[type].Count > 0)
        {
            enemy = enemyPools[type].Dequeue();
        }
        else
        {
            // Create new instance if pool is empty
            enemy = Instantiate(enemyPrefabs[type]);
            enemyTypeMap[enemy] = type; // Track the new enemy's type
        }
        
        return enemy;
    }
    
    public void ReturnEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        enemy.SetActive(false);
        
        // Use the stored type mapping instead of name comparison
        if (enemyTypeMap.ContainsKey(enemy))
        {
            int enemyType = enemyTypeMap[enemy];
            enemyPools[enemyType].Enqueue(enemy);
        }
        else
        {
            // Fallback: try to determine type by comparing prefab names
            // Remove "(Clone)" from the name for comparison
            string enemyName = enemy.name.Replace("(Clone)", "").Trim();
            
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyName == enemyPrefabs[i].name)
                {
                    enemyTypeMap[enemy] = i; // Store for future use
                    enemyPools[i].Enqueue(enemy);
                    return;
                }
            }
            
            // If we can't determine the type, destroy the object
            Debug.LogWarning($"Could not determine type for enemy: {enemy.name}. Destroying object.");
            Destroy(enemy);
        }
    }
    // Clean up the type mapping when objects are destroyed
    void OnDestroy()
    {
        enemyTypeMap.Clear();
    }
}