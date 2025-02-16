public class PlayerNode : Node
{
    public PlayerBehaviour playerBehaviour;
    public bool IsAlive
    {
        get => playerBehaviour.isAlive.Value;
    }
}
