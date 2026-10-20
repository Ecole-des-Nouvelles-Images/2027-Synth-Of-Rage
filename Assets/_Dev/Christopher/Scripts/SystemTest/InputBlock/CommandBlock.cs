using System.Collections.Generic;
using _Dev.Christopher.Scripts.InputBlock;
using SynthOfRage.Scripts.Gameplay.Unit.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Avatar = SynthOfRage.Scripts.Gameplay.Unit.Player.Avatar;

namespace _Dev.Christopher.Scripts.SystemTest.InputBlock
{
    public class CommandBlock : MonoBehaviour
    {
        [SerializeField] private Avatar _avatar;
        
        private Stack<ICommand> _commands = new Stack<ICommand>();
        private Vector2 _moveVector;
        private Vector2 _moveInput;
        private int _comboCounter;

        private void OnEnable()
        {
            _avatar.InputController.OnAttackLightInput += AtkLCommandGenerator;
        }
        private void AtkLCommandGenerator(InputAction.CallbackContext callbackContext)
        {
            Debug.Log("AtkLCommandGenerator");
            _commands.Push(new AtkLCommand(_comboCounter));
        }
        
        
        public void ConsumeCommand(ICommand command)
        {
            command.Execute();
            _commands.Clear();
        }
        public ICommand ReadCommand() 
        {
            if(_commands.Count > 0)
                return _commands.Peek();
            return null;
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