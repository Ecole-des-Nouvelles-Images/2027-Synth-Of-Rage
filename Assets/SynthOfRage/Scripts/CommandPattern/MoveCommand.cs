using SynthOfRage.Scripts.Player;
using UnityEngine;

namespace SynthOfRage.Scripts.CommandPattern
{
    public class MoveCommand : ICommand
    {
        private GameObject _character;
        private float _moveSpeed;
        private Vector2 _input;
        private PlayerMovement _playerMovement;
        
        public MoveCommand(GameObject character, Vector2 input)
        {
            _character = character;
            _input = input;
            if(_character.GetComponent<PlayerMovement>())
                _playerMovement = _character.GetComponent<PlayerMovement>();
            if(_character.GetComponent<Player.Player>())
                _moveSpeed = _character.GetComponent<Player.Player>().MoveSpeed;
        }
        
        public void Execute()
        {
            Vector3 movement = GetMovementDirection(_input);

            movement *= _moveSpeed;

            if(_playerMovement)
                _playerMovement.MoveWithMovementSpaceLimits(movement * Time.deltaTime);
        }
        
        private Vector3 GetMovementDirection(Vector2 input)
        {
            Vector3 right = _character.transform.right;
            Vector3 forward = _character.transform.forward;
            Vector3 direction = right * input.x + forward * input.y;

            direction.y = 0f;

            return Vector3.ClampMagnitude(direction, 1f );
        }
       
    }
}
