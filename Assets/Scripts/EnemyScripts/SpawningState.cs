using UnityEngine;
using UnityEngine.AI;


public class SpawningState : IEnemyState
{
    private float spawnDuration = 1f;
    private float spawnTimer;

    public void Enter(Enemy enemy)
    {
        spawnTimer = 0f;
        enemy.transform.localScale = Vector3.zero;

        
        if (enemy.NavAgent != null && enemy.NavAgent.enabled && enemy.NavAgent.isOnNavMesh)
        {
            enemy.NavAgent.isStopped = true;
            enemy.NavAgent.ResetPath();
        }
    }

    public void Update(Enemy enemy)
    {
        spawnTimer += Time.deltaTime;
        float progress = spawnTimer / spawnDuration;

        // Smooth scale up
        enemy.transform.localScale = Vector3.Lerp(Vector3.zero, enemy.OriginalScale, progress);

        if (spawnTimer >= spawnDuration)
        {
            // Ensure NavMeshAgent is properly set up before transitioning
            if (enemy.NavAgent != null)
            {
                if (!enemy.NavAgent.enabled)
                {
                    enemy.NavAgent.enabled = true;
                }

                // Only proceed if agent is on NavMesh
                if (enemy.NavAgent.isOnNavMesh)
                {
                    enemy.NavAgent.speed = enemy.MoveSpeed;
                    enemy.StateMachine.ChangeState(enemy.GetMovementState());
                }
                else
                {
                    // Try to place on NavMesh
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(enemy.transform.position, out hit, 10f, NavMesh.AllAreas))
                    {
                        enemy.NavAgent.Warp(hit.position);
                        enemy.NavAgent.speed = enemy.MoveSpeed;
                        enemy.StateMachine.ChangeState(enemy.GetMovementState());
                    }
                    else
                    {
                        Debug.LogWarning($"Could not place {enemy.gameObject.name} on NavMesh during spawn transition");
                  
                        spawnTimer = spawnDuration - 0.1f;
                    }
                }
            }
            else
            {
                // No NavMeshAgent, just transition
                enemy.StateMachine.ChangeState(enemy.GetMovementState());
            }
        }
    }

    public void Exit(Enemy enemy)
    {
        enemy.transform.localScale = enemy.OriginalScale;
    }
}