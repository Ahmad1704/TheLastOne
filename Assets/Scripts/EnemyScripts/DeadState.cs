using UnityEngine;

public class DeadState : IEnemyState
{
    public void Enter(Enemy enemy)
    {
        // Stop NavMesh movement immediately
        if (enemy.NavAgent != null && enemy.NavAgent.enabled)
        {
            enemy.NavAgent.isStopped = true;
            enemy.NavAgent.velocity = Vector3.zero;
        }

        // Enable root motion for death animation and trigger it
        if (enemy.Animator != null)
        {
            // Enable root motion for the death animation
            enemy.Animator.applyRootMotion = true;
            
            // Trigger death animation
            enemy.Animator.SetTrigger("Die");
        }

        // Call the death handler
        enemy.OnEnemyDied();
    }

    public void Update(Enemy enemy)
    {
        // Dead enemies don't update, but we can add debug info if needed
    }

    public void Exit(Enemy enemy)
    {
        if (enemy.Animator != null)
        {
            // Clean up animation state
            enemy.Animator.ResetTrigger("Die");
            
            // Disable root motion when exiting death state
            enemy.Animator.applyRootMotion = false;
        }
    }
}