using System;
using SynthOfRage.Scripts.Utilities.Attributes;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Common.StateMachine
{
    /// <summary>
    /// A generic base FSM that run its logic itself
    /// </summary>

    [Serializable]
    public abstract class FSM
    {
        /// <summary>Was the FSM properly initialized before running ?</summary>
        /// <seealso cref="Initialize"/>
        [ReadOnly("Visual debug only")]
        [SerializeField] private bool _isInitialized = false;

        /// <summary> The current state that the FSM should run.</summary>
        protected BaseState ActiveState;
        /// <summary>The immediate previous state that was run before the current<c>ActiveState</c>.</summary>
        protected BaseState PreviousState;
        /// <summary>The next state to be active in the next <c>Update</c> cycle, should there be one.</summary>
        protected BaseState QueuedState;

        /// <summary>The object the FSM is attached to and will run from.</summary>
        protected MonoBehaviour ObjectContext;

        /// <summary>Is the state machine currently switching state (from <c>ActiveState</c> to <c>QueuedState</c>) ?</summary>
        /// <seealso cref="TransitionState"/>
        protected bool IsTransitioning;

        /// <summary>The "<i>default</i>" constructor of a FSM.</summary>
        /// <param name="ctx">Only require the <c>MonoBehaviour</c> on which FSM will run from.</param>
        protected FSM(MonoBehaviour ctx)
        {
            if (!ctx)
                throw new NullReferenceException($"[{this.GetType().Name}] MonoBehavior context provided on construction is null or not set");

            ObjectContext = ctx;
        }

        /// <summary>Initialize the FSM before running with its context and other preparations.<br/>⚠️ <b>Should be run from the attached <c>MonoBehavior</c> !</b></summary>
        public abstract void Initialize();

        /// <summary>Start the FSM by executing the first state.<br/>⚠️ <b>Should be run from the attached <c>MonoBehavior</c> !</b></summary>
        /// <param name="firstStateOverride">Enable to override (or set) the first <c>ActiveState</c> that will run.</param>
        /// <exception cref="Exception">Checks if the FSM was properly initialized before running. Raise an exception otherwise.</exception>
        protected virtual void Start(BaseState firstStateOverride = null)
        {
            if (!_isInitialized) throw new Exception($"[{this.GetType().Name}] FSM object is not properly initialized before running.");

            if (firstStateOverride != null) ActiveState = firstStateOverride;

            ActiveState.EnterState(this);
        }

        /// <summary>The continuous loop the FSM will run.<br/>⚠️ <b>Should be run from the attached <c>MonoBehavior</c> !</b></summary>
        public abstract void Update();

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
        /// If a state is queued and pending to be active, this method will fully transition between the <c>ActiveState</c> and the next one, if possible (i.e. not already transitioning).
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
