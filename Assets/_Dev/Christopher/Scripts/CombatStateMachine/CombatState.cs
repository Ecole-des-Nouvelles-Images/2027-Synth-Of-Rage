using SynthOfRage.Scripts.Utilities;

namespace _Dev.Christopher.Scripts.CombatStateMachine
{
    public abstract class CombatState
    {
        public abstract void OnStart();
        public abstract void OnUpdate();
        public abstract void OnEnd();
        public abstract CustomEnumLib.CombatState NextState();
    }
}