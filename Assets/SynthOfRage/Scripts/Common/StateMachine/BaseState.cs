namespace SynthOfRage.Scripts.Common.StateMachine
{
    public abstract class BaseState
    {
        public abstract void EnterState(FSM reference);
        public abstract void UpdateState(FSM reference);
        public abstract void ExitState(FSM reference);
    }
}
