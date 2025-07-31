public class DeadState : IEnemyState
{
    public void Enter(Enemy enemy)
    {
        // Stop NavMesh movement
        if (enemy.NavAgent != null && enemy.NavAgent.enabled)
        {
            enemy.NavAgent.isStopped = true;
            enemy.NavAgent.enabled = false;
        }
        enemy.OnEnemyDied();
    }

    public void Update(Enemy enemy)
    {
        // Dead enemies don't update
    }

    public void Exit(Enemy enemy)
    {
        // Reset for pooling
    }
}