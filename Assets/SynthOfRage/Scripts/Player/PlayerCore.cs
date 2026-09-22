using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public event Action<Vector2> OnPlayerMove;
        public event Action OnPlayerJump;
        public event Action OnPlayerDash;
        public event Action OnPlayerAtkL;
        public event Action OnPlayerAtkH;
        public event Action<bool> OnPlayerGuard;
        public event Action OnPlayerAtkSp;

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

            if (debugEnabled)
                Debug.Log($"[PlayerCore] Move : {direction}");

            OnPlayerMove?.Invoke(direction);
        }

        private void HandleJump(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] Jump");

            OnPlayerJump?.Invoke();
        }

        private void HandleDash(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] Dash");

            OnPlayerDash?.Invoke();
        }

        private void HandleAtkL(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] AtkL");

            OnPlayerAtkL?.Invoke();
        }

        private void HandleAtkH(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] AtkH");

            OnPlayerAtkH?.Invoke();
        }

        private void HandleGuardStarted(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] Guard : START");

            OnPlayerGuard?.Invoke(true);
        }

        private void HandleGuardCanceled(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] Guard : STOP");

            OnPlayerGuard?.Invoke(false);
        }

        private void HandleAtkSp(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] AtkSp");

            OnPlayerAtkSp?.Invoke();
        }
    }
}