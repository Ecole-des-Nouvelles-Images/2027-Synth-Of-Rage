using System;
using SynthOfRage.Scripts.Common.StateMachine;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player.States
{
    /// <summary>
    /// Move state for the Avatar. Character moves based on input direction.
    /// </summary>
    [Serializable]
    public class AvatarStateMove : BaseState
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveAcceleration = 10f;
        [SerializeField] private float _maxSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 15f;

        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");

        private Vector2 _currentInput;

        public float MoveAcceleration => _moveAcceleration;
        public float MaxSpeed => _maxSpeed;
        public float RotationSpeed => _rotationSpeed;

        public override void EnterState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            // Store the current input that triggered this state
            var animator = fsm.Avatar.Animator;
            if (animator != null)
            {
                animator.SetBool(Move, true);
            }

            UnityEngine.Debug.Log("[AvatarStateMove] Entered Move state");
        }

        public override void UpdateState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            // Process movement based on current input
            ProcessMovement(fsm);

            // Process gravity
            ProcessGravity(fsm);

            // Update character rotation
            // UpdateCharacterRotation(fsm);

            // If movement input stops, transition back to idle
            if (fsm.CurrentMoveInput.sqrMagnitude < 0.01f)
            {
                fsm.QueueNextState(fsm.States.Idle);
            }
        }

        public override void ExitState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            var animator = fsm.Avatar.Animator;
            if (animator != null)
            {
                animator.SetBool(Move, false);
            }

            UnityEngine.Debug.Log("[AvatarStateMove] Exited Move state");
        }

        private void ProcessMovement(AvatarFSM fsm)
        {
            var characterController = fsm.Avatar.CharacterController;
            var movementSpace = fsm.Avatar.CurrentMovementSpace;

            if (characterController == null || movementSpace == null) return;

            if (fsm.CurrentMoveInput.sqrMagnitude > 0.01f)
            {
                // Calculate movement direction relative to movement space
                Vector3 forward = movementSpace.transform.forward;
                Vector3 right = movementSpace.transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                Vector3 movementDirection = (forward * fsm.CurrentMoveInput.y + right * fsm.CurrentMoveInput.x).normalized;

                // Apply movement with acceleration
                float effectiveSpeed = Mathf.Lerp(0, _maxSpeed, _moveAcceleration * Time.deltaTime);
                Vector3 movement = movementDirection * effectiveSpeed * Time.deltaTime;

                characterController.Move(movement);

                // Update animator speed parameter
                var animator = fsm.Avatar.Animator;
                if (animator != null)
                {
                    animator.SetFloat(MoveSpeed, fsm.CurrentMoveInput.magnitude);
                }
            }
        }

        private void ProcessGravity(AvatarFSM fsm)
        {
            var characterController = fsm.Avatar.CharacterController;
            if (characterController == null) return;

            if (characterController.isGrounded && fsm.YVelocity < 0)
            {
                fsm.YVelocity = -2f; // Small downward force to ensure grounding
            }
            else
            {
                fsm.YVelocity += fsm.Avatar.Gravity * Time.deltaTime;
            }

            // Apply gravity movement
            Vector3 gravityMovement = Vector3.up * fsm.YVelocity * Time.deltaTime;
            characterController.Move(gravityMovement);
        }

        private void UpdateCharacterRotation(AvatarFSM fsm)
        {
            if (fsm.CurrentMoveInput.sqrMagnitude > 0.01f && fsm.Avatar != null)
            {
                var movementSpace = fsm.Avatar.CurrentMovementSpace;
                if (movementSpace != null)
                {
                    // Calculate target rotation based on movement space
                    Vector3 forward = movementSpace.transform.forward;
                    Vector3 right = movementSpace.transform.right;
                    forward.y = 0;
                    right.y = 0;
                    forward.Normalize();
                    right.Normalize();

                    Vector3 movementDirection = (forward * fsm.CurrentMoveInput.y + right * fsm.CurrentMoveInput.x).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);

                    // Smooth rotation
                    fsm.Avatar.transform.rotation = Quaternion.Slerp(
                        fsm.Avatar.transform.rotation,
                        targetRotation,
                        _rotationSpeed * Time.deltaTime
                    );
                }
            }
        }


    }
}
