using System;
using SynthOfRage.Scripts.Gameplay.Common.Modules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
{
    /// <summary>
    /// The main class representing the Player.<br/>
    /// It offers « player action events » to subscribe to and manages the components that handle the visuals.
    /// </summary>
    public class Avatar : UnitBase
    {
        #region Player Action Events

        /// <summary>
        /// This is a « <b><i>player action event</i></b> ». It relays its counterpart « <i>input event</i> » from the <c>AvatarController</c>.<br/>
        /// You can subscribe to these events freely. You can consider them as firing as soon the input is performed, for the time being.
        /// </summary>
        public event Action<InputAction.CallbackContext> OnMove;
        /// <inheritdoc cref="OnMove"/>
        public event Action<InputAction.CallbackContext> OnMoveEnd;
        /// <inheritdoc cref="OnMove"/>
        public event Action<InputAction.CallbackContext> OnJump;
        /// <inheritdoc cref="OnMove"/>
        public event Action<InputAction.CallbackContext> OnDash;
        /// <inheritdoc cref="OnMove"/>
        public event Action<InputAction.CallbackContext> OnGuard;
        /// <inheritdoc cref="OnMove"/>
        public event Action<InputAction.CallbackContext> OnAttackLight;
        /// <inheritdoc cref="OnMove"/>
        public event Action<InputAction.CallbackContext> OnAttackHeavy;
        /// <inheritdoc cref="OnMove"/>
        public event Action<InputAction.CallbackContext> OnSpecial;
        /// <inheritdoc cref="OnMove"/>
        public event Action<InputAction.CallbackContext> OnInteract;

        #endregion

        public CharacterController Controller { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        public Animator Animator { get; private set; }
        public HealthModule HealthModule { get; private set; }

        [SerializeField] private AvatarController _inputController;

        private void Awake()
        {
            RegisterComponents();
        }

        private void OnEnable()
        {
            ManageEventCallbacks();
        }

        private void OnDisable()
        {
            ManageEventCallbacks(true);
        }

        /// <summary>Reference all the components needed for <c>Avatar</c></summary>
        /// <exception cref="NullReferenceException">Raise a critical exception if a missing reference is unresolved.</exception>
        private void RegisterComponents()
        {
            _inputController = new();

            Controller = GetComponentInChildren<CharacterController>();
            if (!Controller)
                throw new NullReferenceException("[Avatar] Missing <CharacterController> component.");

            Renderer = GetComponentInChildren<SpriteRenderer>();
            if (!Renderer)
                throw new NullReferenceException("[Avatar] Missing <SpriteRender> component.");

            Animator = GetComponentInChildren<Animator>();
            if (!Animator)
            {
                // TODO: Automatically add the component and use <Addressable> package to link the runtime controller.
                throw new NullReferenceException("[Avatar] No Animator component was found.");
            }
            else if (!(Animator.isInitialized && Animator.runtimeAnimatorController))
            {
                UnityEngine.Debug.LogError("[Avatar] Animator is missing a controller or is not correctly initialized.");
            }

            HealthModule = GetComponent<HealthModule>();
            if (!HealthModule)
            {
                HealthModule = gameObject.AddComponent<HealthModule>();
                UnityEngine.Debug.LogWarning("[Avatar] No HealthModule was found. Automatically attach a new one but you should add it manually !");
            }
        }

        #region Gameplay Event Callbacks

        /// <summary>
        /// Manages the wiring between the <c>AvatarController</c>'s raw « <i>input events</i> » and the <c>Avatar</c>'s « <i>player action events</i> ».
        /// </summary>
        /// <param name="unsubscribe">Should the method rather un-subscribe the events.</param>
        private void ManageEventCallbacks(bool unsubscribe = false)
        {
            if (unsubscribe)
            {
                _inputController.UnsubscribeFromInputActions();

                _inputController.OnMoveInput -= OnMoveActionCallback;
                _inputController.OnMoveEndInput -= OnMoveEndActionCallback;
                _inputController.OnJumpInput -= OnJumpActionCallback;
                _inputController.OnDashInput -= OnDashActionCallback;
                _inputController.OnGuardInput -= OnGuardActionCallback;
                _inputController.OnAttackLightInput -= OnAttackLightActionCallback;
                _inputController.OnAttackHeavyInput -= OnAttackHeavyActionCallback;
                _inputController.OnSpecialInput -= OnSpecialActionCallback;
                _inputController.OnInteractInput -= OnInteractActionCallback;
            }
            else
            {
                _inputController.SubscribeToInputActions();

                _inputController.OnMoveInput += OnMoveActionCallback;
                _inputController.OnMoveEndInput -= OnMoveEndActionCallback;
                _inputController.OnJumpInput += OnJumpActionCallback;
                _inputController.OnDashInput += OnDashActionCallback;
                _inputController.OnGuardInput += OnGuardActionCallback;
                _inputController.OnAttackLightInput += OnAttackLightActionCallback;
                _inputController.OnAttackHeavyInput += OnAttackHeavyActionCallback;
                _inputController.OnSpecialInput += OnSpecialActionCallback;
                _inputController.OnInteractInput += OnInteractActionCallback;
            }
        }

        private void OnMoveActionCallback(InputAction.CallbackContext ctx) => OnMove?.Invoke(ctx);
        private void OnMoveEndActionCallback(InputAction.CallbackContext ctx) => OnMoveEnd?.Invoke(ctx);
        private void OnJumpActionCallback(InputAction.CallbackContext ctx) => OnJump?.Invoke(ctx);
        private void OnDashActionCallback(InputAction.CallbackContext ctx) => OnDash?.Invoke(ctx);
        private void OnGuardActionCallback(InputAction.CallbackContext ctx) => OnGuard?.Invoke(ctx);
        private void OnAttackLightActionCallback(InputAction.CallbackContext ctx) => OnAttackLight?.Invoke(ctx);
        private void OnAttackHeavyActionCallback(InputAction.CallbackContext ctx) => OnAttackHeavy?.Invoke(ctx);
        private void OnSpecialActionCallback(InputAction.CallbackContext ctx) => OnSpecial?.Invoke(ctx);
        private void OnInteractActionCallback(InputAction.CallbackContext ctx) => OnInteract?.Invoke(ctx);

        #endregion
    }
}
