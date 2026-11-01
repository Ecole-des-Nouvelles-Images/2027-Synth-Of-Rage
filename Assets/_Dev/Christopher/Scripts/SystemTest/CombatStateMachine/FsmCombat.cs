using System;
using System.Runtime.CompilerServices;
using _Dev.Christopher.Scripts.InputBlock;
using _Dev.Christopher.Scripts.SystemTest.InputBlock;
using UnityEngine;
using Avatar = SynthOfRage.Scripts.Gameplay.Unit.Player.Avatar;

namespace _Dev.Christopher.Scripts.SystemTest.CombatStateMachine
{
    public class FsmCombat : MonoBehaviour
    {
        //ALL STATE LIST
        
        //
        [SerializeField] private Avatar _avatar;
        [SerializeField] private CommandBlock _commandBlock;
        //[SerializeField] private AnimationEventLoopAntenna _animationEventLoopAntenna;

        public CombatStateOther OtherState { get; private set; } 
        public CombatStateAttack AttackState { get; private set; }
        
        private CombatState _currentState;

        private void OnEnable()
        {
            OtherState =  new CombatStateOther(GetComponent<FsmCombat>(), _avatar);
            AttackState = new CombatStateAttack(GetComponent<FsmCombat>(), _avatar);
        }

        private void Start()
        {
            _currentState = OtherState;
            _currentState.OnStart();
        }

        private void Update()
        {
            _currentState.OnUpdate();
        }

        public void ChangeState(CombatState newState)
        {
            _currentState = newState;
            _currentState.OnStart();
        }

        public ICommand ReadLastCommand()
        {
            return _commandBlock.ReadCommand();
        }
    }
}