using System;
using SynthOfRage.Scripts.Common.StateMachine;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player.States
{
    /// <summary>
    /// Guard state for the Avatar. Character enters a defensive stance.
    /// </summary>
    [Serializable]
    public class AvatarStateGuard : BaseState
    {
        private static readonly int Guard = Animator.StringToHash("Guard");
        private static readonly int Guarding = Animator.StringToHash("Guarding");

        public override void EnterState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            var animator = fsm.Avatar.Animator;
            if (animator != null)
            {
                animator.SetTrigger(Guard);
                animator.SetBool(Guarding, true);
            }

            UnityEngine.Debug.Log("[AvatarStateGuard] Entered Guard state");
        }

        public override void UpdateState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            // Continue processing gravity during guard
            ProcessGravity(fsm);
            UpdateCharacterRotation(fsm);

            // For now, guard state transitions back to idle/move based on input
            // In a real implementation, this would be controlled by input release
            if (!fsm.IsGuarding)
            {
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

        private void ProcessGravity(AvatarFSM fsm)
        {
            var characterController = fsm.Avatar.CharacterController;
            if (characterController == null) return;

            if (characterController.isGrounded && fsm.YVelocity < 0)
            {
                fsm.YVelocity = -2f;
            }
            else
            {
                fsm.YVelocity += fsm.Avatar.Gravity * Time.deltaTime;
            }

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
                    Vector3 forward = movementSpace.transform.forward;
                    Vector3 right = movementSpace.transform.right;
                    forward.y = 0;
                    right.y = 0;
                    forward.Normalize();
                    right.Normalize();

                    Vector3 movementDirection = (forward * fsm.CurrentMoveInput.y + right * fsm.CurrentMoveInput.x).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);

                    float rotationSpeed = 15f;
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

        public override void ExitState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            var animator = fsm.Avatar.Animator;
            if (animator != null)
            {
                animator.SetBool(Guarding, false);
            }

            UnityEngine.Debug.Log("[AvatarStateGuard] Exited Guard state");
        }
    }
}
