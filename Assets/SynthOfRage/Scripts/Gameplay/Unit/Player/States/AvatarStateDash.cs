using System;
using SynthOfRage.Scripts.Common.StateMachine;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player.States
{
    /// <summary>
    /// Dash state for the Avatar. Character performs a quick dash movement.
    /// </summary>
    [Serializable]
    public class AvatarStateDash : BaseState
    {
        [Header("Dash Settings")]
        [SerializeField] private float _dashDistance = 4f;
        [SerializeField] private float _dashDuration = 0.2f;
        [SerializeField] private float _dashCooldown = 0.5f;

        private static readonly int Dash = Animator.StringToHash("Dash");

        private bool _dashStarted = false;
        private float _dashTimer = 0f;
        private float _dashCooldownTimer = 0f;
        private Vector3 _dashDirection;
        private float _dashStartYVelocity;

        public float DashDistance => _dashDistance;
        public float DashDuration => _dashDuration;
        public float DashCooldown => _dashCooldown;

        public override void EnterState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            // Start the dash in the current input direction
            _dashStarted = TryStartDash(fsm);

            if (_dashStarted)
            {
                var animator = fsm.Avatar.Animator;
                if (animator != null)
                    animator.SetTrigger(Dash);
            }

            UnityEngine.Debug.Log("[AvatarStateDash] Entered Dash state");
        }

        private bool TryStartDash(AvatarFSM fsm)
        {
            var characterController = fsm.Avatar.CharacterController;
            if (characterController == null) return false;

            if (fsm.IsDashing || _dashCooldownTimer > 0 || fsm.IsJumping)
                return false;

            fsm.IsDashing = true;
            _dashTimer = 0f;
            _dashCooldownTimer = 0f;

            // Store dash direction
            if (fsm.CurrentMoveInput.sqrMagnitude > 0.01f)
            {
                var movementSpace = fsm.Avatar.CurrentMovementSpace;
                if (movementSpace != null)
                {
                    Vector3 forward = movementSpace.transform.forward;
                    Vector3 right = movementSpace.transform.right;
                    forward.y = 0;
                    right.y = 0;
                    forward.Normalize();
                    right.Normalize();

                    _dashDirection = (forward * fsm.CurrentMoveInput.y + right * fsm.CurrentMoveInput.x).normalized;
                }
                else
                {
                    _dashDirection = new Vector3(fsm.CurrentMoveInput.x, 0, fsm.CurrentMoveInput.y).normalized;
                }
            }
            else
            {
                // Dash in current facing direction if no input
                _dashDirection = fsm.Avatar.transform.forward;
                _dashDirection.y = 0;
                _dashDirection.Normalize();
            }

            // Store current vertical velocity for continuation after dash
            _dashStartYVelocity = fsm.YVelocity;
            fsm.YVelocity = 0; // Disable gravity during dash

            return true;
        }

        public override void UpdateState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            if (!_dashStarted)
            {
                // If dash couldn't start, try to go to appropriate state
                if (fsm.Avatar.CharacterController.isGrounded && fsm.CurrentMoveInput.sqrMagnitude > 0.01f)
                {
                    fsm.QueueNextState(fsm.States.Move);
                }
                else if (fsm.Avatar.CharacterController.isGrounded)
                {
                    fsm.QueueNextState(fsm.States.Idle);
                }
                else
                {
                    fsm.QueueNextState(fsm.States.Jump);
                }
                return;
            }

            // Process dash movement
            ProcessDash(fsm);

            // Process dash cooldown
            ProcessDashCooldown(fsm);

            // Update character rotation
            UpdateCharacterRotation(fsm);

            // Check if dash has ended
            if (!fsm.IsDashing)
            {
                EndDash(fsm);

                // Transition to appropriate state
                if (fsm.Avatar.CharacterController.isGrounded && fsm.CurrentMoveInput.sqrMagnitude > 0.01f)
                {
                    fsm.QueueNextState(fsm.States.Move);
                }
                else if (fsm.Avatar.CharacterController.isGrounded)
                {
                    fsm.QueueNextState(fsm.States.Idle);
                }
                else
                {
                    // If we're in the air, go to jump state
                    fsm.QueueNextState(fsm.States.Jump);
                }
            }
        }

        private void ProcessDash(AvatarFSM fsm)
        {
            _dashTimer += Time.deltaTime;

            // Calculate dash progress (0 to 1)
            float dashProgress = _dashTimer / _dashDuration;

            if (dashProgress < 1f)
            {
                // Calculate dash speed (ease out for smooth stopping)
                float dashSpeed = _dashDistance / _dashDuration;
                float speedMultiplier = 1f - dashProgress; // Linear falloff

                var characterController = fsm.Avatar.CharacterController;
                if (characterController != null)
                {
                    Vector3 dashMovement = _dashDirection * dashSpeed * speedMultiplier * Time.deltaTime;
                    characterController.Move(dashMovement);
                }
            }
            else
            {
                fsm.IsDashing = false;
            }
        }

        private void ProcessDashCooldown(AvatarFSM fsm)
        {
            if (!fsm.IsDashing && _dashCooldownTimer < _dashCooldown)
            {
                _dashCooldownTimer += Time.deltaTime;
            }
        }

        private void UpdateCharacterRotation(AvatarFSM fsm)
        {
            if (fsm.Avatar != null && _dashDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_dashDirection, Vector3.up);
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

        private void EndDash(AvatarFSM fsm)
        {
            fsm.IsDashing = false;
            _dashTimer = 0f;
            _dashCooldownTimer = 0f;

            // Restore gravity
            fsm.YVelocity = _dashStartYVelocity;
        }

        public override void ExitState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            UnityEngine.Debug.Log("[AvatarStateDash] Exited Dash state");
        }
    }
}
