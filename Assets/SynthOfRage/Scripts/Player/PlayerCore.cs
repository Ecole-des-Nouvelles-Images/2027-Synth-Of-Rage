using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Player
{
    public class PlayerCore : MonoBehaviour
    {
        public enum PlayerInputMap
        {
            Gameplay,
            UI
        }

        // ─────────────────────────────────────────────
        // Dev Settings
        // ─────────────────────────────────────────────

        [Header("Dev Settings")]
        [SerializeField] private bool debugEnabled;

        // ─────────────────────────────────────────────
        // Gameplay Input Actions
        // ─────────────────────────────────────────────

        [Header("Gameplay Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference dashAction;
        [SerializeField] private InputActionReference atkLAction;
        [SerializeField] private InputActionReference atkHAction;
        [SerializeField] private InputActionReference guardAction;
        [SerializeField] private InputActionReference atkSpAction;
        [SerializeField] private InputActionReference pauseAction;

        // ─────────────────────────────────────────────
        // UI Input Actions
        // ─────────────────────────────────────────────

        [Header("UI Input Actions")]
        [SerializeField] private InputActionReference uiMoveAction;
        [SerializeField] private InputActionReference uiPauseAction;
        [SerializeField] private InputActionReference validateAction;
        [SerializeField] private InputActionReference cancelAction;
        [SerializeField] private InputActionReference panelAction;

        // ─────────────────────────────────────────────
        // Gameplay Events
        // ─────────────────────────────────────────────

        public event Action<Vector2> OnPlayerMove;
        public event Action OnPlayerJump;
        public event Action OnPlayerDash;
        public event Action OnPlayerAtkL;
        public event Action OnPlayerAtkH;
        public event Action<bool> OnPlayerGuard;
        public event Action OnPlayerAtkSp;
        public event Action OnPlayerPause;

        // ─────────────────────────────────────────────
        // UI Events
        // ─────────────────────────────────────────────

        public event Action<Vector2> OnUIMove;
        public event Action OnUIPause;
        public event Action OnUIValidate;
        public event Action OnUICancel;
        public event Action OnUIPanelPrevious;
        public event Action OnUIPanelNext;

        // ─────────────────────────────────────────────
        // Input Map Events
        // ─────────────────────────────────────────────

        public event Action<PlayerInputMap> OnInputMapChanged;

        // ─────────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────────

        public PlayerInputMap CurrentInputMap { get; private set; }

        private bool inputMapInitialized;

        // ─────────────────────────────────────────────
        // Unity
        // ─────────────────────────────────────────────

        private void Awake()
        {
            CurrentInputMap = PlayerInputMap.Gameplay;
            inputMapInitialized = false;
        }

        private void OnEnable()
        {
            RegisterGameplayCallbacks();
            RegisterUICallbacks();

            SwitchToGameplay();
        }

        private void OnDisable()
        {
            UnregisterGameplayCallbacks();
            UnregisterUICallbacks();

            DisableGameplayMap();
            DisableUIMap();

            inputMapInitialized = false;
        }

        // ─────────────────────────────────────────────
        // Gameplay Registration
        // ─────────────────────────────────────────────

        private void RegisterGameplayCallbacks()
        {
            if (moveAction != null)
            {
                moveAction.action.performed += HandleMove;
                moveAction.action.canceled += HandleMove;
            }

            if (jumpAction != null)
                jumpAction.action.performed += HandleJump;

            if (dashAction != null)
                dashAction.action.performed += HandleDash;

            if (atkLAction != null)
                atkLAction.action.performed += HandleAtkL;

            if (atkHAction != null)
                atkHAction.action.performed += HandleAtkH;

            if (guardAction != null)
            {
                guardAction.action.performed += HandleGuardStarted;
                guardAction.action.canceled += HandleGuardCanceled;
            }

            if (atkSpAction != null)
                atkSpAction.action.performed += HandleAtkSp;

            if (pauseAction != null)
                pauseAction.action.performed += HandlePause;
        }

        private void UnregisterGameplayCallbacks()
        {
            if (moveAction != null)
            {
                moveAction.action.performed -= HandleMove;
                moveAction.action.canceled -= HandleMove;
            }

            if (jumpAction != null)
                jumpAction.action.performed -= HandleJump;

            if (dashAction != null)
                dashAction.action.performed -= HandleDash;

            if (atkLAction != null)
                atkLAction.action.performed -= HandleAtkL;

            if (atkHAction != null)
                atkHAction.action.performed -= HandleAtkH;

            if (guardAction != null)
            {
                guardAction.action.performed -= HandleGuardStarted;
                guardAction.action.canceled -= HandleGuardCanceled;
            }

            if (atkSpAction != null)
                atkSpAction.action.performed -= HandleAtkSp;

            if (pauseAction != null)
                pauseAction.action.performed -= HandlePause;
        }

        // ─────────────────────────────────────────────
        // UI Registration
        // ─────────────────────────────────────────────

        private void RegisterUICallbacks()
        {
            if (uiMoveAction != null)
            {
                uiMoveAction.action.performed += HandleUIMove;
                uiMoveAction.action.canceled += HandleUIMove;
            }

            if (uiPauseAction != null)
                uiPauseAction.action.performed += HandleUIPause;

            if (validateAction != null)
                validateAction.action.performed += HandleUIValidate;

            if (cancelAction != null)
                cancelAction.action.performed += HandleUICancel;

            if (panelAction != null)
                panelAction.action.performed += HandleUIPanel;
        }

        private void UnregisterUICallbacks()
        {
            if (uiMoveAction != null)
            {
                uiMoveAction.action.performed -= HandleUIMove;
                uiMoveAction.action.canceled -= HandleUIMove;
            }

            if (uiPauseAction != null)
                uiPauseAction.action.performed -= HandleUIPause;

            if (validateAction != null)
                validateAction.action.performed -= HandleUIValidate;

            if (cancelAction != null)
                cancelAction.action.performed -= HandleUICancel;

            if (panelAction != null)
                panelAction.action.performed -= HandleUIPanel;
        }

        // ─────────────────────────────────────────────
        // Gameplay Input Callbacks
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

        private void HandlePause(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] Pause : Gameplay -> UI");

            OnPlayerPause?.Invoke();

            SwitchToUI();
        }

        // ─────────────────────────────────────────────
        // UI Input Callbacks
        // ─────────────────────────────────────────────

        private void HandleUIMove(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();

            if (debugEnabled)
                Debug.Log($"[PlayerCore] UI Move : {direction}");

            OnUIMove?.Invoke(direction);
        }

        private void HandleUIPause(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] Pause : UI -> Gameplay");

            OnUIPause?.Invoke();

            SwitchToGameplay();
        }

        private void HandleUIValidate(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] UI Validate");

            OnUIValidate?.Invoke();
        }

        private void HandleUICancel(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                Debug.Log("[PlayerCore] UI Cancel");

            OnUICancel?.Invoke();
        }

        private void HandleUIPanel(InputAction.CallbackContext context)
        {
            float direction = context.ReadValue<float>();

            if (debugEnabled)
                Debug.Log($"[PlayerCore] UI Panel : {direction}");

            if (direction < 0f)
            {
                OnUIPanelPrevious?.Invoke();
            }
            else if (direction > 0f)
            {
                OnUIPanelNext?.Invoke();
            }
        }

        // ─────────────────────────────────────────────
        // Input Map Switching
        // ─────────────────────────────────────────────

        public void SwitchToGameplay()
        {
            SwitchInputMap(PlayerInputMap.Gameplay);
        }

        public void SwitchToUI()
        {
            SwitchInputMap(PlayerInputMap.UI);
        }

        public void SwitchInputMap(PlayerInputMap targetMap)
        {
            if (inputMapInitialized && CurrentInputMap == targetMap)
                return;

            DisableGameplayMap();
            DisableUIMap();

            CurrentInputMap = targetMap;

            switch (targetMap)
            {
                case PlayerInputMap.Gameplay:
                    EnableGameplayMap();
                    break;

                case PlayerInputMap.UI:
                    EnableUIMap();
                    break;
            }

            inputMapInitialized = true;

            if (debugEnabled)
            {
                Debug.Log(
                    $"[PlayerCore] Input Map Changed : {CurrentInputMap}"
                );
            }

            OnInputMapChanged?.Invoke(CurrentInputMap);
        }

        // ─────────────────────────────────────────────
        // Gameplay Action Map
        // ─────────────────────────────────────────────

        private void EnableGameplayMap()
        {
            if (moveAction == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerCore)}] " +
                    "Gameplay Move Action reference is missing.",
                    this
                );

                return;
            }

            InputActionMap actionMap = moveAction.action.actionMap;

            if (actionMap == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerCore)}] " +
                    "Gameplay Move Action does not belong to an Action Map.",
                    this
                );

                return;
            }

            actionMap.Enable();

            if (debugEnabled)
            {
                Debug.Log(
                    $"[PlayerCore] Gameplay Action Map : ENABLED ({actionMap.name})"
                );
            }
        }

        private void DisableGameplayMap()
        {
            if (moveAction == null)
                return;

            InputActionMap actionMap = moveAction.action.actionMap;

            if (actionMap == null)
                return;

            actionMap.Disable();

            if (debugEnabled)
            {
                Debug.Log(
                    $"[PlayerCore] Gameplay Action Map : DISABLED ({actionMap.name})"
                );
            }
        }

        // ─────────────────────────────────────────────
        // UI Action Map
        // ─────────────────────────────────────────────

        private void EnableUIMap()
        {
            if (uiMoveAction == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerCore)}] " +
                    "UI Move Action reference is missing.",
                    this
                );

                return;
            }

            InputActionMap actionMap = uiMoveAction.action.actionMap;

            if (actionMap == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerCore)}] " +
                    "UI Move Action does not belong to an Action Map.",
                    this
                );

                return;
            }

            actionMap.Enable();

            if (debugEnabled)
            {
                Debug.Log(
                    $"[PlayerCore] UI Action Map : ENABLED ({actionMap.name})"
                );
            }
        }

        private void DisableUIMap()
        {
            if (uiMoveAction == null)
                return;

            InputActionMap actionMap = uiMoveAction.action.actionMap;

            if (actionMap == null)
                return;

            actionMap.Disable();

            if (debugEnabled)
            {
                Debug.Log(
                    $"[PlayerCore] UI Action Map : DISABLED ({actionMap.name})"
                );
            }
        }
    }
}