public interface IPlayerInputHandler
{
    void Register(PlayerInputActions inputActions);
    void Unregister(PlayerInputActions inputActions);
}
