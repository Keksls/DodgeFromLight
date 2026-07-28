namespace DodgeFromLight.Core.Events
{
    public enum SimulationEventType
    {
        PlayerMoved = 0,
        PlayerWaited = 1,
        TurnCompleted = 2,
        GameWon = 3,
        EnemyMoved = 4,
        EnemyTurned = 5,
        PlayerDetected = 6,
        GameLost = 7
    }
}
