using UnityEngine;
using UnityEngine.Serialization;
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
        [SerializeField] private AnimationCurve _jumpCurve = new(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

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

        private CharacterController _controller;
        private Animator _animator;

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
        private Vector3 _dashDirection;
        private float _dashTimer;
        private bool _isDashing;

        private bool _isJumping;
        private float _jumpDuration;
        private float _jumpTimer;

        private Vector2 _moveInput;
        private float _movementSpaceRotationVelocity;
        private float _previousJumpHeight;
        private float _verticalVelocity;

        public bool IsMovementSpaceBlending => _isMovementSpaceBlending;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();

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
            _moveInput = input;

            if (_animator)
                _animator.SetBool(Move, _moveInput != Vector2.zero);

            UpdateSpriteDirection(input);

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] " + $"Move Input : {_moveInput}", this);
        }

        private void HandlePlayerJump()
        {
            if (!_controller.isGrounded || _isJumping || _isDashing || _jumpHeight <= 0f)
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
            if (_isDashing || _dashCooldownTimer > 0f)
                return;

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

            _dashDirection = _moveInput.sqrMagnitude > 0.01f ? GetMovementDirection(_moveInput) : _spriteRenderer != null && _spriteRenderer.flipX ? -transform.right : transform.right;
            _dashDirection.y = 0f;
            _dashDirection.Normalize();

            _dashTimer = 0f;
            _isDashing = true;
            _dashCooldownTimer = _dashCooldown;

            if (_animator)
            {
                _animator.SetBool(IsDashing, true);
                SetAnimatorVerticalVelocity(0f);
            }
        }

        // =========================================================
        // MOVEMENT
        // =========================================================

        private void HandleMovement()
        {
            if (_isDashing)
                return;

            MoveWithMovementSpaceLimits(GetMovementDirection(_moveInput) * (_moveSpeed * Time.deltaTime));
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

            if (_currentMovementSpace != null && _currentMovementSpace.TryGetAnyBlend(transform.position, out MovementSpace.BlendResult result))
                BeginMovementSpaceBlend(result);
        }

        private void BeginMovementSpaceBlend(MovementSpace.BlendResult result)
        {
            if (result.Source == null || result.Target == null)
                return;

            _isMovementSpaceBlending = true;
            _blendSourceMovementSpace = result.Source;
            _blendTargetMovementSpace = result.Target;
            _blendSourceBoundary = result.SourceBoundary;

            _stableBlendT = Mathf.Min(result.T, GetCurrentRotationBlendT(result.Source, result.Target));
            _blendMissingSampleTimer = 0f;

            SetStableBlendResult(result);

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] " + $"Blend commencé | {result.Source.name} -> {result.Target.name} | Raw={result.T:F3} | Stable={_stableBlendT:F3}", this);
        }

        private void UpdateActiveMovementSpaceBlend()
        {
            if (_blendSourceMovementSpace == null || _blendTargetMovementSpace == null)
            {
                ClearMovementSpaceBlend();
                return;
            }

            if (_blendSourceMovementSpace.TryGetBlend(_blendSourceBoundary, transform.position, out MovementSpace.BlendResult result))
            {
                _blendMissingSampleTimer = 0f;

                _blendTargetMovementSpace = result.Target;

                MoveStableBlendT(result.T);
                SetStableBlendResult(result);

                return;
            }

            _blendMissingSampleTimer += Time.deltaTime;

            if (_blendMissingSampleTimer <= _blendMissingSampleGraceTime && _hasStableBlendResult)
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
                UnityEngine.Debug.Log("[PlayerMovement] " + $"Blend terminé | {_blendSourceMovementSpace.name} -> {_blendTargetMovementSpace.name}", this);

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

            _stableBlendT = Mathf.MoveTowards(_stableBlendT, targetT, _blendMaxProgressSpeed * Time.deltaTime);
        }

        private void SetStableBlendResult(MovementSpace.BlendResult sourceResult)
        {
            _stableBlendResult = sourceResult;
            _stableBlendResult.T = _stableBlendT;
            _stableBlendResult.Rotation = Quaternion.Slerp(sourceResult.Source.transform.rotation, sourceResult.Target.transform.rotation, _stableBlendT);

            _hasStableBlendResult = true;
        }

        private float GetCurrentRotationBlendT(MovementSpace source, MovementSpace target)
        {
            float sourceY = source.transform.eulerAngles.y;
            float targetY = target.transform.eulerAngles.y;

            float totalAngle = Mathf.Abs(Mathf.DeltaAngle(sourceY, targetY));

            if (totalAngle <= 0.001f)
                return 0f;

            float currentAngle = Mathf.Abs(Mathf.DeltaAngle(sourceY, transform.eulerAngles.y));

            return Mathf.Clamp01(currentAngle / totalAngle);
        }

        public bool TryGetActiveMovementSpaceBlend(out MovementSpace.BlendResult result)
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

            return _currentMovementSpace != null ? _currentMovementSpace.transform.rotation : Quaternion.identity;
        }

        public float GetEffectiveMovementSpaceY() => GetEffectiveMovementSpaceRotation().eulerAngles.y;

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
                transform.rotation = Quaternion.Euler(0f, _stableBlendResult.Rotation.eulerAngles.y, 0f);
                _movementSpaceRotationVelocity = 0f;
                return;
            }

            float targetY = _movementSpace.eulerAngles.y;
            float currentY = transform.eulerAngles.y;

            float smoothedY = Mathf.SmoothDampAngle(currentY, targetY, ref _movementSpaceRotationVelocity, _movementSpaceRotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, smoothedY, 0f);
        }

        private void SetAnimatorVerticalVelocity(float velocity)
        {
            if (!_animator)
                return;

            float maxReference = Mathf.Max(_jumpHeight, 0.01f);
            float clampedVelocity = Mathf.Clamp(velocity, -maxReference, maxReference);

            _animator.SetFloat(VerticalVelocity, clampedVelocity);
        }

        // =========================================================
        // JUMP / GRAVITY
        // =========================================================

        private void CalculateJumpDuration()
        {
            if (_gravity >= 0f)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(PlayerMovement)}] Gravity must be negative.", this);
                _jumpDuration = 0.1f;
                return;
            }

            float jumpVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            float physicalJumpDuration = -2f * jumpVelocity / _gravity;

            _jumpDuration = Mathf.Max(physicalJumpDuration * _jumpDurationMultiplier, 0.01f);
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
            float actualVerticalVelocity = actualDeltaHeight / Mathf.Max(Time.deltaTime, 0.0001f);

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
            if (_verticalVelocity < 0f && Mathf.Abs(actualDeltaHeight) <= 0.0001f)
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

            float previousTime = _dashTimer;
            
            _dashTimer += Time.deltaTime;
            
            float previousProgress = Mathf.Clamp01(previousTime / _dashDuration);
            float currentProgress = Mathf.Clamp01(_dashTimer / _dashDuration);
            
            float deltaCurve = _dashCurve.Evaluate(currentProgress) - _dashCurve.Evaluate(previousProgress);
    
            MoveWithMovementSpaceLimits(_dashDirection * (_dashDistance * deltaCurve));

            if (_dashTimer < _dashDuration)
                return;

            _dashTimer = 0f;
            _isDashing = false;
            _verticalVelocity = -2f;

            if (_animator)
            {
                _animator.SetBool(IsDashing, false);
                SetAnimatorVerticalVelocity(0f);
            }
        }

        private void HandleDashCooldown()
        {
            if (_dashCooldownTimer <= 0f)
                return;

            _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - Time.deltaTime);
        }

        // =========================================================
        // MOVEMENT SPACE LIMITS
        // =========================================================

        private void MoveWithMovementSpaceLimits(Vector3 movement)
        {
            if (!_movementSpace)
            {
                _controller.Move(movement);
                return;
            }

            Vector3 targetPosition = transform.position + movement;
            Vector3 localTargetPosition = _movementSpace.InverseTransformPoint(targetPosition);

            if (_currentMovementSpace)
            {
                MovementSpace connectedMovementSpace = GetConnectedMovementSpace(localTargetPosition, out MovementSpace.Boundary crossedBoundary);

                if (connectedMovementSpace)
                {
                    if (!_isMovementSpaceBlending && _currentMovementSpace.TryGetBlend(crossedBoundary, targetPosition, out MovementSpace.BlendResult blendResult))
                        BeginMovementSpaceBlend(blendResult);

                    SetMovementSpaceInternal(connectedMovementSpace.transform, false);
                    localTargetPosition = _movementSpace.InverseTransformPoint(targetPosition);
                }

                if (_currentMovementSpace.UseDepthMin && localTargetPosition.z < _currentMovementSpace.MinDepth)
                    localTargetPosition.z = _currentMovementSpace.MinDepth;

                if (_currentMovementSpace.UseDepthMax && localTargetPosition.z > _currentMovementSpace.MaxDepth)
                    localTargetPosition.z = _currentMovementSpace.MaxDepth;

                if (_currentMovementSpace.UseSideLeft && localTargetPosition.x < _currentMovementSpace.MinSide)
                    localTargetPosition.x = _currentMovementSpace.MinSide;

                if (_currentMovementSpace.UseSideRight && localTargetPosition.x > _currentMovementSpace.MaxSide)
                    localTargetPosition.x = _currentMovementSpace.MaxSide;
            }

            targetPosition = _movementSpace.TransformPoint(localTargetPosition);

            _controller.Move(targetPosition - transform.position);
        }

        private MovementSpace GetConnectedMovementSpace(Vector3 localTargetPosition, out MovementSpace.Boundary crossedBoundary)
        {
            crossedBoundary = default;

            if (!_currentMovementSpace)
                return null;

            if (!_currentMovementSpace.UseDepthMin && localTargetPosition.z < _currentMovementSpace.MinDepth)
            {
                crossedBoundary = MovementSpace.Boundary.DepthMin;
                return _currentMovementSpace.DepthMinConnection;
            }

            if (!_currentMovementSpace.UseDepthMax && localTargetPosition.z > _currentMovementSpace.MaxDepth)
            {
                crossedBoundary = MovementSpace.Boundary.DepthMax;
                return _currentMovementSpace.DepthMaxConnection;
            }

            if (!_currentMovementSpace.UseSideLeft && localTargetPosition.x < _currentMovementSpace.MinSide)
            {
                crossedBoundary = MovementSpace.Boundary.SideLeft;
                return _currentMovementSpace.SideLeftConnection;
            }

            if (!_currentMovementSpace.UseSideRight && localTargetPosition.x > _currentMovementSpace.MaxSide)
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
                UnityEngine.Debug.LogWarning($"[{nameof(PlayerMovement)}] Le Movement Space '{_movementSpace.name}' ne possède pas de composant MovementSpace.", _movementSpace);
        }

        public void SetMovementSpace(Transform newMovementSpace)
        {
            if (!newMovementSpace)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(PlayerMovement)}] Impossible d'assigner un Movement Space null.", this);
                return;
            }

            ClearMovementSpaceBlend();
            SetMovementSpaceInternal(newMovementSpace, false);
        }

        private void SetMovementSpaceInternal(Transform newMovementSpace, bool clearBlend)
        {
            if (!newMovementSpace)
                return;

            if (clearBlend)
                ClearMovementSpaceBlend();

            _movementSpace = newMovementSpace;
            _movementSpaceRotationVelocity = 0f;

            CacheMovementSpaceComponent();

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] " + "Movement Space changé : " + $"{_movementSpace.name}", this);
        }

        public Transform GetMovementSpace() => _movementSpace;
    }
}
