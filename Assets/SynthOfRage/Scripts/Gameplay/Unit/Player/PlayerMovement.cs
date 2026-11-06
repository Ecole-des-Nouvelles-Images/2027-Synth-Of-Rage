using UnityEngine;
using UnityEngine.Serialization;
using SynthOfRage.Scripts.Gameplay.Common.Modules;
using SynthOfRage.Scripts.Gameplay.Core;
using SynthOfRage.Scripts.Gameplay.Unit.Player.Movement.Utils;
using Debug = UnityEngine.Debug;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int IsJumping = Animator.StringToHash("IsJumping");
        private static readonly int IsDashing = Animator.StringToHash("IsDashing");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");

        [Header("References")]
        [FormerlySerializedAs("playerObserver")]
        [SerializeField] private PlayerObserver _playerObserver;

        [FormerlySerializedAs("spriteRenderer")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [FormerlySerializedAs("movementSpace")]
        [SerializeField] private Transform _movementSpace;

        [Header("Dev Settings")]
        [FormerlySerializedAs("debugEnabled")]
        [SerializeField] private bool _debugEnabled;

        [Header("Movement")]
        [FormerlySerializedAs("moveSpeed")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Movement Space Rotation")]
        [FormerlySerializedAs("followMovementSpaceRotation")]
        [SerializeField] private bool _followMovementSpaceRotation = true;

        [FormerlySerializedAs("movementSpaceRotationSmoothTime")]
        [SerializeField] private float _movementSpaceRotationSmoothTime = 0.15f;

        [Header("Movement Space Blend")]
        [Tooltip("Variation maximale du T de blend par seconde. 0 = aucune limitation.")]
        [SerializeField, Min(0f)] private float _blendMaxProgressSpeed = 3f;

        [Tooltip("Temps pendant lequel on conserve le dernier T valide si la géométrie du blend perd le joueur pendant quelques frames.")]
        [SerializeField, Min(0f)] private float _blendMissingSampleGraceTime = 0.08f;

        [Header("Jump")]
        [FormerlySerializedAs("jumpHeight")]
        [SerializeField] private float _jumpHeight = 2f;

        [FormerlySerializedAs("gravity")]
        [SerializeField] private float _gravity = -20f;

        [FormerlySerializedAs("jumpCurve")]
        [SerializeField] private AnimationCurve _jumpCurve = new(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f));

        [FormerlySerializedAs("jumpDurationMultiplier")]
        [SerializeField] private float _jumpDurationMultiplier = 1f;

        [Header("Dash")]
        [FormerlySerializedAs("dashDistance")]
        [SerializeField] private float _dashDistance = 4f;

        [FormerlySerializedAs("dashDuration")]
        [SerializeField] private float _dashDuration = 0.2f;

        [FormerlySerializedAs("dashCooldown")]
        [SerializeField] private float _dashCooldown = 0.5f;

        [FormerlySerializedAs("dashCurve")]
        [SerializeField] private AnimationCurve _dashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Rhythm - Dash (Optional)")]
        [Tooltip("When enabled, dash duration and cooldown are controlled exclusively by RhythmManager beats.")]
        [SerializeField] private bool _syncDashToBeats = true;

        [SerializeField, Min(0.01f)] private float _dashDurationBeats = 0.5f;
        [SerializeField, Min(0f)] private float _dashCooldownBeats = 1f;

        // =========================================================
        // RHYTHM - DASH ON BEAT REWARD
        // =========================================================

        [Header("Rhythm - Dash On Beat Reward")]
        [Tooltip("Musical grid used to judge the dash. 1 = beat, 0.5 = half-beat, 0.25 = quarter-beat.")]
        [SerializeField, Min(0.01f)] private float _dashOnBeatEveryBeats = 1f;

        [Tooltip("Maximum distance from the nearest grid point to qualify as On-Beat, in beats.")]
        [SerializeField, Min(0f)] private float _dashOnBeatThresholdBeats = 0.15f;

        [Tooltip("VFX instantiated when the dash starts On-Beat.")]
        [SerializeField] private GameObject _dashOnBeatVfx;

        [Tooltip("Position offset applied to the dash VFX, in world space.")]
        [SerializeField] private Vector3 _dashOnBeatVfxOffset;

        [Tooltip("Use the player's rotation when instantiating the dash VFX.")]
        [SerializeField] private bool _dashOnBeatVfxUsePlayerRotation = true;

        /// <summary>
        /// Indicates whether the most recently started dash was judged On-Beat.
        /// </summary>
        public bool IsLastDashOnBeat { get; private set; }

        private CharacterController _controller;
        private Animator _animator;
        private HealthModule _healthModule;

        private MovementSpace _currentMovementSpace;

        private bool _isMovementSpaceBlending;
        private MovementSpace _blendSourceMovementSpace;
        private MovementSpace _blendTargetMovementSpace;
        private MovementSpace.Boundary _blendSourceBoundary;

        private float _stableBlendT;
        private float _blendMissingSampleTimer;

        private bool _hasStableBlendResult;
        private MovementSpace.BlendResult _stableBlendResult;

        private float _dashCooldownTimer;
        private bool _dashCooldownUsesRhythm;

        private Vector3 _dashDirection;
        private float _dashTimer;
        private bool _isDashing;
        private bool _dashUsingRhythm;

        private double _dashStartBeat;
        private double _nextDashAvailableBeat;

        private float _dashPreviousProgress;
        private float _dashActiveDurationSeconds;

        private bool _isJumping;
        private float _jumpDuration;
        private float _jumpTimer;

        private Vector2 _moveInput;
        private float _movementSpaceRotationVelocity;
        private float _previousJumpHeight;
        private float _verticalVelocity;

        // [ELEVATOR] Indique que le joueur est actuellement transporté par une cabine.
        // Le CharacterController reste actif : le joueur conserve son contrôle.
        private bool _isElevatorTransportActive;

        public bool IsMovementSpaceBlending => _isMovementSpaceBlending;
        public bool IsElevatorTransportLocked => false;
        public bool IsElevatorTransportActive => _isElevatorTransportActive;

        // =========================================================
        // UNITY LIFECYCLE
        // =========================================================

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            _healthModule = GetComponent<HealthModule>();

            if (!_controller)
                UnityEngine.Debug.LogError("[PlayerMovement] CharacterController component is not found on Player !", this);

            if (!_animator)
                UnityEngine.Debug.LogError("[PlayerMovement] Animator component is not found on Player !", this);

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            CacheMovementSpaceComponent();
            CalculateJumpDuration();
        }

        private void Update()
        {
            HandleMovement();
            HandleJump();
            HandleGravity();
            HandleDashCooldown();
            HandleDash();

            UpdateMovementSpaceBlend();
            UpdateCharacterRotation();
        }

        private void OnEnable()
        {
            if (_healthModule)
                _healthModule.OnStunChanged += HandleStunChanged;

            if (_playerObserver == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(PlayerMovement)}] PlayerObserver reference is missing.", this);
                return;
            }

            _playerObserver.OnPlayerMove += HandlePlayerMove;
            _playerObserver.OnPlayerJump += HandlePlayerJump;
            _playerObserver.OnPlayerDash += HandlePlayerDash;
        }

        private void OnDisable()
        {
            if (_healthModule)
                _healthModule.OnStunChanged -= HandleStunChanged;

            if (_playerObserver == null)
                return;

            _playerObserver.OnPlayerMove -= HandlePlayerMove;
            _playerObserver.OnPlayerJump -= HandlePlayerJump;
            _playerObserver.OnPlayerDash -= HandlePlayerDash;
        }

        // =========================================================
        // INPUT
        // =========================================================

        private void HandlePlayerMove(Vector2 input)
        {
            if (_healthModule && _healthModule.IsStunned)
            {
                _moveInput = Vector2.zero;
                return;
            }

            _moveInput = input;

            if (_animator)
                _animator.SetBool(Move, _moveInput != Vector2.zero);

            UpdateSpriteDirection(input);

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] " + $"Move Input : {_moveInput}", this);
        }

        private void HandlePlayerJump()
        {
            if ((_healthModule && _healthModule.IsStunned) ||
                !_controller.isGrounded ||
                _isJumping ||
                _isDashing ||
                _jumpHeight <= 0f)
                return;

            CalculateJumpDuration();

            _isJumping = true;
            _jumpTimer = 0f;
            _previousJumpHeight = 0f;
            _verticalVelocity = 0f;

            if (_animator)
            {
                _animator.SetBool(IsJumping, true);
                SetAnimatorVerticalVelocity(0f);
            }
        }

        private void HandlePlayerDash()
        {
            if ((_healthModule && _healthModule.IsStunned) || _isDashing)
                return;

            RhythmManager rhythm = null;
            bool rhythmAvailable = false;

            // In rhythm mode, the central clock is mandatory.
            // No seconds-based fallback is allowed if it is unavailable.
            if (_syncDashToBeats)
            {
                rhythm = RhythmManager.Instance;

                if (!rhythm || !rhythm.IsReady)
                    return;

                rhythmAvailable = true;

                if (_dashCooldownUsesRhythm &&
                    rhythm.CurrentBeat + 1e-6 < _nextDashAvailableBeat)
                    return;
            }
            else
            {
                if (_dashCooldownTimer > 0f)
                    return;

                // The dash can still be judged On-Beat even when its duration
                // and cooldown use seconds instead of beats.
                rhythm = RhythmManager.Instance;
                rhythmAvailable = rhythm && rhythm.IsReady;
            }

            // Reset the previous judgement before evaluating this dash.
            IsLastDashOnBeat = false;

            if (rhythmAvailable)
                IsLastDashOnBeat = EvaluateDashOnBeat(rhythm.CurrentBeat);

            if (_isJumping)
            {
                _isJumping = false;
                _jumpTimer = 0f;
                _previousJumpHeight = 0f;
                _verticalVelocity = 0f;

                if (_animator)
                {
                    _animator.SetBool(IsJumping, false);
                    SetAnimatorVerticalVelocity(0f);
                }
            }

            _dashDirection = _moveInput.sqrMagnitude > 0.01f
                ? GetMovementDirection(_moveInput)
                : _spriteRenderer != null && _spriteRenderer.flipX
                    ? -transform.right
                    : transform.right;

            _dashDirection.y = 0f;
            _dashDirection.Normalize();

            _dashTimer = 0f;
            _dashPreviousProgress = 0f;
            _isDashing = true;
            _dashUsingRhythm = _syncDashToBeats && rhythmAvailable;

            if (_dashUsingRhythm)
            {
                _dashStartBeat = rhythm.CurrentBeat;
                _nextDashAvailableBeat = _dashStartBeat + _dashCooldownBeats;

                _dashActiveDurationSeconds = Mathf.Max(
                    0.001f,
                    (float)rhythm.BeatsToSeconds(_dashDurationBeats));

                // The cooldown is tracked by the beat clock, not by a seconds timer.
                _dashCooldownUsesRhythm = true;
                _dashCooldownTimer = 0f;
            }
            else
            {
                _dashActiveDurationSeconds = Mathf.Max(0.001f, _dashDuration);

                _dashCooldownUsesRhythm = false;
                _dashCooldownTimer = _dashCooldown;
            }

            if (_animator)
            {
                _animator.SetBool(IsDashing, true);
                SetAnimatorVerticalVelocity(0f);
            }

            // Dash On-Beat VFX is spawned at the beginning of the dash.
            if (IsLastDashOnBeat && _dashOnBeatVfx)
            {
                Quaternion rotation = _dashOnBeatVfxUsePlayerRotation
                    ? transform.rotation
                    : Quaternion.identity;

                Instantiate(
                    _dashOnBeatVfx,
                    transform.position + _dashOnBeatVfxOffset,
                    rotation);
            }

            if (_debugEnabled)
            {
                UnityEngine.Debug.Log(
                    _dashUsingRhythm
                        ? $"[PlayerMovement] Dash started | Beat: {_dashStartBeat:F3} | Duration: {_dashDurationBeats:F2} beats | Cooldown: {_dashCooldownBeats:F2} beats | On-Beat: {IsLastDashOnBeat}."
                        : $"[PlayerMovement] Dash started | Duration: {_dashDuration:F2}s | Cooldown: {_dashCooldown:F2}s | On-Beat: {IsLastDashOnBeat}.",
                    this);
            }
        }

        /// <summary>
        /// Judges the dash against the nearest point on its configured musical grid.
        /// The central RhythmManager is the only source of beat time.
        /// </summary>
        private bool EvaluateDashOnBeat(double currentBeat)
        {
            if (_dashOnBeatEveryBeats <= 0f)
                return false;

            double gridIndex = System.Math.Round(
                currentBeat / _dashOnBeatEveryBeats);

            double nearestGridBeat = gridIndex * _dashOnBeatEveryBeats;
            double distanceToGrid = System.Math.Abs(
                currentBeat - nearestGridBeat);

            bool onBeat =
                distanceToGrid <= _dashOnBeatThresholdBeats + 1e-6;

            if (_debugEnabled)
            {
                UnityEngine.Debug.Log(
                    $"[PlayerMovement] Dash rhythm judgement | " +
                    $"Beat: {currentBeat:F3} | " +
                    $"Nearest grid: {nearestGridBeat:F3} | " +
                    $"Distance: {distanceToGrid:F3} | " +
                    $"Threshold: {_dashOnBeatThresholdBeats:F3} | " +
                    $"{(onBeat ? "ON BEAT" : "OFF BEAT")}.",
                    this);
            }

            return onBeat;
        }

        // =========================================================
        // ELEVATOR TRANSPORT
        // =========================================================

        /// <summary>
        /// Active le transport de cabine sans désactiver le CharacterController.
        /// Le joueur conserve son déplacement normal pendant toute la montée.
        /// </summary>
        public bool BeginElevatorTransport()
        {
            if (!_controller || !_controller.enabled)
                return false;

            _isElevatorTransportActive = true;

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] Transport ascenseur démarré.", this);

            return true;
        }

        /// <summary>
        /// Applique le déplacement de la cabine au joueur via son CharacterController.
        /// Cette méthode ne remplace pas le déplacement normal du joueur.
        /// </summary>
        public void ApplyElevatorTransportMovement(Vector3 movement)
        {
            if (!_isElevatorTransportActive ||
                !_controller ||
                !_controller.enabled ||
                movement.sqrMagnitude <= 0f)
                return;

            _controller.Move(movement);
        }

        /// <summary>
        /// Termine le transport de cabine. Le CharacterController est déjà actif.
        /// </summary>
        public void EndElevatorTransport()
        {
            if (!_isElevatorTransportActive)
                return;

            _isElevatorTransportActive = false;

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] Transport ascenseur terminé.", this);
        }

        // =========================================================
        // MOVEMENT
        // =========================================================

        private void HandleStunChanged(bool stunned)
        {
            if (!stunned)
                return;

            _moveInput = Vector2.zero;

            _isDashing = false;
            _dashTimer = 0f;
            _dashPreviousProgress = 0f;
            _dashUsingRhythm = false;

            if (_animator)
            {
                _animator.SetBool(Move, false);
                _animator.SetBool(IsDashing, false);
            }
        }

        private void HandleMovement()
        {
            if (_isDashing || (_healthModule && _healthModule.IsStunned))
                return;

            MoveWithMovementSpaceLimits(
                GetMovementDirection(_moveInput) * (_moveSpeed * Time.deltaTime));
        }

        private Vector3 GetMovementDirection(Vector2 input)
        {
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            direction.y = 0f;

            return Vector3.ClampMagnitude(direction, 1f);
        }

        private void UpdateSpriteDirection(Vector2 input)
        {
            if (_spriteRenderer == null)
                return;

            if (input.x < -0.01f)
                _spriteRenderer.flipX = true;
            else if (input.x > 0.01f)
                _spriteRenderer.flipX = false;
        }

        // =========================================================
        // BLEND
        // =========================================================

        private void UpdateMovementSpaceBlend()
        {
            if (_isMovementSpaceBlending)
            {
                UpdateActiveMovementSpaceBlend();
                return;
            }

            if (_currentMovementSpace != null &&
                _currentMovementSpace.TryGetAnyBlend(
                    transform.position,
                    out MovementSpace.BlendResult result))
            {
                BeginMovementSpaceBlend(result);
            }
        }

        private void BeginMovementSpaceBlend(MovementSpace.BlendResult result)
        {
            if (result.Source == null || result.Target == null)
                return;

            _isMovementSpaceBlending = true;
            _blendSourceMovementSpace = result.Source;
            _blendTargetMovementSpace = result.Target;
            _blendSourceBoundary = result.SourceBoundary;

            _stableBlendT = Mathf.Min(
                result.T,
                GetCurrentRotationBlendT(result.Source, result.Target));

            _blendMissingSampleTimer = 0f;

            SetStableBlendResult(result);

            if (_debugEnabled)
            {
                UnityEngine.Debug.Log(
                    "[PlayerMovement] " +
                    $"Blend commencé | {result.Source.name} -> {result.Target.name} | " +
                    $"Raw={result.T:F3} | Stable={_stableBlendT:F3}",
                    this);
            }
        }

        private void UpdateActiveMovementSpaceBlend()
        {
            if (_blendSourceMovementSpace == null ||
                _blendTargetMovementSpace == null)
            {
                ClearMovementSpaceBlend();
                return;
            }

            if (_blendSourceMovementSpace.TryGetBlend(
                _blendSourceBoundary,
                transform.position,
                out MovementSpace.BlendResult result))
            {
                _blendMissingSampleTimer = 0f;

                _blendTargetMovementSpace = result.Target;

                MoveStableBlendT(result.T);
                SetStableBlendResult(result);

                return;
            }

            _blendMissingSampleTimer += Time.deltaTime;

            if (_blendMissingSampleTimer <= _blendMissingSampleGraceTime &&
                _hasStableBlendResult)
                return;

            float terminalT;

            if (_currentMovementSpace == _blendTargetMovementSpace)
                terminalT = 1f;
            else if (_currentMovementSpace == _blendSourceMovementSpace)
                terminalT = 0f;
            else
                terminalT = _stableBlendT >= 0.5f ? 1f : 0f;

            MoveStableBlendT(terminalT);

            if (_hasStableBlendResult)
                SetStableBlendResult(_stableBlendResult);

            if (!Mathf.Approximately(_stableBlendT, terminalT))
                return;

            if (_debugEnabled)
            {
                UnityEngine.Debug.Log(
                    "[PlayerMovement] " +
                    $"Blend terminé | {_blendSourceMovementSpace.name} -> {_blendTargetMovementSpace.name}",
                    this);
            }

            ClearMovementSpaceBlend();
        }

        private void MoveStableBlendT(float targetT)
        {
            targetT = Mathf.Clamp01(targetT);

            if (_blendMaxProgressSpeed <= 0f)
            {
                _stableBlendT = targetT;
                return;
            }

            _stableBlendT = Mathf.MoveTowards(
                _stableBlendT,
                targetT,
                _blendMaxProgressSpeed * Time.deltaTime);
        }

        private void SetStableBlendResult(MovementSpace.BlendResult sourceResult)
        {
            _stableBlendResult = sourceResult;
            _stableBlendResult.T = _stableBlendT;

            _stableBlendResult.Rotation = Quaternion.Slerp(
                sourceResult.Source.transform.rotation,
                sourceResult.Target.transform.rotation,
                _stableBlendT);

            _hasStableBlendResult = true;
        }

        private float GetCurrentRotationBlendT(MovementSpace source, MovementSpace target)
        {
            float sourceY = source.transform.eulerAngles.y;
            float targetY = target.transform.eulerAngles.y;

            float totalAngle = Mathf.Abs(Mathf.DeltaAngle(sourceY, targetY));

            if (totalAngle <= 0.001f)
                return 0f;

            float currentAngle = Mathf.Abs(
                Mathf.DeltaAngle(sourceY, transform.eulerAngles.y));

            return Mathf.Clamp01(currentAngle / totalAngle);
        }

        public bool TryGetActiveMovementSpaceBlend(
            out MovementSpace.BlendResult result)
        {
            result = default;

            if (!_isMovementSpaceBlending || !_hasStableBlendResult)
                return false;

            result = _stableBlendResult;
            return true;
        }

        public Quaternion GetEffectiveMovementSpaceRotation()
        {
            if (_isMovementSpaceBlending && _hasStableBlendResult)
                return _stableBlendResult.Rotation;

            return _currentMovementSpace != null
                ? _currentMovementSpace.transform.rotation
                : Quaternion.identity;
        }

        public float GetEffectiveMovementSpaceY() =>
            GetEffectiveMovementSpaceRotation().eulerAngles.y;

        private void ClearMovementSpaceBlend()
        {
            _isMovementSpaceBlending = false;
            _blendSourceMovementSpace = null;
            _blendTargetMovementSpace = null;

            _stableBlendT = 0f;
            _blendMissingSampleTimer = 0f;

            _hasStableBlendResult = false;
            _stableBlendResult = default;

            _movementSpaceRotationVelocity = 0f;
        }

        // =========================================================
        // ROTATION
        // =========================================================

        private void UpdateCharacterRotation()
        {
            if (!_followMovementSpaceRotation || !_movementSpace)
                return;

            if (_isMovementSpaceBlending && _hasStableBlendResult)
            {
                transform.rotation = Quaternion.Euler(
                    0f,
                    _stableBlendResult.Rotation.eulerAngles.y,
                    0f);

                _movementSpaceRotationVelocity = 0f;
                return;
            }

            float targetY = _movementSpace.eulerAngles.y;
            float currentY = transform.eulerAngles.y;

            float smoothedY = Mathf.SmoothDampAngle(
                currentY,
                targetY,
                ref _movementSpaceRotationVelocity,
                _movementSpaceRotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, smoothedY, 0f);
        }

        private void SetAnimatorVerticalVelocity(float velocity)
        {
            if (!_animator)
                return;

            float maxReference = Mathf.Max(_jumpHeight, 0.01f);
            float clampedVelocity = Mathf.Clamp(
                velocity,
                -maxReference,
                maxReference);

            _animator.SetFloat(VerticalVelocity, clampedVelocity);
        }

        // =========================================================
        // JUMP / GRAVITY
        // =========================================================

        private void CalculateJumpDuration()
        {
            if (_gravity >= 0f)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(PlayerMovement)}] Gravity must be negative.",
                    this);

                _jumpDuration = 0.1f;
                return;
            }

            float jumpVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            float physicalJumpDuration = -2f * jumpVelocity / _gravity;

            _jumpDuration = Mathf.Max(
                physicalJumpDuration * _jumpDurationMultiplier,
                0.01f);
        }

        private void HandleJump()
        {
            if (_isDashing || !_isJumping)
                return;

            float progress = Mathf.Clamp01(_jumpTimer / _jumpDuration);
            float currentHeight = _jumpCurve.Evaluate(progress) * _jumpHeight;
            float deltaHeight = currentHeight - _previousJumpHeight;

            float previousY = transform.position.y;

            MoveWithMovementSpaceLimits(Vector3.up * deltaHeight);

            float actualDeltaHeight = transform.position.y - previousY;
            float actualVerticalVelocity = actualDeltaHeight /
                                           Mathf.Max(Time.deltaTime, 0.0001f);

            _verticalVelocity = actualVerticalVelocity;
            _previousJumpHeight = currentHeight;
            _jumpTimer += Time.deltaTime;

            SetAnimatorVerticalVelocity(actualVerticalVelocity);

            if (_jumpTimer >= _jumpDuration)
                EndJump();
        }

        private void EndJump()
        {
            _isJumping = false;
            _jumpTimer = 0f;
            _previousJumpHeight = 0f;
            _verticalVelocity = -2f;

            if (_animator)
                _animator.SetBool(IsJumping, false);
        }

        private void HandleGravity()
        {
            if (_isDashing || _isJumping)
                return;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -2f;
                SetAnimatorVerticalVelocity(0f);
                return;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            float previousY = transform.position.y;

            MoveWithMovementSpaceLimits(
                Vector3.up * (_verticalVelocity * Time.deltaTime)
            );

            float actualDeltaHeight = transform.position.y - previousY;

            // Si la gravité veut nous faire descendre mais que le CharacterController
            // n'a réellement pas bougé, on est posé sur quelque chose même si
            // isGrounded a raté cette frame.
            if (_verticalVelocity < 0f &&
                Mathf.Abs(actualDeltaHeight) <= 0.0001f)
            {
                _verticalVelocity = -2f;
                SetAnimatorVerticalVelocity(0f);
                return;
            }

            float actualVerticalVelocity =
                actualDeltaHeight / Mathf.Max(Time.deltaTime, 0.0001f);

            SetAnimatorVerticalVelocity(actualVerticalVelocity);
        }

        // =========================================================
        // DASH
        // =========================================================

        private void HandleDash()
        {
            if (!_isDashing)
                return;

            float currentProgress;

            if (_dashUsingRhythm)
            {
                RhythmManager rhythm = RhythmManager.Instance;

                // Never switch to seconds if the central clock disappears.
                // End the dash cleanly instead.
                if (!rhythm || !rhythm.IsReady)
                {
                    EndDash();
                    return;
                }

                currentProgress = Mathf.Clamp01(
                    (float)(
                        (rhythm.CurrentBeat - _dashStartBeat) /
                        Mathf.Max(0.01f, _dashDurationBeats)));
            }
            else
            {
                _dashTimer += Time.deltaTime;

                currentProgress = Mathf.Clamp01(
                    _dashTimer / Mathf.Max(0.001f, _dashActiveDurationSeconds));
            }

            currentProgress = Mathf.Max(
                currentProgress,
                _dashPreviousProgress);

            float deltaCurve =
                _dashCurve.Evaluate(currentProgress) -
                _dashCurve.Evaluate(_dashPreviousProgress);

            MoveWithMovementSpaceLimits(
                _dashDirection * (_dashDistance * deltaCurve));

            _dashPreviousProgress = currentProgress;

            if (currentProgress < 1f)
                return;

            EndDash();
        }

        private void EndDash()
        {
            _dashTimer = 0f;
            _dashPreviousProgress = 0f;
            _isDashing = false;
            _dashUsingRhythm = false;
            _verticalVelocity = -2f;

            if (_animator)
            {
                _animator.SetBool(IsDashing, false);
                SetAnimatorVerticalVelocity(0f);
            }

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] Dash ended.", this);
        }

        private void HandleDashCooldown()
        {
            if (_dashCooldownUsesRhythm)
            {
                // A beat-based cooldown remains locked if the clock is unavailable.
                if (!_syncDashToBeats)
                {
                    _dashCooldownUsesRhythm = false;
                    _dashCooldownTimer = 0f;
                    return;
                }

                RhythmManager rhythm = RhythmManager.Instance;

                if (!rhythm || !rhythm.IsReady)
                    return;

                if (rhythm.CurrentBeat + 1e-6 >= _nextDashAvailableBeat)
                    _dashCooldownUsesRhythm = false;

                return;
            }

            if (_dashCooldownTimer <= 0f)
                return;

            _dashCooldownTimer = Mathf.Max(
                0f,
                _dashCooldownTimer - Time.deltaTime);
        }

        // =========================================================
        // MOVEMENT SPACE LIMITS
        // =========================================================

        /// <summary>
        /// Applies external displacement (such as knockback) while respecting MovementSpace boundaries.
        /// </summary>
        public void ApplyExternalMovement(Vector3 movement)
        {
            if (!_controller ||
                !_controller.enabled ||
                movement.sqrMagnitude <= 0f)
                return;

            MoveWithMovementSpaceLimits(movement);
        }

        private void MoveWithMovementSpaceLimits(Vector3 movement)
        {
            if (!_controller || !_controller.enabled)
                return;

            if (!_movementSpace)
            {
                _controller.Move(movement);
                return;
            }

            Vector3 targetPosition = transform.position + movement;
            Vector3 localTargetPosition = _movementSpace.InverseTransformPoint(targetPosition);

            if (_currentMovementSpace)
            {
                MovementSpace connectedMovementSpace = GetConnectedMovementSpace(
                    localTargetPosition,
                    out MovementSpace.Boundary crossedBoundary);

                if (connectedMovementSpace)
                {
                    if (!_isMovementSpaceBlending &&
                        _currentMovementSpace.TryGetBlend(
                            crossedBoundary,
                            targetPosition,
                            out MovementSpace.BlendResult blendResult))
                    {
                        BeginMovementSpaceBlend(blendResult);
                    }

                    SetMovementSpaceInternal(
                        connectedMovementSpace.transform,
                        false);

                    localTargetPosition =
                        _movementSpace.InverseTransformPoint(targetPosition);
                }

                if (_currentMovementSpace.UseDepthMin &&
                    localTargetPosition.z < _currentMovementSpace.MinDepth)
                    localTargetPosition.z = _currentMovementSpace.MinDepth;

                if (_currentMovementSpace.UseDepthMax &&
                    localTargetPosition.z > _currentMovementSpace.MaxDepth)
                    localTargetPosition.z = _currentMovementSpace.MaxDepth;

                if (_currentMovementSpace.UseSideLeft &&
                    localTargetPosition.x < _currentMovementSpace.MinSide)
                    localTargetPosition.x = _currentMovementSpace.MinSide;

                if (_currentMovementSpace.UseSideRight &&
                    localTargetPosition.x > _currentMovementSpace.MaxSide)
                    localTargetPosition.x = _currentMovementSpace.MaxSide;
            }

            targetPosition = _movementSpace.TransformPoint(localTargetPosition);

            _controller.Move(targetPosition - transform.position);
        }

        private MovementSpace GetConnectedMovementSpace(
            Vector3 localTargetPosition,
            out MovementSpace.Boundary crossedBoundary)
        {
            crossedBoundary = default;

            if (!_currentMovementSpace)
                return null;

            if (!_currentMovementSpace.UseDepthMin &&
                localTargetPosition.z < _currentMovementSpace.MinDepth)
            {
                crossedBoundary = MovementSpace.Boundary.DepthMin;
                return _currentMovementSpace.DepthMinConnection;
            }

            if (!_currentMovementSpace.UseDepthMax &&
                localTargetPosition.z > _currentMovementSpace.MaxDepth)
            {
                crossedBoundary = MovementSpace.Boundary.DepthMax;
                return _currentMovementSpace.DepthMaxConnection;
            }

            if (!_currentMovementSpace.UseSideLeft &&
                localTargetPosition.x < _currentMovementSpace.MinSide)
            {
                crossedBoundary = MovementSpace.Boundary.SideLeft;
                return _currentMovementSpace.SideLeftConnection;
            }

            if (!_currentMovementSpace.UseSideRight &&
                localTargetPosition.x > _currentMovementSpace.MaxSide)
            {
                crossedBoundary = MovementSpace.Boundary.SideRight;
                return _currentMovementSpace.SideRightConnection;
            }

            return null;
        }

        private void CacheMovementSpaceComponent()
        {
            if (!_movementSpace)
            {
                _currentMovementSpace = null;
                return;
            }

            _currentMovementSpace = _movementSpace.GetComponent<MovementSpace>();

            if (!_currentMovementSpace)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(PlayerMovement)}] Le Movement Space '{_movementSpace.name}' ne possède pas de composant MovementSpace.",
                    _movementSpace);
            }
        }

        public void SetMovementSpace(Transform newMovementSpace)
        {
            if (!newMovementSpace)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(PlayerMovement)}] Impossible d'assigner un Movement Space null.",
                    this);

                return;
            }

            ClearMovementSpaceBlend();
            SetMovementSpaceInternal(newMovementSpace, false);
        }

        private void SetMovementSpaceInternal(
            Transform newMovementSpace,
            bool clearBlend)
        {
            if (!newMovementSpace)
                return;

            if (clearBlend)
                ClearMovementSpaceBlend();

            _movementSpace = newMovementSpace;
            _movementSpaceRotationVelocity = 0f;

            CacheMovementSpaceComponent();

            if (_debugEnabled)
            {
                UnityEngine.Debug.Log(
                    "[PlayerMovement] " +
                    "Movement Space changé : " +
                    $"{_movementSpace.name}",
                    this);
            }
        }

        public Transform GetMovementSpace() => _movementSpace;

        // =========================================================
        // INSPECTOR VALIDATION
        // =========================================================

        private void OnValidate()
        {
            _dashDurationBeats = Mathf.Max(0.01f, _dashDurationBeats);
            _dashCooldownBeats = Mathf.Max(0f, _dashCooldownBeats);

            _dashOnBeatEveryBeats = Mathf.Max(0.01f, _dashOnBeatEveryBeats);
            _dashOnBeatThresholdBeats = Mathf.Max(0f, _dashOnBeatThresholdBeats);
        }
    }
}