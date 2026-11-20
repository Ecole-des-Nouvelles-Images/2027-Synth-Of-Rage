using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Gameplay.Unit.Avatar
{
    [RequireComponent(typeof(PlayerInput))]
    public partial class AvatarController : MonoBehaviour
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
        
        [SerializeField] private InputActionAsset _inputActionAsset;
        
        private PlayerInput _playerInputs;
        
        private InputActionMap _gameplayMap;
        private InputActionMap _uiMap;

        private InputAction _move;
        private InputAction _jump;
        private InputAction _dash;
        private InputAction _guard;
        private InputAction _attackLight;
        private InputAction _attackHeavy;
        private InputAction _special;
        private InputAction _interaction;
        private bool _subscribed;
        private bool _warnedMissingAsset;

        private void Awake()
        {
            RegisterInputActionFields();
        }

        private void Start()
        {
            SubscribeToInputActions();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInputActions();
        }

        public void TogglesActionMaps()
        {
            if(_playerInputs.currentActionMap.name == "Gameplay")
                _playerInputs.SwitchCurrentActionMap("UI");
            else
                _playerInputs.SwitchCurrentActionMap("Gameplay");
        }
        
        private void RegisterInputActionFields()
        {
            if (!_inputActionAsset)
                _inputActionAsset = InputSystem.actions;

            _gameplayMap = _inputActionAsset.FindActionMap("Gameplay", false);
            _uiMap = _inputActionAsset.FindActionMap("UI", false);
            
            _move = _gameplayMap.FindAction("Move", false);
            _jump = _gameplayMap.FindAction("Jump", false);
            _dash = _gameplayMap.FindAction("Dash", false);
            _guard = _gameplayMap.FindAction("Guard", false);
            _attackLight = _gameplayMap.FindAction("AttackLight", false);
            _attackHeavy = _gameplayMap.FindAction("AttackHeavy", false);
            _special = _gameplayMap.FindAction("Special", false);
            _interaction = _gameplayMap.FindAction("Interact", false);
            
            _gameplayMap.Disable();
            _uiMap.Disable();
        }
        
         public void SubscribeToInputActions()
        {
            UnityEngine.Debug.Log($"Subscribe to input actions");
            if (_subscribed) return;

            if (_move != null) { _move.started += OnMoveInputCallback; _move.canceled += OnMoveEndInputCallback; }
            if (_jump != null)_jump.performed += OnJumpInputCallback;
            if (_dash != null) _dash.performed += OnDashInputCallback;
            if (_guard != null) { _guard.started += OnGuardInputCallback; _guard.canceled += OnGuardInputEndCallback; }
            if (_attackLight != null) _attackLight.performed += OnAttackLightInputCallback;
            if (_attackHeavy != null) _attackHeavy.performed += OnAttackHeavyInputCallback;
            if (_special != null) _special.performed += OnSpecialInputCallback;
            if (_interaction != null) _interaction.performed += OnInteractionInputCallback;
            _subscribed = true;
            _gameplayMap.Enable();
            
        }

        /// <summary>
        /// Unsubscribes from the <c>InputActions</c>.
        /// </summary>
        public void UnsubscribeFromInputActions()
        {
            if (!_subscribed) return;
            if (_move != null) { _move.started -= OnMoveInputCallback; _move.canceled -= OnMoveEndInputCallback; }
            if (_jump != null) _jump.performed -= OnJumpInputCallback;
            if (_dash != null) _dash.performed -= OnDashInputCallback;
            if (_guard != null) { _guard.started -= OnGuardInputCallback; _guard.canceled -= OnGuardInputEndCallback; }
            if (_attackLight != null) _attackLight.performed -= OnAttackLightInputCallback;
            if (_attackHeavy != null) _attackHeavy.performed -= OnAttackHeavyInputCallback;
            if (_special != null) _special.performed -= OnSpecialInputCallback;
            if (_interaction != null) _interaction.performed -= OnInteractionInputCallback;
            _subscribed = false;
        }
        
        /// <summary>
        /// Subscribes the « <i>input events</i> » to the <c>InputActions</c>.
        /// </summary>
       
        #region Input Event Callbacks

        private void OnMoveInputCallback(InputAction.CallbackContext ctx)
        { 
            UnityEngine.Debug.Log($"Move START input callback: " + ctx.action.name);
        }
        private void OnMoveEndInputCallback(InputAction.CallbackContext ctx)
        { 
            UnityEngine.Debug.Log($"Move END input callback: " + ctx.action.name);
        }
        private void OnJumpInputCallback(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log($"Jump input callback: " + ctx.action.name);
        }
        private void OnDashInputCallback(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log($"Dash input callback: " + ctx.action.name);
        }
        private void OnGuardInputCallback(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log($"Guard input START callback: " + ctx.action.name);
        }
        private void OnGuardInputEndCallback(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log($"Guard input END callback: " + ctx.action.name);
        }
        private void OnAttackLightInputCallback(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log($"AttackLight input callback: " + ctx.action.name);
        }
        private void OnAttackHeavyInputCallback(InputAction.CallbackContext ctx) 
        {
            UnityEngine.Debug.Log($"AttackHeavy input callback: " + ctx.action.name);
        }
        private void OnSpecialInputCallback(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log($"Spécial input callback: " + ctx.action.name);
        }
        private void OnInteractionInputCallback(InputAction.CallbackContext ctx)
        {
            UnityEngine.Debug.Log($"Interact input callback: " + ctx.action.name);
        }
        #endregion
    }
}