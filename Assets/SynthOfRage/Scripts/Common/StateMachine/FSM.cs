using UnityEngine;

namespace SynthOfRage.Scripts.Common.StateMachine
{
    /// <summary>
    /// A generic base FSM that run its logic itself
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class FSM : MonoBehaviour
    {
        protected BaseState ActiveState;
        protected BaseState PreviousState;
        protected BaseState QueuedState;

        protected GameObject ObjectContext;

        protected bool IsTransitioning;

        /// <summary>
        /// Initialize the FSM before running with its context and other preparations.
        /// </summary>
        /// <param name="context">The <c>GameObject</c> on which the FSM is attached and serve as context.</param>
        public virtual void Initialize(GameObject context) => ObjectContext = context;
        protected abstract void Start();
        protected abstract void Update();

        /// <summary>
        /// External dedicated method to tell the FSM a next state to transition to.
        /// </summary>
        /// <param name="nextState">The state to enqueue.</param>
        public void QueueNextState(BaseState nextState)
        {
            if (nextState != null)
                QueuedState = nextState;
        }

        /// <summary>
        /// If a state is queued and pending to be active, the FSM will fully transition between the <c>_activeState</c> and the next one, if possible (i.e. not already transitioning).
        /// </summary>
        protected void TransitionState()
        {
            if (QueuedState == null || IsTransitioning) return;

            IsTransitioning = true;
            ActiveState.ExitState(this);
            PreviousState = ActiveState;
            ActiveState = QueuedState;
            QueuedState = null;
            ActiveState.EnterState(this);
            IsTransitioning = false;
        }
    }
}
