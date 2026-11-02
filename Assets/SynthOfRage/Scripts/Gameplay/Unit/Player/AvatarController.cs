using System;
using SynthOfRage.Scripts.Debug;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
{
    /// <summary>Responsible to handle all input related operations for the <see cref="Avatar"/>. </summary>
    [Serializable]
    public partial class AvatarController
    {
        #region Static Input Debug Events

#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION

        /// <summary>Controls whenever <c>PlayerControllers</c> should log the input events for debugging.</summary>
        /// <seealso cref="DebugController"/>
        [AutoStaticsCleanup] private static bool _debugInputEvent = false;

        /// <summary>
        /// Leverage Unity's InputSystem event bus to debug <i><b>any</b></i> InputAction performed, depending on the state of <see cref="_debugInputEvent"/>.
        /// </summary>
        /// <param name="forceDisable">Request the method to bypass the standard toggle and force to unsubscribe from the InputSystem event bus.</param>
        public static void ToggleInputEventsDebug(bool forceDisable = false)
        {
            bool wasEnabled = _debugInputEvent;
            _debugInputEvent = forceDisable ? false : !_debugInputEvent;

            if (_debugInputEvent && !wasEnabled)
                InputSystem.onActionChange += OnGlobalActionChange;
            else if (!_debugInputEvent && wasEnabled)
                InputSystem.onActionChange -= OnGlobalActionChange;

            UnityEngine.Debug.Log($"[AvatarController] Input debugging is now {(_debugInputEvent ? "ENABLED" : "DISABLED")}.");
        }

        private static void OnGlobalActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;

            InputAction action = (InputAction)obj;

            UnityEngine.Debug.Log($"[AvatarController] Input {action.name} was performed.");
        }

#endif

        #endregion

        #region Input Events (Internal)

        /// <summary>
        /// This is an « <i>input event</i> ». It fires when the associated input has been performed by the user.
        /// These events are <b>internal</b>. You should subscribe to the « <b>gameplay events</b> » managed directly by <see cref="Avatar"/> instead.
        /// </summary>
        internal event Action<InputAction.CallbackContext> OnMoveInput;
        /// <inheritdoc cref="OnMoveInput"/>
        internal event Action<InputAction.CallbackContext> OnMoveEndInput;
        /// <inheritdoc cref="OnMoveInput"/>
        internal event Action<InputAction.CallbackContext> OnJumpInput;
        /// <inheritdoc cref="OnMoveInput"/>
        internal event Action<InputAction.CallbackContext> OnDashInput;
        /// <inheritdoc cref="OnMoveInput"/>
        internal event Action<InputAction.CallbackContext> OnGuardInput;
        /// <inheritdoc cref="OnMoveInput"/>
        internal event Action<InputAction.CallbackContext> OnAttackLightInput;
        /// <inheritdoc cref="OnMoveInput"/>
        internal event Action<InputAction.CallbackContext> OnAttackHeavyInput;
        /// <inheritdoc cref="OnMoveInput"/>
        internal event Action<InputAction.CallbackContext> OnSpecialInput;
        /// <inheritdoc cref="OnMoveInput"/>
        internal event Action<InputAction.CallbackContext> OnInteractInput;

        #endregion

        private InputActionMap _mapGameplay;
        private InputActionMap _mapUI;

        private InputAction _move;
        private InputAction _jump;
        private InputAction _dash;
        private InputAction _guard;
        private InputAction _attackLight;
        private InputAction _attackHeavy;
        private InputAction _special;
        private InputAction _interaction;

        [SerializeField] private InputActionAsset _inputActionAsset;
        private bool _subscribed;
        private bool _warnedMissingAsset;

        public AvatarController(InputActionAsset inputActionAsset = null)
        {
            _inputActionAsset = inputActionAsset;
        }

        /// <summary>
        /// Wires the <c>AvatarController</c> InputAction related fields from the project-wide <c>InputActionAsset</c> or the override.
        /// </summary>
        /// <exception cref="NullReferenceException">Raise a critical exception in the event any map or action if not defined in the <c>InputActionAsset</c>.</exception>
        private void RegisterInputActionFields()
        {
            if (!_inputActionAsset)
                _inputActionAsset = InputSystem.actions;

            if (!_inputActionAsset)
            {
                if (!_warnedMissingAsset)
                {
                    UnityEngine.Debug.LogError("[AvatarController] No InputActionAsset available. Assign Input Action Asset on Avatar (same asset as PlayerCore) in the Inspector.");
                    _warnedMissingAsset = true;
                }
                return;
            }

            _mapGameplay = _inputActionAsset.FindActionMap("Gameplay", false);
            _mapUI = _inputActionAsset.FindActionMap("UI", false);
            if (_mapGameplay == null)
            {
                UnityEngine.Debug.LogError($"[AvatarController] Gameplay Action Map missing from {_inputActionAsset.name}.");
                return;
            }

            _move = _mapGameplay.FindAction("Move", false);
            _jump = _mapGameplay.FindAction("Jump", false);
            _dash = _mapGameplay.FindAction("Dash", false);
            _guard = _mapGameplay.FindAction("Guard", false);
            _attackLight = _mapGameplay.FindAction("AttackLight", false);
            _attackHeavy = _mapGameplay.FindAction("AttackHeavy", false);
            _special = _mapGameplay.FindAction("Special", false);
            _interaction = _mapGameplay.FindAction("Interact", false);
        }

        /// <summary>
        /// Subscribes the « <i>input events</i> » to the <c>InputActions</c>.
        /// </summary>
        public void SubscribeToInputActions()
        {
            if (_subscribed) return;
            RegisterInputActionFields();
            if (_inputActionAsset == null || _mapGameplay == null) return;

            if (_move != null) { _move.performed += OnMoveInputCallback; _move.canceled += OnMoveEndInputCallback; }
            if (_jump != null) _jump.performed += OnJumpInputCallback;
            if (_dash != null) _dash.performed += OnDashInputCallback;
            if (_guard != null) _guard.performed += OnGuardInputCallback;
            if (_attackLight != null) _attackLight.performed += OnAttackLightInputCallback;
            if (_attackHeavy != null) _attackHeavy.performed += OnAttackHeavyInputCallback;
            if (_special != null) _special.performed += OnSpecialInputCallback;
            if (_interaction != null) _interaction.performed += OnInteractionInputCallback;
            _subscribed = true;
        }

        /// <summary>
        /// Unsubscribes from the <c>InputActions</c>.
        /// </summary>
        public void UnsubscribeFromInputActions()
        {
            if (!_subscribed) return;
            if (_move != null) { _move.performed -= OnMoveInputCallback; _move.canceled -= OnMoveEndInputCallback; }
            if (_jump != null) _jump.performed -= OnJumpInputCallback;
            if (_dash != null) _dash.performed -= OnDashInputCallback;
            if (_guard != null) _guard.performed -= OnGuardInputCallback;
            if (_attackLight != null) _attackLight.performed -= OnAttackLightInputCallback;
            if (_attackHeavy != null) _attackHeavy.performed -= OnAttackHeavyInputCallback;
            if (_special != null) _special.performed -= OnSpecialInputCallback;
            if (_interaction != null) _interaction.performed -= OnInteractionInputCallback;
            _subscribed = false;
        }

        #region Input Event Callbacks

        private void OnMoveInputCallback(InputAction.CallbackContext ctx) => OnMoveInput?.Invoke(ctx);
        private void OnMoveEndInputCallback(InputAction.CallbackContext ctx) => OnMoveEndInput?.Invoke(ctx);
        private void OnJumpInputCallback(InputAction.CallbackContext ctx) => OnJumpInput?.Invoke(ctx);
        private void OnDashInputCallback(InputAction.CallbackContext ctx) => OnDashInput?.Invoke(ctx);
        private void OnGuardInputCallback(InputAction.CallbackContext ctx) => OnGuardInput?.Invoke(ctx);
        private void OnAttackLightInputCallback(InputAction.CallbackContext ctx) => OnAttackLightInput?.Invoke(ctx);
        private void OnAttackHeavyInputCallback(InputAction.CallbackContext ctx) => OnAttackHeavyInput?.Invoke(ctx);
        private void OnSpecialInputCallback(InputAction.CallbackContext ctx) => OnSpecialInput?.Invoke(ctx);
        private void OnInteractionInputCallback(InputAction.CallbackContext ctx) => OnInteractInput?.Invoke(ctx);

        #endregion
    }
}
