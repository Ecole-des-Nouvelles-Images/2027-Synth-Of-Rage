using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

using SynthOfRage.Scripts.Debug;

namespace SynthOfRage.Scripts.Player
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

        [SerializeField] private InputActionAsset _inputActionAsset = InputSystem.actions;

        public AvatarController()
        {
            RegisterInputActionFields();
        }

        /// <summary>
        /// Wires the <c>AvatarController</c> InputAction related fields from the project-wide <c>InputActionAsset</c> or the override.
        /// </summary>
        /// <exception cref="NullReferenceException">Raise a critical exception in the event any map or action if not defined in the <c>InputActionAsset</c>.</exception>
        private void RegisterInputActionFields()
        {
            _mapGameplay = _inputActionAsset.FindActionMap("Gameplay");
            if (_mapGameplay == null) throw new NullReferenceException($"[AvatarController] The project-wide Input Action Asset « {_inputActionAsset.name} » doesn't have a « Gameplay » map.");

            _mapUI = _inputActionAsset.FindActionMap("UI");
            if (_mapUI == null) throw new NullReferenceException($"[AvatarController] The project-wide Input Action Asset « {_inputActionAsset.name} » doesn't have a « UI » map.");

            _move = _mapGameplay.FindAction("Move");
            if (_move == null) throw new NullReferenceException($"[AvatarController] The Gameplay Input Action Map doesn't have a « Move » action.");
            _jump = _mapGameplay.FindAction("Jump");
            if (_jump == null) throw new NullReferenceException($"[AvatarController] The Gameplay Input Action Map doesn't have a « Jump » action.");
            _dash = _mapGameplay.FindAction("Dash");
            if (_dash == null) throw new NullReferenceException($"[AvatarController] The Gameplay Input Action Map doesn't have a « Dash » action.");
            _guard = _mapGameplay.FindAction("Guard");
            if (_guard == null) throw new NullReferenceException($"[AvatarController] The Gameplay Input Action Map doesn't have a « Guard » action.");
            _attackLight = _mapGameplay.FindAction("AttackLight");
            if (_attackLight == null) throw new NullReferenceException($"[AvatarController] The Gameplay Input Action Map doesn't have a « AttackLight » action.");
            _attackHeavy = _mapGameplay.FindAction("AttackHeavy");
            if (_attackHeavy == null) throw new NullReferenceException($"[AvatarController] The Gameplay Input Action Map doesn't have a « AttackHeavy » action.");
            _special = _mapGameplay.FindAction("Special");
            if (_special == null) throw new NullReferenceException($"[AvatarController] The Gameplay Input Action Map doesn't have a « Special » action.");
            _interaction = _mapGameplay.FindAction("Interaction");
            if (_interaction == null) throw new NullReferenceException($"[AvatarController] The Gameplay Input Action Map doesn't have a « Interaction » action.");
        }

        /// <summary>
        /// Subscribes the « <i>input events</i> » to the <c>InputActions</c>.
        /// </summary>
        public void SubscribeToInputActions()
        {
            _move.performed += OnMoveInputCallback;
            _jump.performed += OnJumpInputCallback;
            _dash.performed += OnDashInputCallback;
            _guard.performed += OnGuardInputCallback;
            _attackLight.performed += OnAttackLightInputCallback;
            _attackHeavy.performed += OnAttackHeavyInputCallback;
            _special.performed += OnSpecialInputCallback;
            _interaction.performed += OnInteractionInputCallback;
        }

        /// <summary>
        /// Unsubscribes from the <c>InputActions</c>.
        /// </summary>
        public void UnsubscribeFromInputActions()
        {
            _move.performed -= OnMoveInputCallback;
            _jump.performed -= OnJumpInputCallback;
            _dash.performed -= OnDashInputCallback;
            _guard.performed -= OnGuardInputCallback;
            _attackLight.performed -= OnAttackLightInputCallback;
            _attackHeavy.performed -= OnAttackHeavyInputCallback;
            _special.performed -= OnSpecialInputCallback;
            _interaction.performed -= OnInteractionInputCallback;
        }

        #region Input Event Callbacks

        private void OnMoveInputCallback(InputAction.CallbackContext ctx) => OnMoveInput?.Invoke(ctx);
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
