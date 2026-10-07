using System;
using SynthOfRage.Scripts.Common.Modules;
using SynthOfRage.Scripts.Gameplay.Unit.Player.Movement.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
{
    /// <summary>
    /// The main class representing the Player.<br/>
    /// It offers « player actions events » to subscribe to and manages the components that handle the visuals.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Avatar : UnitBase
    {
        #region Player Actions Events

        /// <summary>
        /// This is a « <b><i>player action</i></b> ». It relays its counterpart « <i>input event</i> » from the <c>AvatarController</c>.<br/>
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

        #region Other Gameplay Events

        public event Action OnHurt;

        #endregion

        [Header("Component References")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private MovementSpace _currentMovementSpace;
        [SerializeField] private Animator _animator;

        [Header("State Machine")]
        [SerializeField] private AvatarFSM _stateMachine;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _dashDistance = 4f;
        [SerializeField] private float _dashDuration = 0.2f;
        [SerializeField] private float _dashCooldown = 0.5f;

        private AvatarController _controller;
        private HealthModule _healthModule;

        public int HP => _healthModule.HP;
        public int MaxHP => _healthModule.MaxHP;

        // Public accessors for states
        public CharacterController CharacterController => _characterController;
        public MovementSpace CurrentMovementSpace { get => _currentMovementSpace; set => _currentMovementSpace = value; }
        public Animator Animator => _animator;
        public float MoveSpeed => _moveSpeed;
        public float JumpHeight => _jumpHeight;
        public float Gravity => _gravity;
        public float DashDistance => _dashDistance;
        public float DashDuration => _dashDuration;
        public float DashCooldown => _dashCooldown;

        private void Awake()
        {
            _stateMachine = new (this);

            _controller = new ();
            _controller.Initialize();

            // Get component references if not assigned in inspector
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();

            if (_currentMovementSpace == null)
                _currentMovementSpace = GetComponentInChildren<MovementSpace>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (!(_healthModule = GetComponent<HealthModule>()))
            {
                _healthModule = gameObject.AddComponent<HealthModule>();
                UnityEngine.Debug.LogWarning("[Avatar] No HealthModule was found. Automatically attach a new one but you should add it manually !");
            }

            if (_animator == null)
            {
                // TODO: Automatically add the component and use <Addressable> package to link the runtime controller.
                throw new NullReferenceException("[Avatar] No Animator component was found.");
            }
            else if (!(_animator.isInitialized && _animator.runtimeAnimatorController))
            {
                UnityEngine.Debug.LogError("[Avatar] Animator is missing a controller or is not correctly initialized.");
            }
        }

        private void Start()
        {
            _stateMachine.Initialize();
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        private void OnEnable()
        {
            ManageEventCallbacks(false);
        }

        private void OnDisable()
        {
            ManageEventCallbacks(true);
        }

        public void TakeDamage(int amount)
        {
            _healthModule.Apply(-Mathf.Abs(amount));
            OnHurt?.Invoke();
        }

        /// <summary>
        /// Manages the wiring between the <c>AvatarController</c>'s raw « <i>input events</i> » and the <c>Avatar</c>'s « <i>player actions events</i> ».
        /// </summary> E
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

        #region Player Action Callbacks

        private void OnMoveGameplayCallback(InputAction.CallbackContext ctx) => OnMove?.Invoke(ctx);
        private void OnJumpGameplayCallback(InputAction.CallbackContext ctx) => OnJump?.Invoke(ctx);
        private void OnDashGameplayCallback(InputAction.CallbackContext ctx) => OnDash?.Invoke(ctx);
        private void OnGuardGameplayCallback(InputAction.CallbackContext ctx) => OnGuard?.Invoke(ctx);
        private void OnAttackLightGameplayCallback(InputAction.CallbackContext ctx) => OnAttackLight?.Invoke(ctx);
        private void OnAttackHeavyGameplayCallback(InputAction.CallbackContext ctx) => OnAttackHeavy?.Invoke(ctx);
        private void OnSpecialGameplayCallback(InputAction.CallbackContext ctx) => OnSpecial?.Invoke(ctx);
        private void OnInteractGameplayCallback(InputAction.CallbackContext ctx) => OnInteract?.Invoke(ctx);

        #endregion

        // ------------------------ //

        #region Debug

#if UNITY_EDITOR || UNITY_USE_INSTRUMENTATION

        public HealthModule HealthModuleDebug => _healthModule;

#endif

        #endregion
    }
}
