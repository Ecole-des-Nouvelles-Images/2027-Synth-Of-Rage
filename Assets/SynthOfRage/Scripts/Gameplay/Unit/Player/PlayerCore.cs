
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
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
        // Local Multiplayer
        // ─────────────────────────────────────────────

        [Header("Local Multiplayer")]
        [Tooltip("0 = Player 1 / First Gamepad. 1 = Player 2 / Second Gamepad.")]
        [SerializeField, Range(0, 1)] private int playerIndex = 0;

        public int PlayerIndex => playerIndex;
        public Gamepad AssignedGamepad { get; private set; }
        public bool HasAssignedGamepad => AssignedGamepad != null;

        private InputActionAsset runtimeActions;
        private InputUser inputUser;
        private bool inputUserInitialized;

        private InputActionReference[] originalActionReferences;
        private readonly List<InputActionReference> runtimeActionReferences = new();

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
            InitializePlayerInput();

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

            CleanupPlayerInput();

            inputMapInitialized = false;
        }

        // ─────────────────────────────────────────────
        // Local Multiplayer Initialization
        // ─────────────────────────────────────────────

        private void InitializePlayerInput()
        {
            InputActionReference[] originalReferences =
            {
                moveAction,
                jumpAction,
                dashAction,
                atkLAction,
                atkHAction,
                guardAction,
                atkSpAction,
                pauseAction,

                uiMoveAction,
                uiPauseAction,
                validateAction,
                cancelAction,
                panelAction
            };

            InputActionReference firstReference = null;

            foreach (InputActionReference reference in originalReferences)
            {
                if (reference != null)
                {
                    firstReference = reference;
                    break;
                }
            }

            if (firstReference == null)
            {
                UnityEngine.Debug.LogError(
                    "[PlayerCore] No Input Actions assigned.",
                    this
                );

                return;
            }

            InputActionAsset sourceAsset =
                firstReference.action.actionMap.asset;

            // Verify that every action belongs to the same asset.
            foreach (InputActionReference reference in originalReferences)
            {
                if (reference != null &&
                    reference.action.actionMap.asset != sourceAsset)
                {
                    UnityEngine.Debug.LogError(
                        "[PlayerCore] All Input Actions must belong to the same Input Action Asset.",
                        this
                    );

                    return;
                }
            }

            originalActionReferences = originalReferences;

            // Create an independent Input Action Asset for this player.
            runtimeActions = Instantiate(sourceAsset);

            InputActionReference[] clonedReferences =
                new InputActionReference[originalReferences.Length];

            for (int i = 0; i < originalReferences.Length; i++)
            {
                if (originalReferences[i] == null)
                    continue;

                InputAction action = runtimeActions.FindAction(
                    originalReferences[i].action.id.ToString()
                );

                if (action == null)
                {
                    UnityEngine.Debug.LogError(
                        $"[PlayerCore] Could not find cloned action: {originalReferences[i].action.name}",
                        this
                    );

                    continue;
                }

                InputActionReference reference =
                    InputActionReference.Create(action);

                clonedReferences[i] = reference;
                runtimeActionReferences.Add(reference);
            }

            // Replace serialized references with player-specific references.
            SetInputReferences(clonedReferences);

            // Create an independent Input User.
            inputUser = InputUser.CreateUserWithoutPairedDevices();

            inputUser.AssociateActionsWithUser(runtimeActions);

            inputUserInitialized = true;

            // Automatically assign the correct gamepad.
            TryAssignGamepad();

            // Listen for device connections and disconnections.
            InputSystem.onDeviceChange += HandleDeviceChange;

            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
                    $"[PlayerCore] Player {playerIndex + 1} initialized.",
                    this
                );
            }
        }

        private void SetInputReferences(
            InputActionReference[] references)
        {
            moveAction = references[0];
            jumpAction = references[1];
            dashAction = references[2];
            atkLAction = references[3];
            atkHAction = references[4];
            guardAction = references[5];
            atkSpAction = references[6];
            pauseAction = references[7];

            uiMoveAction = references[8];
            uiPauseAction = references[9];
            validateAction = references[10];
            cancelAction = references[11];
            panelAction = references[12];
        }

        // ─────────────────────────────────────────────
        // Gamepad Assignment
        // ─────────────────────────────────────────────

        private void TryAssignGamepad()
        {
            if (!inputUserInitialized)
                return;

            if (AssignedGamepad != null)
                return;

            if (Gamepad.all.Count <= playerIndex)
                return;

            // Player 1 uses Gamepad 0.
            // Player 2 uses Gamepad 1.
            Gamepad gamepad = Gamepad.all[playerIndex];

            if (gamepad == null || !gamepad.added)
                return;

            // Prevent assigning a gamepad already used by another Input User.
            InputUser? existingUser =
                InputUser.FindUserPairedToDevice(gamepad);

            if (existingUser.HasValue && existingUser.Value.valid)
            {
                // The gamepad is already assigned to another player.
                if (existingUser.Value.id != inputUser.id)
                {
                    if (debugEnabled)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[PlayerCore] Gamepad {playerIndex + 1} is already paired with another Input User.",
                            this
                        );
                    }

                    return;
                }
            }

            // Pair the gamepad with this player's Input User.
            inputUser = InputUser.PerformPairingWithDevice(
                gamepad,
                inputUser
            );

            AssignedGamepad = gamepad;

            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
                    $"[PlayerCore] Player {playerIndex + 1} assigned to {gamepad.displayName}.",
                    this
                );
            }
        }

        private void HandleDeviceChange(
            InputDevice device,
            InputDeviceChange change)
        {
            if (device is not Gamepad gamepad)
                return;

            // Handle disconnection.
            if (change == InputDeviceChange.Removed ||
                change == InputDeviceChange.Disconnected)
            {
                if (AssignedGamepad == gamepad)
                {
                    if (debugEnabled)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[PlayerCore] Player {playerIndex + 1} gamepad disconnected.",
                            this
                        );
                    }

                    if (inputUserInitialized && inputUser.valid)
                        inputUser.UnpairDevices();

                    AssignedGamepad = null;
                }

                return;
            }

            // Handle connection or reconnection.
            if (change == InputDeviceChange.Added ||
                change == InputDeviceChange.Reconnected)
            {
                TryAssignGamepad();
            }
        }

        // ─────────────────────────────────────────────
        // Cleanup
        // ─────────────────────────────────────────────

        private void CleanupPlayerInput()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;

            AssignedGamepad = null;

            if (inputUserInitialized && inputUser.valid)
            {
                inputUser.UnpairDevicesAndRemoveUser();
            }

            inputUserInitialized = false;

            // Restore original serialized references.
            if (originalActionReferences != null)
            {
                SetInputReferences(originalActionReferences);
                originalActionReferences = null;
            }

            // Destroy temporary references.
            foreach (InputActionReference reference in runtimeActionReferences)
            {
                if (reference != null)
                    Destroy(reference);
            }

            runtimeActionReferences.Clear();

            // Destroy the player's private action asset.
            if (runtimeActions != null)
            {
                Destroy(runtimeActions);
                runtimeActions = null;
            }
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
            OnPlayerMove?.Invoke(direction);
        }

        private void HandleJump(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] Jump");

            OnPlayerJump?.Invoke();
        }

        private void HandleDash(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] Dash");

            OnPlayerDash?.Invoke();
        }

        private void HandleAtkL(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] AtkL");

            OnPlayerAtkL?.Invoke();
        }

        private void HandleAtkH(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] AtkH");

            OnPlayerAtkH?.Invoke();
        }

        private void HandleGuardStarted(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] Guard : START");

            OnPlayerGuard?.Invoke(true);
        }

        private void HandleGuardCanceled(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] Guard : STOP");

            OnPlayerGuard?.Invoke(false);
        }

        private void HandleAtkSp(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] AtkSp");

            OnPlayerAtkSp?.Invoke();
        }

        private void HandlePause(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] Pause : Gameplay -> UI");

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
                UnityEngine.Debug.Log($"[PlayerCore] UI Move : {direction}");

            OnUIMove?.Invoke(direction);
        }

        private void HandleUIPause(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] Pause : UI -> Gameplay");

            OnUIPause?.Invoke();
            SwitchToGameplay();
        }

        private void HandleUIValidate(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] UI Validate");

            OnUIValidate?.Invoke();
        }

        private void HandleUICancel(InputAction.CallbackContext context)
        {
            if (debugEnabled)
                UnityEngine.Debug.Log("[PlayerCore] UI Cancel");

            OnUICancel?.Invoke();
        }

        private void HandleUIPanel(InputAction.CallbackContext context)
        {
            float direction = context.ReadValue<float>();

            if (debugEnabled)
                UnityEngine.Debug.Log($"[PlayerCore] UI Panel : {direction}");

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
                UnityEngine.Debug.Log(
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
                UnityEngine.Debug.LogError(
                    $"[{nameof(PlayerCore)}] Gameplay Move Action reference is missing.",
                    this
                );

                return;
            }

            InputActionMap actionMap = moveAction.action.actionMap;

            if (actionMap == null)
            {
                UnityEngine.Debug.LogError(
                    $"[{nameof(PlayerCore)}] Gameplay Move Action does not belong to an Action Map.",
                    this
                );

                return;
            }

            actionMap.Enable();

            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
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
                UnityEngine.Debug.Log(
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
                UnityEngine.Debug.LogError(
                    $"[{nameof(PlayerCore)}] UI Move Action reference is missing.",
                    this
                );

                return;
            }

            InputActionMap actionMap = uiMoveAction.action.actionMap;

            if (actionMap == null)
            {
                UnityEngine.Debug.LogError(
                    $"[{nameof(PlayerCore)}] UI Move Action does not belong to an Action Map.",
                    this
                );

                return;
            }

            actionMap.Enable();

            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
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
                UnityEngine.Debug.Log(
                    $"[PlayerCore] UI Action Map : DISABLED ({actionMap.name})"
                );
            }
        }
    }
}
