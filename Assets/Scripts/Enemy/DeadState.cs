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

        // Trigger death animation
        if (enemy.Animator != null)
        {
            // Check if the animator has the "Die" trigger
            foreach (var param in enemy.Animator.parameters)
            {
                if (param.name == "Die" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    enemy.Animator.applyRootMotion = true;
                    enemy.Animator.SetTrigger("Die");
                    break;
                }
            }
            
            // If no death parameters found, log it
            bool hasDeathParam = false;
            foreach (var param in enemy.Animator.parameters)
            {
                if (param.name == "Die" || param.name == "IsDead")
                {
                    hasDeathParam = true;
                    break;
                }
            }
            
            if (!hasDeathParam)
            {
               
                foreach (var param in enemy.Animator.parameters)
                {
                    Debug.Log($"- {param.name} ({param.type})");
                }
            }
        }
        else
        {
            Debug.LogWarning("No Animator found on enemy: " + enemy.name);
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
        Debug.Log("Exiting dead state for: " + enemy.name);

        if (enemy.Animator != null)
        {
            enemy.Animator.ResetTrigger("Die");      
            enemy.Animator.applyRootMotion = false; 
        }
    }
}