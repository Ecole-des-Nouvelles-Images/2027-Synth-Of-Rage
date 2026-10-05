using System;
using SynthOfRage.Scripts.Common.StateMachine;
using SynthOfRage.Scripts.Unit.Player.States;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Unit.Player
{
    public class AvatarStateCollection: StateCollection
    {
        public readonly AvatarStateIdle Idle = new();
        public readonly AvatarStateMove Move = new();
        public readonly AvatarStateJump Jump = new();
        public readonly AvatarStateAttackLight AttackLight = new();
        public readonly AvatarStateAttackHeavy AttackHeavy = new();
        public readonly AvatarStateDash Dash = new();
        public readonly AvatarStateGuard Guard = new();
    }

    public class AvatarFSM: FSM
    {
        [SerializeField] private float _updateCycleDuration = Time.deltaTime;

        public AvatarStateCollection States { get; } = new ();

        public Avatar Avatar { get; private set; }

        private float _timer = 0f;

        public override void Initialize(GameObject context)
        {
            base.Initialize(context);

            if (_updateCycleDuration <= 0) _updateCycleDuration = Time.deltaTime;

            if (!(Avatar = ObjectContext.GetComponent<Avatar>()))
                throw new NullReferenceException("[AvatarFSM] Can't <Avatar> context doesn't exist or can't be get properly.");

            Avatar.OnMove += OnMoveCallback;
            Avatar.OnJump += OnJumpCallback;
            Avatar.OnDash += OnDashCallback;
            Avatar.OnGuard += OnGuardCallback;
            Avatar.OnAttackLight += OnAttackLightCallback;
            Avatar.OnAttackHeavy += OnAttackHeavyCallback;
            Avatar.OnSpecial += OnSpecialCallback;

            ActiveState = States.Idle;
        }

        protected override void Start()
        {
            ActiveState.EnterState(this);
        }

        protected override void Update()
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

        private void OnDestroy()
        {
            Avatar.OnMove -= OnMoveCallback;
            Avatar.OnJump -= OnJumpCallback;
            Avatar.OnDash -= OnDashCallback;
            Avatar.OnGuard -= OnGuardCallback;
            Avatar.OnAttackLight -= OnAttackLightCallback;
            Avatar.OnAttackHeavy -= OnAttackHeavyCallback;
            Avatar.OnSpecial -= OnSpecialCallback;
        }

        #region Event Callbacks

        private void OnMoveCallback(InputAction.CallbackContext ctx) => QueueNextState(States.Move);
        private void OnJumpCallback(InputAction.CallbackContext ctx) => QueueNextState(States.Jump);
        private void OnDashCallback(InputAction.CallbackContext ctx) => QueueNextState(States.Dash);
        private void OnGuardCallback(InputAction.CallbackContext ctx) => QueueNextState(States.Guard);
        private void OnAttackLightCallback(InputAction.CallbackContext ctx) => QueueNextState(States.AttackLight);
        private void OnAttackHeavyCallback(InputAction.CallbackContext ctx) => QueueNextState(States.AttackHeavy);

        private void OnSpecialCallback(InputAction.CallbackContext ctx) => throw new NotImplementedException("[AvatarFSM] Event OnSpecial callback is not implemented");

        #endregion
    }
}
