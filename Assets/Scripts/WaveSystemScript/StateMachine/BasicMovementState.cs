using UnityEngine;
public class BasicMovementState : IEnemyState
{
    private float destinationUpdateRate = 1f; // Increased from 0.5f for web
    private float lastDestinationUpdate;
    private Vector3 cachedTargetPosition;

    public void Enter(Enemy enemy)
    {
        var navAgent = enemy.NavAgent;
        if (navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.speed = enemy.MoveSpeed;
            navAgent.acceleration = 6f; // Reduced from 8f
            navAgent.angularSpeed = 180f; // Reduced from 360f
        }
    }

    public void Update(Enemy enemy)
    {
        var navAgent = enemy.NavAgent;
        if (navAgent == null || !navAgent.enabled) return;

        float currentTime = Time.time;
        if (currentTime - lastDestinationUpdate >= destinationUpdateRate)
        {
            // Cache calculation to avoid repeated operations
            if (cachedTargetPosition == Vector3.zero)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 2f;
                cachedTargetPosition = new Vector3(randomOffset.x, 0, randomOffset.y);
            }

            navAgent.SetDestination(cachedTargetPosition);
            lastDestinationUpdate = currentTime;
            
            // Reset cached position for next update
            cachedTargetPosition = Vector3.zero;
        }
    }

    public void Exit(Enemy enemy)
    {
        var navAgent = enemy.NavAgent;
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.isStopped = true;
        }
    }
}