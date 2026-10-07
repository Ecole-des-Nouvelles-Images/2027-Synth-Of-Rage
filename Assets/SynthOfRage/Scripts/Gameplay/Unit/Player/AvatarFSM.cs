using System;
using SynthOfRage.Scripts.Common.StateMachine;
using SynthOfRage.Scripts.Gameplay.Unit.Player.States;
using SynthOfRage.Scripts.Utilities.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
{
    [Serializable]
    public class AvatarStateCollection
    {
        [SerializeField] public AvatarStateIdle Idle = new();
        [SerializeField] public AvatarStateMove Move = new();
        [SerializeField] public AvatarStateJump Jump = new();
        [SerializeField] public AvatarStateAttackLight AttackLight = new();
        [SerializeField] public AvatarStateAttackHeavy AttackHeavy = new();
        [SerializeField] public AvatarStateDash Dash = new();
        [SerializeField] public AvatarStateGuard Guard = new();
    }

    [Serializable]
    public class AvatarFSM: FSM
    {
        [SerializeField] private AvatarStateCollection _states;

        public AvatarStateCollection States => _states;
        public Avatar Avatar { get; private set; }

        /// <summary>
        /// The current movement input direction, normalized.
        /// </summary>
        public Vector2 CurrentMoveInput { get; private set; }

        /// <summary>
        /// Whether the guard action is currently active.
        /// </summary>
        public bool IsGuarding { get; private set; }

        // Movement state shared between all states
        public float YVelocity { get; set; }
        public bool IsJumping { get; set; }
        public bool IsDashing { get; set; }
        public float DashCooldownTimer { get; set; }

        [Header("Debug")]
        [SerializeField] private bool _debugEnabled;

        [ImplicitValue(0, "defaults to Time.deltaTime")] [Min(0f)]
        [SerializeField] private float _updateCycleDuration = 0;
        private float _timer = 0f;

        /// <inheritdoc cref="SynthOfRage.Scripts.Common.StateMachine.FSM(MonoBehaviour)"/>
        public AvatarFSM(MonoBehaviour ctx) : base(ctx)
        {
            Avatar = ctx as Avatar;
        }

        ~AvatarFSM()
        {
            if (Avatar != null)
            {
                Avatar.OnMove -= OnMoveCallback;
                Avatar.OnJump -= OnJumpCallback;
                Avatar.OnDash -= OnDashCallback;
                Avatar.OnGuard -= OnGuardCallback;
                Avatar.OnAttackLight -= OnAttackLightCallback;
                Avatar.OnAttackHeavy -= OnAttackHeavyCallback;
                Avatar.OnSpecial -= OnSpecialCallback;
            }
        }

        public override void Initialize()
        {
            if (_states == null)
                _states = new AvatarStateCollection();

            if (_updateCycleDuration <= 0) _updateCycleDuration = Time.deltaTime;

            if (!Avatar)
                throw new NullReferenceException($"[AvatarFSM] MonoBehavior context is expected to be of type <Avatar> but is actually {(!ObjectContext ? "null" : ObjectContext.GetType().Name)}");

            Avatar.OnMove += OnMoveCallback;
            Avatar.OnJump += OnJumpCallback;
            Avatar.OnDash += OnDashCallback;
            Avatar.OnGuard += OnGuardCallback;
            Avatar.OnAttackLight += OnAttackLightCallback;
            Avatar.OnAttackHeavy += OnAttackHeavyCallback;
            Avatar.OnSpecial += OnSpecialCallback;

            YVelocity = 0f;
            IsJumping = false;
            IsDashing = false;
            DashCooldownTimer = 0f;

            ActiveState = States.Idle;
            ActiveState.EnterState(this);
        }

        public override void Update()
        {
            if (_timer < _updateCycleDuration)
            {
                _timer += Time.deltaTime;
                return;
            }

            if ((QueuedState != null && QueuedState != ActiveState) && !IsTransitioning)
            {
                TransitionState();
            }
            else if (!IsTransitioning)
            {
                ActiveState.UpdateState(this);
            }
        }

        protected override void Start(BaseState firstStateOverride = null)
        {
            base.Start(firstStateOverride);

            // Start with Idle state by default
            if (ActiveState == null)
            {
                if (States != null && States.Idle != null)
                {
                    ActiveState = States.Idle;
                    ActiveState.EnterState(this);
                }
            }
        }

        #region Event Callbacks

        private void OnMoveCallback(InputAction.CallbackContext ctx)
        {
            Vector2 input = ctx.ReadValue<Vector2>();
            CurrentMoveInput = input;

            // Only queue move state if we have significant input
            if (input.sqrMagnitude > 0.01f)
            {
                QueueNextState(States.Move);
            }
            else
            {
                // If input stops, go to idle
                QueueNextState(States.Idle);

            }
        }

        private void OnJumpCallback(InputAction.CallbackContext ctx)
        {
            QueueNextState(States.Jump);
        }

        private void OnDashCallback(InputAction.CallbackContext ctx)
        {
            QueueNextState(States.Dash);
        }

        private void OnGuardCallback(InputAction.CallbackContext ctx)
        {
            IsGuarding = ctx.performed;

            if (ctx.performed)
            {
                QueueNextState(States.Guard);
            }
            else
            {
                // When guard is released, go back to idle or move based on input
                if (CurrentMoveInput.sqrMagnitude > 0.01f)
                {
                    QueueNextState(States.Move);
                }
                else
                {
                    QueueNextState(States.Idle);
                }
            }
        }

        private void OnAttackLightCallback(InputAction.CallbackContext ctx)
        {
            QueueNextState(States.AttackLight);
        }

        private void OnAttackHeavyCallback(InputAction.CallbackContext ctx)
        {
            QueueNextState(States.AttackHeavy);
        }

        private void OnSpecialCallback(InputAction.CallbackContext ctx) => throw new NotImplementedException("[AvatarFSM] Event OnSpecial callback is not implemented");

        #endregion
    }
}
