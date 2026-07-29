namespace DodgeFromLight.Application.Game
{
    /// <summary>
    /// Describes the UI-facing lifecycle of one puzzle attempt.
    /// </summary>
    public enum GameSessionStatus
    {
        Running = 0,
        Won = 1,
        Lost = 2
    }
}
