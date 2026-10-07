using System;

namespace SynthOfRage.Scripts.Common.StateMachine
{
    /// <summary>
    /// Base class for FSM states. Made serializable to support inspector configuration.
    /// </summary>
    [Serializable]
    public abstract class BaseState
    {
        public abstract void EnterState(FSM reference);
        public abstract void UpdateState(FSM reference);
        public abstract void ExitState(FSM reference);
    }
}
