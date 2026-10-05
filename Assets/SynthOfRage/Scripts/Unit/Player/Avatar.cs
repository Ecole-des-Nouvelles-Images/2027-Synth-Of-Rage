using System;
using SynthOfRage.Scripts.Common.Modules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Unit.Player
{
    /// <summary>
    /// The main class representing the Player.<br/>
    /// It offers « gameplay events » to subscribe to and manages the components that handle the visuals.
    /// </summary>
    public class Avatar : UnitBase
    {
        #region Gameplay Events

        /// <summary>
        /// This is a « <b><i>gameplay event</i></b> ». It relays its counterpart « <i>input event</i> » from the <c>AvatarController</c>.<br/>
        /// You can subscribe to these events freely. You can consider them as firing as soon the input is performed, for the time being.
        /// </summary>
        public event Action<InputAction.CallbackContext> OnMove;
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

        public HealthModule HealthModule { get; private set; }
        private readonly AvatarController _controller = new();
        private Animator _animator;

        private void Awake()
        {
            if ((HealthModule = GetComponent<HealthModule>()) == null)
            {
                HealthModule = gameObject.AddComponent<HealthModule>();
                UnityEngine.Debug.LogWarning("[Avatar] No HealthModule was found. Automatically attach a new one but you should add it manually !");
            }

            if ((_animator = GetComponentInChildren<Animator>()) == null)
            {
                // TODO: Automatically add the component and use <Addressable> package to link the runtime controller.
                throw new NullReferenceException("[Avatar] No Animator component was found.");
            }
            else if (!(_animator.isInitialized && _animator.runtimeAnimatorController != null))
            {
                UnityEngine.Debug.LogError("[Avatar] Animator is missing a controller or is not correctly initialized.");
            }
        }

        private void OnEnable()
        {
            ManageEventCallbacks(false);
        }

        private void OnDisable()
        {
            ManageEventCallbacks(true);
        }

        /// <summary>
        /// Manages the wiring between the <c>AvatarController</c>'s raw « <i>input events</i> » and the <c>Avatar</c>'s « <i>gameplay events</i> ».
        /// </summary>
        /// <param name="unsubscribe">Should the method rather un-subscribe the events.</param>
        private void ManageEventCallbacks(bool unsubscribe = false)
        {
            if (unsubscribe)
            {
                _controller.UnsubscribeFromInputActions();

                _controller.OnMoveInput -= OnMoveGameplayCallback;
                _controller.OnJumpInput -= OnJumpGameplayCallback;
                _controller.OnDashInput -= OnDashGameplayCallback;
                _controller.OnGuardInput -= OnGuardGameplayCallback;
                _controller.OnAttackLightInput -= OnAttackLightGameplayCallback;
                _controller.OnAttackHeavyInput -= OnAttackHeavyGameplayCallback;
                _controller.OnSpecialInput -= OnSpecialGameplayCallback;
                _controller.OnInteractInput -= OnInteractGameplayCallback;
            }
            else
            {
                _controller.SubscribeToInputActions();

                _controller.OnMoveInput += OnMoveGameplayCallback;
                _controller.OnJumpInput += OnJumpGameplayCallback;
                _controller.OnDashInput += OnDashGameplayCallback;
                _controller.OnGuardInput += OnGuardGameplayCallback;
                _controller.OnAttackLightInput += OnAttackLightGameplayCallback;
                _controller.OnAttackHeavyInput += OnAttackHeavyGameplayCallback;
                _controller.OnSpecialInput += OnSpecialGameplayCallback;
                _controller.OnInteractInput += OnInteractGameplayCallback;
            }
        }

        #region Gameplay Event Callbacks

        private void OnMoveGameplayCallback(InputAction.CallbackContext ctx) => OnMove?.Invoke(ctx);
        private void OnJumpGameplayCallback(InputAction.CallbackContext ctx) => OnJump?.Invoke(ctx);
        private void OnDashGameplayCallback(InputAction.CallbackContext ctx) => OnDash?.Invoke(ctx);
        private void OnGuardGameplayCallback(InputAction.CallbackContext ctx) => OnGuard?.Invoke(ctx);
        private void OnAttackLightGameplayCallback(InputAction.CallbackContext ctx) => OnAttackLight?.Invoke(ctx);
        private void OnAttackHeavyGameplayCallback(InputAction.CallbackContext ctx) => OnAttackHeavy?.Invoke(ctx);
        private void OnSpecialGameplayCallback(InputAction.CallbackContext ctx) => OnSpecial?.Invoke(ctx);
        private void OnInteractGameplayCallback(InputAction.CallbackContext ctx) => OnInteract?.Invoke(ctx);

        #endregion
    }
}
