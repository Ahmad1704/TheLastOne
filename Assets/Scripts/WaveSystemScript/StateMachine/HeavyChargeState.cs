using UnityEngine;
public class HeavyChargeState : IEnemyState
{
    private enum ChargePhase { Preparing, Charging, Cooldown }
    private ChargePhase currentPhase = ChargePhase.Preparing;
    private float phaseTimer;
    private float prepareTime = 1.5f; // Reduced from 2f
    private float chargeTime = 1.2f; // Reduced from 1.5f
    private float cooldownTime = 0.8f; // Reduced from 1f
    private Vector3 chargeTarget;

    public void Enter(Enemy enemy)
    {
        currentPhase = ChargePhase.Preparing;
        phaseTimer = 0f;

        var navAgent = enemy.NavAgent;
        if (navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.acceleration = 4f;
        }

        SetChargeTarget(enemy);
    }

    public void Update(Enemy enemy)
    {
        var navAgent = enemy.NavAgent;
        if (navAgent == null || !navAgent.enabled) return;

        phaseTimer += Time.deltaTime;

        switch (currentPhase)
        {
            case ChargePhase.Preparing:
                navAgent.speed = enemy.MoveSpeed * 0.4f; // Slightly faster
                navAgent.acceleration = 4f;

                // Simplified pulsing effect
                float pulseScale = 1f + Mathf.Sin(phaseTimer * 6f) * 0.08f; // Reduced frequency and amplitude
                enemy.CachedTransform.localScale = enemy.OriginalScale * pulseScale;

                // Less frequent target updates
                if (phaseTimer % 0.8f < Time.deltaTime) // Increased from 0.5f
                {
                    SetChargeTarget(enemy);
                }

                if (phaseTimer >= prepareTime)
                {
                    currentPhase = ChargePhase.Charging;
                    phaseTimer = 0f;
                    enemy.CachedTransform.localScale = enemy.OriginalScale;
                }
                break;

            case ChargePhase.Charging:
                navAgent.speed = enemy.MoveSpeed * 2f; // Reduced from 2.5f
                navAgent.acceleration = 15f; // Reduced from 20f

                if (phaseTimer >= chargeTime ||
                    (!navAgent.pathPending && navAgent.remainingDistance < 1.5f)) // Increased threshold
                {
                    currentPhase = ChargePhase.Cooldown;
                    phaseTimer = 0f;
                }
                break;

            case ChargePhase.Cooldown:
                navAgent.isStopped = true;

                if (phaseTimer >= cooldownTime)
                {
                    navAgent.isStopped = false;
                    currentPhase = ChargePhase.Preparing;
                    phaseTimer = 0f;
                    SetChargeTarget(enemy);
                }
                break;
        }
    }

    private void SetChargeTarget(Enemy enemy)
    {
        Vector2 randomOffset = Random.insideUnitCircle * 2f; // Reduced from 3f
        chargeTarget = new Vector3(randomOffset.x, 0, randomOffset.y);
        enemy.NavAgent.SetDestination(chargeTarget);
    }

    public void Exit(Enemy enemy)
    {
        var navAgent = enemy.NavAgent;
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.isStopped = true;
        }
        enemy.CachedTransform.localScale = enemy.OriginalScale;
    }
}