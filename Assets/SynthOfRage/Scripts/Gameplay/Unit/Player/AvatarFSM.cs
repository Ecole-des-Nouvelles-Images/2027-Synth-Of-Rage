using System;
using UnityEngine;
using UnityEngine.InputSystem;

using SynthOfRage.Scripts.Gameplay.Common.StateMachine;
using SynthOfRage.Scripts.Gameplay.Unit.Player.States;
using SynthOfRage.Scripts.Utilities.Attributes;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
{
    [Serializable]
    public class AvatarStateCollection
    {
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

        [ImplicitValue(0, "defaults to Time.deltaTime")] [Min(0f)]
        [SerializeField] private float _updateCycleDuration = 0;
        private float _timer = 0f;

        /// <inheritdoc cref="FSM(MonoBehaviour)"/>
        public AvatarFSM(MonoBehaviour ctx) : base(ctx)
        {
            Avatar = ctx as Avatar;
        }

        ~AvatarFSM()
        {
            // TODO: FSM construction
        }

        public override void Initialize()
        {
            // TODO: FSM initialization
        }

        public override void Update()
        {
            // TODO: how the FSM update

            /*if (_timer < _updateCycleDuration)
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
            }*/
        }

        protected override void Start(BaseState firstStateOverride = null)
        {
            base.Start(firstStateOverride);

            // TODO: how the FSM start ?
        }

        #region Event Callbacks

        #endregion
    }
}
