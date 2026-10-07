using System;
using SynthOfRage.Scripts.Common.StateMachine;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player.States
{
    /// <summary>
    /// Idle state for the Avatar. Character stands still and waits for input.
    /// </summary>
    [Serializable]
    public class AvatarStateIdle : BaseState
    {
        private static readonly int Idle = Animator.StringToHash("Idle");

        public override void EnterState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            // Trigger idle animation
            var animator = fsm.Avatar.Animator;
            if (animator != null)
                animator.SetTrigger(Idle);

            UnityEngine.Debug.Log("[AvatarStateIdle] Entered Idle state");
        }

        public override void UpdateState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;

            // Process gravity to keep character grounded
            ProcessGravity(fsm);
        }

        private void ProcessGravity(AvatarFSM fsm)
        {
            var characterController = fsm.Avatar.CharacterController;
            if (characterController == null) return;

            // Process gravity using shared velocity from FSM
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

        public override void ExitState(FSM reference)
        {
            var fsm = reference as AvatarFSM;
            if (fsm?.Avatar == null) return;


            UnityEngine.Debug.Log("[AvatarStateIdle] Exited Idle state");
        }
    }
}
