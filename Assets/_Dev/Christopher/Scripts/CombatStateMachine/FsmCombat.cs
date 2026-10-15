using System;
using _Dev.Christopher.Scripts.InputBlock;
using UnityEngine;

namespace _Dev.Christopher.Scripts.CombatStateMachine
{
    public class FsmCombat : MonoBehaviour
    {
        //ALL STATE LIST
        
        //
        
        [SerializeField]private CommandBlock _commandBlock;
        [SerializeField]private AnimationEventLoopAntenna _animationEventLoopAntenna;
        
        private CombatState _currentState;
        
        private void Start()
        {
            _currentState.OnStart();
        }

        private void Update()
        {
            _currentState.OnUpdate();
        }
    }
}