
public class EnemyStateMachine
{
    private IEnemyState currentState;
    private Enemy enemy;

    public EnemyStateMachine(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit(enemy);
        currentState = newState;
        currentState?.Enter(enemy);
    }

    public void Update()
    {
        currentState?.Update(enemy);
    }

    public IEnemyState GetCurrentState()
    {
        return currentState;
    }
}