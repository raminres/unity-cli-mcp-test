namespace Arcade.Core
{
    /// <summary>
    /// Lifecycle states for arcade games.
    /// </summary>
    public enum GameState
    {
        ReadyToLaunch,
        Playing,
        BallLost,
        LevelClear,
        GameOver,
        Paused
    }
}
