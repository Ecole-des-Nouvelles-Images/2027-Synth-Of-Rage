using System;
using SynthOfRage.Scripts.Common.StateMachine;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player.States
{
    /// <summary>
    /// Jump state for the Avatar. Character performs a jump action.
    /// </summary>
    [Serializable]
    public class AvatarStateJump : BaseState
    {
        [Header("Jump Settings")]
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _jumpDuration = 0.5f;
        [SerializeField] private float _airControl = 0.5f;

        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int AirSpeed = Animator.StringToHash("AirSpeed");

        private bool _jumpStarted = false;

        public float JumpHeight => _jumpHeight;
        public float JumpDuration => _jumpDuration;
        public float AirControl => _airControl;

        public override void EnterState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            var characterController = fsm.Avatar.CharacterController;
            if (characterController == null || !characterController.isGrounded || fsm.IsJumping || fsm.IsDashing)
            {
                _jumpStarted = false;
                return;
            }

            // Start the jump
            _jumpStarted = TryStartJump(fsm);

            if (_jumpStarted)
            {
                var animator = fsm.Avatar.Animator;
                if (animator != null)
                    animator.SetTrigger(Jump);
            }


            UnityEngine.Debug.Log("[AvatarStateJump] Entered Jump state");
        }

        public override void UpdateState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            if (!_jumpStarted)
            {
                // If jump couldn't start, go back to idle
                fsm.QueueNextState(fsm.States.Idle);
                return;
            }

            // Process jump physics
            ProcessJump(fsm);

            // Process horizontal movement during jump
            ProcessMovement(fsm);

            // Update character rotation
            UpdateCharacterRotation(fsm);

            // Check if jump has ended
            if (!fsm.IsJumping)
            {
                EndJump(fsm);

                // Transition to appropriate state based on input
                if (fsm.CurrentMoveInput.sqrMagnitude > 0.01f)
                {
                    fsm.QueueNextState(fsm.States.Move);
                }
                else
                {
                    fsm.QueueNextState(fsm.States.Idle);
                }
            }
        }

        private bool TryStartJump(AvatarFSM fsm)
        {
            var characterController = fsm.Avatar.CharacterController;
            if (characterController == null || !characterController.isGrounded || fsm.IsJumping || fsm.IsDashing)
                return false;

            // Calculate jump velocity
            fsm.YVelocity = Mathf.Sqrt(_jumpHeight * -2f * fsm.Avatar.Gravity);
            fsm.IsJumping = true;

            return true;
        }

        private void ProcessJump(AvatarFSM fsm)
        {
            var characterController = fsm.Avatar.CharacterController;
            if (characterController == null) return;

            // Apply gravity during jump
            fsm.YVelocity += fsm.Avatar.Gravity * Time.deltaTime;

            // Apply vertical movement
            Vector3 verticalMovement = Vector3.up * fsm.YVelocity * Time.deltaTime;
            characterController.Move(verticalMovement);

            // End jump when we hit the ground
            if (characterController.isGrounded)
            {
                fsm.IsJumping = false;
            }
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

                // Apply movement with air control
                float effectiveSpeed = fsm.Avatar.MoveSpeed * _airControl;
                Vector3 movement = movementDirection * effectiveSpeed * Time.deltaTime;

                characterController.Move(movement);

                // Update animator speed parameter
                var animator = fsm.Avatar.Animator;
                if (animator != null)
                {
                    animator.SetFloat(AirSpeed, fsm.CurrentMoveInput.magnitude * _airControl);
                }
            }
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

                    // Smooth rotation - use Move state's rotation speed as fallback
                    float rotationSpeed = 15f; // Default
                    if (fsm.States.Move != null)
                        rotationSpeed = fsm.States.Move.RotationSpeed;

                    fsm.Avatar.transform.rotation = Quaternion.Slerp(
                        fsm.Avatar.transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }

        private void EndJump(AvatarFSM fsm)
        {
            fsm.IsJumping = false;
        }

        public override void ExitState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;


            UnityEngine.Debug.Log("[AvatarStateJump] Exited Jump state");
        }
    }
}
