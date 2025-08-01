using UnityEngine;
using System;

public class Health : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    private float currentHealth;
    public event Action<float> OnHealthChanged;
    public event Action OnDeath;
    
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return; 
        
        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    private void Die()
    {
        OnDeath?.Invoke();
        
        // For object pooling, we don't destroy immediately
        // Let the enemy handle the pooling logic
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.OnEnemyDied();
        }
        else
        {
            // Fallback for non-enemy objects
            Destroy(gameObject);
        }
    }
}