using UnityEngine;
public class FastZigzagState : IEnemyState
{
    private float zigzagTimer;
    private float zigzagInterval = 1.2f; 
    private Vector3 currentTarget;
    private float destinationRadius = 12f; 

    public void Enter(Enemy enemy)
    {
        zigzagTimer = 0f;
        var navAgent = enemy.NavAgent;
        if (navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.speed = enemy.MoveSpeed;
            navAgent.acceleration = 8f; 
            navAgent.angularSpeed = 270f; 
        }
        SetNewZigzagTarget(enemy);
    }

    public void Update(Enemy enemy)
    {
        var navAgent = enemy.NavAgent;
        if (navAgent == null || !navAgent.enabled) return;

        zigzagTimer += Time.deltaTime;

        if (zigzagTimer >= zigzagInterval ||
            (!navAgent.pathPending && navAgent.remainingDistance < 3f)) 
        {
            SetNewZigzagTarget(enemy);
            zigzagTimer = 0f;
        }
    }

    private void SetNewZigzagTarget(Enemy enemy)
    {
        var enemyTransform = enemy.CachedTransform;
        Vector3 toCenter = -enemyTransform.position.normalized; 
        Vector3 perpendicular = new Vector3(-toCenter.z, 0, toCenter.x); 

        float zigzagStrength = Random.Range(-0.5f, 0.5f);
        Vector3 targetDirection = (toCenter + perpendicular * zigzagStrength).normalized;

        currentTarget = enemyTransform.position + targetDirection * destinationRadius;
        currentTarget.y = 0;

        enemy.NavAgent.SetDestination(currentTarget);
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
