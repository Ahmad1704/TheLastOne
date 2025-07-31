using UnityEngine;

public class SpawningState : IEnemyState
{
    private float spawnDuration = 1f;
    private float spawnTimer;

    public void Enter(Enemy enemy)
    {
        spawnTimer = 0f;
        // Scale animation or spawn effect
        enemy.transform.localScale = Vector3.zero;

        // Disable NavMeshAgent during spawn
        if (enemy.NavAgent != null)
        {
            enemy.NavAgent.enabled = false;
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
            // Enable NavMeshAgent and transition to movement
            if (enemy.NavAgent != null)
            {
                enemy.NavAgent.enabled = true;
                enemy.NavAgent.speed = enemy.MoveSpeed;
            }
            enemy.StateMachine.ChangeState(enemy.GetMovementState());
        }
    }

    public void Exit(Enemy enemy)
    {
        enemy.transform.localScale = enemy.OriginalScale;
    }
}