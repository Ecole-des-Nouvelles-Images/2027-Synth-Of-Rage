using SynthOfRage.Scripts.Utilities;

namespace SynthOfRage.Scripts.Gameplay.Managers
{
    /// <summary>
    /// This object sole role is to maintain 60 FPS for all the underlying gameplay logic and keep track of the passing frames for advanced timing management in animations.
    /// TODO: The frame counter should be reset when the player enters a new state
    /// </summary>
    public class FrameClock: SingletonMonoBehaviour<FrameClock>
    {

    }
}
