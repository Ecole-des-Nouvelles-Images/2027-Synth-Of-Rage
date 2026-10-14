using System;
using System.Collections.Generic;
using SynthOfRage.Scripts.Gameplay.Unit.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Dev.Christopher.Scripts.InputBlock
{
    public class CommandBlock : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        
        private Stack<ICommand> _commands = new Stack<ICommand>();
        private Vector2 _moveVector;
        private Vector2 _moveInput;
        private int _comboCounter;

        private void OnEnable()
        {
            GetComponent<PlayerObserver>().OnPlayerAtkL += AtkLCommandGenerator;
        }
        private void AtkLCommandGenerator()
        {
            if(_animator)
                _commands.Push(new AtkLCommand(_comboCounter, _animator));
        }
        
        
        public void ConsumeCommand()
        {
            _commands.Pop().Execute();
            _commands.Clear();
        }
        public ICommand ReadCommand() 
        {
            return _commands.Peek();
        }
        public void IncreaseComboCounter() 
        {
            _comboCounter++;
        }
        public void ResetComboCounter() {
            _comboCounter = 0;
        }
    }
}