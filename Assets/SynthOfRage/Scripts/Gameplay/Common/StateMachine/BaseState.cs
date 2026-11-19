using SynthOfRage.Scripts.Gameplay.SO.States;

namespace SynthOfRage.Scripts.Gameplay.Common.StateMachine
{
    /// <summary>
    /// A <i>State</i> represent every single action a <see cref="Unit"/> can do.
    /// <c>BaseState</c> inheritors will implement the functionalities of each behavior but are still data-driven through <see cref="StateSO"/>.
    /// </summary>
    public class BaseState
    {
        /// <summary>The data asset driving this state's behavior, assigned during the FSM <see cref="FSM.Initialize"/>.</summary>
        public StateSO StateData { get; set; }

        /// <summary>Called once when this state becomes the <c>ActiveState</c>.</summary>
        /// <param name="fsm">The FSM running this state.</param>
        public virtual void EnterState(FSM fsm) { }

        /// <summary>Called every FSM <c>Update</c> cycle while this state is the <c>ActiveState</c>.</summary>
        /// <param name="fsm">The FSM running this state.</param>
        public virtual void UpdateState(FSM fsm) { }

        /// <summary>Called once when this state stops being the <c>ActiveState</c>.</summary>
        /// <param name="fsm">The FSM running this state.</param>
        public virtual void ExitState(FSM fsm) { }
    }
}
