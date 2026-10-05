using SynthOfRage.Scripts.Unit.Player.Movement.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace SynthOfRage.Scripts.Unit.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private static readonly int Move = Animator.StringToHash("Move");

        // =========================================================
        // REFERENCES
        // =========================================================

        [FormerlySerializedAs("playerObserver")]
        [Header("References")]
        [SerializeField]
        private PlayerObserver _playerObserver;

        [FormerlySerializedAs("spriteRenderer")]
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [FormerlySerializedAs("movementSpace")]
        [SerializeField]
        private Transform _movementSpace;

        // =========================================================
        // DEV SETTINGS
        // =========================================================

        [FormerlySerializedAs("debugEnabled")]
        [Header("Dev Settings")]
        [SerializeField]
        private bool _debugEnabled;

        // =========================================================
        // MOVEMENT
        // =========================================================

        [FormerlySerializedAs("moveSpeed")]
        [Header("Movement")]
        [SerializeField]
        private float _moveSpeed = 5f;

        // =========================================================
        // MOVEMENT SPACE ROTATION
        // =========================================================

        [FormerlySerializedAs("followMovementSpaceRotation")]
        [Header("Movement Space Rotation")]
        [Tooltip("Permet au personnage de suivre progressivement la rotation Y du Movement Space.")]
        [SerializeField]
        private bool _followMovementSpaceRotation = true;

        [FormerlySerializedAs("movementSpaceRotationSmoothTime")]
        [Tooltip("Temps de lissage de la rotation du personnage hors blend.")]
        [SerializeField]
        private float _movementSpaceRotationSmoothTime = 0.15f;

        // =========================================================
        // JUMP
        // =========================================================

        [FormerlySerializedAs("jumpHeight")]
        [Header("Jump")]
        [SerializeField]
        private float _jumpHeight = 2f;

        [FormerlySerializedAs("gravity")]
        [Tooltip("Gravité utilisée pour calculer la durée de référence du saut.")]
        [SerializeField]
        private float _gravity = -20f;

        [FormerlySerializedAs("jumpCurve")]
        [Tooltip("Courbe représentant la hauteur normalisée du saut.")]
        [SerializeField]
        private AnimationCurve _jumpCurve = new(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        [FormerlySerializedAs("jumpDurationMultiplier")]
        [Tooltip("Modifie la durée calculée à partir de la gravité.")]
        [SerializeField]
        private float _jumpDurationMultiplier = 1f;

        // =========================================================
        // DASH
        // =========================================================

        [FormerlySerializedAs("dashDistance")]
        [Header("Dash")]
        [SerializeField]
        private float _dashDistance = 4f;

        [FormerlySerializedAs("dashDuration")]
        [SerializeField]
        private float _dashDuration = 0.2f;

        [FormerlySerializedAs("dashCooldown")]
        [Tooltip("Temps avant de pouvoir relancer un dash.")]
        [SerializeField]
        private float _dashCooldown = 0.5f;

        [FormerlySerializedAs("dashCurve")]
        [SerializeField]
        private AnimationCurve _dashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // =========================================================
        // COMPONENTS
        // =========================================================

        private CharacterController _controller;
        private Animator _animator;

        // =========================================================
        // MOVEMENT SPACE
        // =========================================================

        private MovementSpace _currentMovementSpace;

        // =========================================================
        // MOVEMENT SPACE BLEND
        // =========================================================

        private bool _isMovementSpaceBlending;
        private MovementSpace _blendSourceMovementSpace;
        private MovementSpace.Boundary _blendSourceBoundary;

        // =========================================================
        // DASH STATE
        // =========================================================

        private float _dashCooldownTimer;
        private Vector3 _dashDirection;
        private float _dashTimer;
        private bool _isDashing;

        // =========================================================
        // JUMP STATE
        // =========================================================

        private bool _isJumping;
        private float _jumpDuration;
        private float _jumpTimer;

        // =========================================================
        // INPUT / ROTATION
        // =========================================================

        private Vector2 _moveInput;
        private float _movementSpaceRotationVelocity;
        private float _previousJumpHeight;
        private float _verticalVelocity;

        // =========================================================
        // PUBLIC STATE
        // =========================================================

        public bool IsMovementSpaceBlending => _isMovementSpaceBlending;

        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();

            if (!_controller)
                UnityEngine.Debug.LogError("[PlayerMovement] CharacterController component is not found on Player !");

            if (!_animator)
                UnityEngine.Debug.LogError("[PlayerMovement] Animator component is not found on Player !");

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            CacheMovementSpaceComponent();
            CalculateJumpDuration();
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            HandleMovement();
            HandleJump();
            HandleGravity();
            HandleDashCooldown();
            HandleDash();
            UpdateCharacterRotation();
        }

        // =========================================================
        // ENABLE / DISABLE
        // =========================================================

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
        // PLAYER MOVE
        // =========================================================

        private void HandlePlayerMove(Vector2 input)
        {
            _moveInput = Vector2.ClampMagnitude(input, 1f);

            if (_animator)
                _animator.SetBool(Move, _moveInput != Vector2.zero);

            UpdateSpriteDirection(input);

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] " + $"Move Input : {_moveInput}");
        }

        // =========================================================
        // PLAYER JUMP
        // =========================================================

        private void HandlePlayerJump()
        {
            if (!_controller.isGrounded)
                return;

            if (_isJumping)
                return;

            if (_isDashing)
                return;

            if (_jumpHeight <= 0f)
                return;

            CalculateJumpDuration();

            _isJumping = true;
            _jumpTimer = 0f;
            _previousJumpHeight = 0f;
            _verticalVelocity = 0f;

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] Jump | " + $"Height : {_jumpHeight} | " + $"Duration : {_jumpDuration}");
        }

        // =========================================================
        // PLAYER DASH
        // =========================================================

        private void HandlePlayerDash()
        {
            if (_isDashing)
                return;

            if (_dashCooldownTimer > 0f)
            {
                if (_debugEnabled)
                    UnityEngine.Debug.Log("[PlayerMovement] " + "Dash unavailable | " + $"Cooldown : {_dashCooldownTimer:F2}s");

                return;
            }

            if (_isJumping)
            {
                _isJumping = false;
                _jumpTimer = 0f;
                _previousJumpHeight = 0f;

                if (_debugEnabled)
                    UnityEngine.Debug.Log("[PlayerMovement] Jump interrupted by Dash");
            }

            if (_moveInput.sqrMagnitude > 0.01f)
                _dashDirection = GetMovementDirection(_moveInput);
            else
                _dashDirection = _spriteRenderer != null && _spriteRenderer.flipX ? -transform.right : transform.right;

            _dashDirection.y = 0f;
            _dashDirection.Normalize();

            _dashTimer = 0f;
            _isDashing = true;
            _dashCooldownTimer = _dashCooldown;

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] Dash : " + $"{_dashDirection} | Cooldown : {_dashCooldown}");
        }

        // =========================================================
        // MOVEMENT
        // =========================================================

        private void HandleMovement()
        {
            if (_isDashing)
                return;

            Vector3 movement = GetMovementDirection(_moveInput);
            movement *= _moveSpeed;

            MoveWithMovementSpaceLimits(movement * Time.deltaTime);
        }

        // =========================================================
        // MOVEMENT DIRECTION
        // =========================================================

        private Vector3 GetMovementDirection(Vector2 input)
        {
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            Vector3 direction = right * input.x + forward * input.y;

            direction.y = 0f;

            return Vector3.ClampMagnitude(direction, 1f);
        }

        // =========================================================
        // SPRITE DIRECTION
        // =========================================================

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
        // CHARACTER ROTATION
        // =========================================================

        private void UpdateCharacterRotation()
        {
            if (!_followMovementSpaceRotation)
                return;

            if (!_movementSpace)
                return;

            if (TryGetBlendRotation(out Quaternion targetRotation))
            {
                ApplyCharacterRotation(targetRotation);
                return;
            }

            float targetY = _movementSpace.eulerAngles.y;
            float currentY = transform.eulerAngles.y;
            float smoothedY = Mathf.SmoothDampAngle(currentY, targetY, ref _movementSpaceRotationVelocity, _movementSpaceRotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, smoothedY, 0f);
        }

        // =========================================================
        // BLEND ROTATION
        // =========================================================

        private bool TryGetBlendRotation(out Quaternion targetRotation)
        {
            targetRotation = Quaternion.identity;

            if (_isMovementSpaceBlending)
            {
                if (_blendSourceMovementSpace == null)
                {
                    ClearMovementSpaceBlend();
                    return false;
                }

                if (_blendSourceMovementSpace.TryGetBlend(_blendSourceBoundary, transform.position, out MovementSpace.BlendResult result))
                {
                    targetRotation = result.Rotation;
                    return true;
                }

                if (_debugEnabled)
                    UnityEngine.Debug.Log("[PlayerMovement] " + $"Blend terminé | Source : {_blendSourceMovementSpace.name} | Current : {(_currentMovementSpace != null ? _currentMovementSpace.name : "NULL")}", this);

                ClearMovementSpaceBlend();
                return false;
            }

            if (_currentMovementSpace != null && _currentMovementSpace.TryGetAnyBlend(transform.position, out MovementSpace.BlendResult newBlend))
            {
                _blendSourceMovementSpace = newBlend.Source;
                _blendSourceBoundary = newBlend.SourceBoundary;
                _isMovementSpaceBlending = true;

                if (_debugEnabled)
                    UnityEngine.Debug.Log("[PlayerMovement] " + $"Blend commencé | {newBlend.Source.name} -> {newBlend.Target.name} | T = {newBlend.T:F3}", this);

                targetRotation = newBlend.Rotation;
                return true;
            }

            return false;
        }

        // =========================================================
        // APPLY CHARACTER ROTATION
        // =========================================================

        private void ApplyCharacterRotation(Quaternion targetRotation)
        {
            float targetY = targetRotation.eulerAngles.y;

            transform.rotation = Quaternion.Euler(0f, targetY, 0f);
            _movementSpaceRotationVelocity = 0f;
        }

        // =========================================================
        // CLEAR MOVEMENT SPACE BLEND
        // =========================================================

        private void ClearMovementSpaceBlend()
        {
            _isMovementSpaceBlending = false;
            _blendSourceMovementSpace = null;
            _movementSpaceRotationVelocity = 0f;
        }

        // =========================================================
        // ACTIVE MOVEMENT SPACE BLEND
        // =========================================================

        public bool TryGetActiveMovementSpaceBlend(out MovementSpace.BlendResult result)
        {
            result = default;

            if (_isMovementSpaceBlending && _blendSourceMovementSpace != null)
            {
                if (_blendSourceMovementSpace.TryGetBlend(_blendSourceBoundary, transform.position, out result))
                    return true;

                return false;
            }

            if (_currentMovementSpace != null && _currentMovementSpace.TryGetAnyBlend(transform.position, out result))
                return true;

            return false;
        }

        // =========================================================
        // EFFECTIVE MOVEMENT SPACE ROTATION
        // =========================================================

        public Quaternion GetEffectiveMovementSpaceRotation()
        {
            if (_currentMovementSpace == null)
                return Quaternion.identity;

            if (_isMovementSpaceBlending && _blendSourceMovementSpace != null)
            {
                if (_blendSourceMovementSpace.TryGetBlend(_blendSourceBoundary, transform.position, out MovementSpace.BlendResult result))
                    return result.Rotation;
            }

            return _currentMovementSpace.transform.rotation;
        }

        public float GetEffectiveMovementSpaceY()
        {
            return GetEffectiveMovementSpaceRotation().eulerAngles.y;
        }

        // =========================================================
        // JUMP DURATION
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

            _jumpDuration = physicalJumpDuration * _jumpDurationMultiplier;
            _jumpDuration = Mathf.Max(_jumpDuration, 0.01f);
        }

        // =========================================================
        // JUMP
        // =========================================================

        private void HandleJump()
        {
            if (_isDashing || !_isJumping)
                return;

            float progress = Mathf.Clamp01(_jumpTimer / _jumpDuration);
            float curveValue = _jumpCurve.Evaluate(progress);
            float currentHeight = curveValue * _jumpHeight;
            float deltaHeight = currentHeight - _previousJumpHeight;

            Vector3 jumpMovement = Vector3.up * deltaHeight;

            MoveWithMovementSpaceLimits(jumpMovement);

            _previousJumpHeight = currentHeight;
            _jumpTimer += Time.deltaTime;

            if (_jumpTimer >= _jumpDuration)
                EndJump();
        }

        private void EndJump()
        {
            _isJumping = false;
            _jumpTimer = 0f;
            _previousJumpHeight = 0f;
            _verticalVelocity = -2f;

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] Jump End");
        }

        // =========================================================
        // GRAVITY
        // =========================================================

        private void HandleGravity()
        {
            if (_isDashing || _isJumping)
                return;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -2f;
                return;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 gravityMovement = Vector3.up * (_verticalVelocity * Time.deltaTime);

            MoveWithMovementSpaceLimits(gravityMovement);
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

            float previousCurveValue = _dashCurve.Evaluate(previousProgress);
            float currentCurveValue = _dashCurve.Evaluate(currentProgress);
            float deltaCurveValue = currentCurveValue - previousCurveValue;

            Vector3 dashMovement = _dashDirection * (_dashDistance * deltaCurveValue);

            MoveWithMovementSpaceLimits(dashMovement);

            if (_dashTimer >= _dashDuration)
            {
                _dashTimer = 0f;
                _isDashing = false;
                _verticalVelocity = -2f;

                if (_debugEnabled)
                    UnityEngine.Debug.Log("[PlayerMovement] Dash End");
            }
        }

        private void HandleDashCooldown()
        {
            if (_dashCooldownTimer <= 0f)
                return;

            _dashCooldownTimer -= Time.deltaTime;

            if (_dashCooldownTimer < 0f)
                _dashCooldownTimer = 0f;
        }

        // =========================================================
        // MOVE WITH MOVEMENT SPACE LIMITS
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
                    {
                        _blendSourceMovementSpace = blendResult.Source;
                        _blendSourceBoundary = blendResult.SourceBoundary;
                        _isMovementSpaceBlending = true;

                        if (_debugEnabled)
                            UnityEngine.Debug.Log("[PlayerMovement] " + $"Blend déclenché par transition | {blendResult.Source.name} -> {blendResult.Target.name} | T = {blendResult.T:F3}", this);
                    }

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

            Vector3 allowedMovement = targetPosition - transform.position;

            _controller.Move(allowedMovement);
        }

        // =========================================================
        // GET CONNECTED MOVEMENT SPACE
        // =========================================================

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

        // =========================================================
        // CACHE MOVEMENT SPACE
        // =========================================================

        private void CacheMovementSpaceComponent()
        {
            if (!_movementSpace)
            {
                _currentMovementSpace = null;
                return;
            }

            _currentMovementSpace = _movementSpace.GetComponent<MovementSpace>();

            if (!_currentMovementSpace)
                UnityEngine.Debug.LogWarning($"[{nameof(PlayerMovement)}] Le Movement Space '{_movementSpace.name}' ne possède pas de composant MovementSpace.\nAucune limite personnalisée ne sera appliquée.", _movementSpace);
        }

        // =========================================================
        // SET MOVEMENT SPACE
        // =========================================================

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

        // =========================================================
        // GET MOVEMENT SPACE
        // =========================================================

        public Transform GetMovementSpace()
        {
            return _movementSpace;
        }
    }
}