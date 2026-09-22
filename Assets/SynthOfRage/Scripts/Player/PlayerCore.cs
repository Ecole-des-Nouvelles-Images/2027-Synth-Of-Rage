using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace SynthOfRage.Scripts.Player
{
    public class PlayerCore : MonoBehaviour
    {
        [Header("Dev Settings")]
        [SerializeField] private bool debugEnabled;
        
        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference dashAction;
        [SerializeField] private InputActionReference atkLAction;
        [SerializeField] private InputActionReference atkHAction;
        [SerializeField] private InputActionReference guardAction;
        [SerializeField] private InputActionReference atkSpAction;

        // ─────────────────────────────────────────────
        // Events
        // ─────────────────────────────────────────────

        public event Action<Vector2> OnMove;
        public event Action OnJump;
        public event Action OnDash;
        public event Action OnAtkL;
        public event Action OnAtkH;
        public event Action<bool> OnGuard;
        public event Action OnAtkSp;

        // ─────────────────────────────────────────────
        // Unity
        // ─────────────────────────────────────────────

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
            dashAction.action.Enable();
            atkLAction.action.Enable();
            atkHAction.action.Enable();
            guardAction.action.Enable();
            atkSpAction.action.Enable();

            moveAction.action.performed += HandleMove;
            moveAction.action.canceled += HandleMove;

            jumpAction.action.performed += HandleJump;
            dashAction.action.performed += HandleDash;
            atkLAction.action.performed += HandleAtkL;
            atkHAction.action.performed += HandleAtkH;
            guardAction.action.performed += HandleGuardStarted;
            guardAction.action.canceled += HandleGuardCanceled;
            atkSpAction.action.performed += HandleAtkSp;
        }

        private void OnDisable()
        {
            moveAction.action.performed -= HandleMove;
            moveAction.action.canceled -= HandleMove;

            jumpAction.action.performed -= HandleJump;
            dashAction.action.performed -= HandleDash;
            atkLAction.action.performed -= HandleAtkL;
            atkHAction.action.performed -= HandleAtkH;
            guardAction.action.performed -= HandleGuardStarted;
            guardAction.action.canceled -= HandleGuardCanceled;
            atkSpAction.action.performed -= HandleAtkSp;

            moveAction.action.Disable();
            jumpAction.action.Disable();
            dashAction.action.Disable();
            atkLAction.action.Disable();
            atkHAction.action.Disable();
            guardAction.action.Disable();
            atkSpAction.action.Disable();
        }

        // ─────────────────────────────────────────────
        // Input Callbacks
        // ─────────────────────────────────────────────

        private void HandleMove(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();
            
            if(debugEnabled)Debug.Log($"[PlayerCore] Move : {direction}");

            OnMove?.Invoke(direction);
        }

        private void HandleJump(InputAction.CallbackContext context)
        {
            if(debugEnabled)Debug.Log("[PlayerCore] Jump");

            OnJump?.Invoke();
        }

        private void HandleDash(InputAction.CallbackContext context)
        {
            if(debugEnabled)Debug.Log("[PlayerCore] Dash");

            OnDash?.Invoke();
        }

        private void HandleAtkL(InputAction.CallbackContext context)
        {
            if(debugEnabled)Debug.Log("[PlayerCore] AtkL");

            OnAtkL?.Invoke();
        }

        private void HandleAtkH(InputAction.CallbackContext context)
        {
            if(debugEnabled)Debug.Log("[PlayerCore] AtkH");

            OnAtkH?.Invoke();
        }

        private void HandleGuardStarted(InputAction.CallbackContext context)
        {
            if(debugEnabled)Debug.Log("[PlayerCore] Guard : START");

            OnGuard?.Invoke(true);
        }

        private void HandleGuardCanceled(InputAction.CallbackContext context)
        {
            if(debugEnabled)Debug.Log("[PlayerCore] Guard : STOP");

            OnGuard?.Invoke(false);
        }

        private void HandleAtkSp(InputAction.CallbackContext context)
        {
            if(debugEnabled)Debug.Log("[PlayerCore] AtkSp");

            OnAtkSp?.Invoke();
        }
    }
}