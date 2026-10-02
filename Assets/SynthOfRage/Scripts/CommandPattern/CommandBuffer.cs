using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using Avatar = SynthOfRage.Scripts.Player.Player;

namespace SynthOfRage.Scripts.CommandPattern
{
    public class CommandBuffer : MonoBehaviour
    {
        

        [SerializeField] private float timeBeforeExecution = 1;
        
        private Stack<ICommand> _commands;
        private float _currentTime = 0;
        
        private void OnEnable()
        {
            Avatar.OnMove  += CreateMoveCommand;
            // Player.OnJump += (ctx) => { };
            // Player.OnDash += (ctx) => { };
            // Player.OnGuard += (ctx) => { };
            // Player.OnAttackLight += (ctx) => { };
            // Player.OnAttackHeavy += (ctx) => { };
            // Player.OnSpecial += (ctx) => { };
            // Player.OnInteract += (ctx) => { };
        }
        
// TODO: Replace lambdas definitions (they can't be unsubscribed that way !)
        private void OnDestroy()
        {
            Avatar.OnMove -= CreateMoveCommand; 
            // Player.OnJump -= (ctx) => { };
            // Player.OnDash -= (ctx) => { };
            // Player.OnGuard -= (ctx) => { };
            // Player.OnAttackLight -= (ctx) => { };
            // Player.OnAttackHeavy -= (ctx) => { };
            // Player.OnSpecial -= (ctx) => { };
            // Player.OnInteract -= (ctx) => { };
        }

        private void CreateMoveCommand(InputAction.CallbackContext  context)
        {
            MoveCommand moveCommand = new MoveCommand(gameObject,context.ReadValue<Vector2>());
            _commands.Push(moveCommand);
            UnityEngine.Debug.Log("Commande créé");
        }

        private void Update()
        {
            if (_currentTime >= timeBeforeExecution)
            {
                if (_commands.Count > 0)
                {
                    _commands.Pop().Execute();
                    _commands.Clear();
                }
                _currentTime = 0;
            }
            _currentTime += Time.deltaTime;
        }
    }
}
