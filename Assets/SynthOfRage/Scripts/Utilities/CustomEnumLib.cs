namespace SynthOfRage.Scripts.Utilities
{
    public static class CustomEnumLib
    {
        public enum Priority
        {
            None,
            Minor,
            Intermediate,
            Major
        }

        public enum CombatState
        {
            None,
            LightAttack,
            HeavyAttack
        }

        public enum AnimationState
        {
            None,
            Start,
            Active,
            Recover,
            End
        }
    }
}