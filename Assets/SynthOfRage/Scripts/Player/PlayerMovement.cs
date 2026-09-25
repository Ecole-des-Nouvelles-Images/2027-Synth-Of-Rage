using UnityEngine;
using UnityEngine.Serialization;

namespace SynthOfRage.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private static readonly int Move = Animator.StringToHash("Move");

        [FormerlySerializedAs("playerObserver")] [Header("References")] [SerializeField]
        private PlayerObserver _playerObserver;

        [FormerlySerializedAs("spriteRenderer")] [SerializeField] private SpriteRenderer _spriteRenderer;
        [FormerlySerializedAs("movementSpace")] [SerializeField] private Transform _movementSpace;

        [FormerlySerializedAs("debugEnabled")] [Header("Dev Settings")]
        [SerializeField] private bool _debugEnabled;

        [FormerlySerializedAs("moveSpeed")] [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

        [FormerlySerializedAs("followMovementSpaceRotation")]
        [Header("Movement Space Rotation")]
        [Tooltip("Permet au personnage de suivre progressivement la rotation Y du Movement Space.")]
        [SerializeField]
        private bool _followMovementSpaceRotation = true;

        [FormerlySerializedAs("movementSpaceRotationSmoothTime")]
        [Tooltip("Temps de lissage de la rotation du personnage.")]
        [SerializeField] private float _movementSpaceRotationSmoothTime = 0.15f;

        [FormerlySerializedAs("jumpHeight")] [Header("Jump")]
        [SerializeField] private float _jumpHeight = 2f;

        [FormerlySerializedAs("gravity")]
        [Tooltip("Gravité utilisée pour calculer la durée de référence du saut.")]
        [SerializeField] private float _gravity = -20f;

        [FormerlySerializedAs("jumpCurve")]
        [Tooltip("Courbe représentant la hauteur normalisée du saut.")]
        [SerializeField] private AnimationCurve _jumpCurve = new(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        [FormerlySerializedAs("jumpDurationMultiplier")]
        [Tooltip("Modifie la durée calculée à partir de la gravité.")]
        [SerializeField] private float _jumpDurationMultiplier = 1f;

        [FormerlySerializedAs("dashDistance")] [Header("Dash")]
        [SerializeField] private float _dashDistance = 4f;

        [FormerlySerializedAs("dashDuration")] [SerializeField] private float _dashDuration = 0.2f;

        [FormerlySerializedAs("dashCooldown")]
        [Tooltip("Temps avant de pouvoir relancer un dash.")]
        [SerializeField] private float _dashCooldown = 0.5f;

        [FormerlySerializedAs("dashCurve")] [SerializeField] private AnimationCurve _dashCurve = AnimationCurve.EaseInOut( 0f, 0f, 1f, 1f);

        private CharacterController _controller;
        private Animator _animator;

        private MovementSpace _currentMovementSpace;
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

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            
            if (!_controller) UnityEngine.Debug.LogError("[PlayerMovement] CharacterController component is not found on Player !");
            if (!_animator) UnityEngine.Debug.LogError("[PlayerMovement] Animator component is not found on Player !");

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            CacheMovementSpaceComponent();
            CalculateJumpDuration();
        }

        private void Update()
        {
            UpdateCharacterRotation();

            HandleMovement();
            HandleJump();
            HandleGravity();
            HandleDashCooldown();
            HandleDash();
        }

        private void OnEnable()
        {
            if (_playerObserver == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(PlayerMovement)}] " + "PlayerObserver reference is missing.", this);
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

        private void HandlePlayerMove(Vector2 input)
        {
            _moveInput = Vector2.ClampMagnitude(input, 1f);
            
            _animator.SetBool(Move, _moveInput != Vector2.zero);

            UpdateSpriteDirection(input);

            if (_debugEnabled)UnityEngine.Debug.Log("[PlayerMovement] " + $"Move Input : {_moveInput}");
        }

        private void HandlePlayerJump()
        {
            if (!_controller.isGrounded) return;
            if (_isJumping) return;
            if (_isDashing) return;
            if (_jumpHeight <= 0f) return;

            CalculateJumpDuration();

            _isJumping = true;
            _jumpTimer = 0f;
            _previousJumpHeight = 0f;
            _verticalVelocity = 0f;

            if (_debugEnabled)
                UnityEngine.Debug.Log("[PlayerMovement] Jump | " + $"Height : {_jumpHeight} | " + $"Duration : {_jumpDuration}");
        }

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
                    UnityEngine.Debug.Log("[PlayerMovement] " + "Jump interrupted by Dash");
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
                UnityEngine.Debug.Log("[PlayerMovement] Dash : " + $"{_dashDirection} | " + $"Cooldown : {_dashCooldown}");
        }

        private void HandleMovement()
        {
            if (_isDashing) return;

            Vector3 movement = GetMovementDirection(_moveInput);

            movement *= _moveSpeed;

            MoveWithMovementSpaceLimits(movement * Time.deltaTime);
        }

        private Vector3 GetMovementDirection(Vector2 input)
        {
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            Vector3 direction = right * input.x + forward * input.y;

            direction.y = 0f;

            return Vector3.ClampMagnitude(direction, 1f );
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

        private void UpdateCharacterRotation()
        {
            if (!_followMovementSpaceRotation)
                return;

            if (!_movementSpace)
                return;

            float targetY = _movementSpace.eulerAngles.y;
            float currentY = transform.eulerAngles.y;
            float smoothedY = Mathf.SmoothDampAngle(currentY, targetY, ref _movementSpaceRotationVelocity, _movementSpaceRotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, smoothedY, 0f);
        }

        private void CalculateJumpDuration()
        {
            if (_gravity >= 0f)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(PlayerMovement)}] " + "Gravity must be negative.", this);

                _jumpDuration = 0.1f;
                return;
            }

            float jumpVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            float physicalJumpDuration = -2f * jumpVelocity / _gravity;

            _jumpDuration = physicalJumpDuration * _jumpDurationMultiplier;
            _jumpDuration = Mathf.Max(_jumpDuration, 0.01f );
        }

        private void HandleJump()
        {
            if (_isDashing || !_isJumping) return;

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

        private void HandleGravity()
        {
            if (_isDashing || _isJumping) return;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -2f;
                return;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 gravityMovement = Vector3.up * (_verticalVelocity * Time.deltaTime);

            MoveWithMovementSpaceLimits(gravityMovement);
        }

        private void HandleDash()
        {
            if (!_isDashing) return;

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
            if (_dashCooldownTimer <= 0f) return;

            _dashCooldownTimer -= Time.deltaTime;

            if (_dashCooldownTimer < 0f)
                _dashCooldownTimer = 0f;
        }

        private void MoveWithMovementSpaceLimits(Vector3 movement)
        {
            if (!_movementSpace)
            {
                _controller.Move(movement);
                return;
            }

            Vector3 targetPosition = transform.position + movement;
            Vector3 localTargetPosition = _movementSpace.InverseTransformPoint(targetPosition);

            /*
             * ---------------------------------------------------------
             * MOVEMENT SPACE TRANSITION
             * ---------------------------------------------------------
             *
             * On vérifie d'abord si le joueur dépasse une limite
             * qui est configurée comme PASSABLE.
             *
             * Si une connexion existe :
             *      -> on change de Movement Space
             *      -> on recalcule la position locale
             *
             * Si aucune connexion n'existe :
             *      -> le joueur continue normalement
             *
             * Une seule transition est effectuée par déplacement.
             * Cela évite les boucles entre deux Movement Spaces.
             * ---------------------------------------------------------
             */

            if (_currentMovementSpace)
            {
                MovementSpace connectedMovementSpace = GetConnectedMovementSpace(localTargetPosition);

                if (connectedMovementSpace)
                {
                    SetMovementSpace(connectedMovementSpace.transform);

                    /*
                     * Le Movement Space vient de changer.
                     *
                     * On doit donc recalculer la position cible
                     * dans le nouveau repère local.
                     */
                    localTargetPosition = _movementSpace.InverseTransformPoint(targetPosition);
                }

                /*
                 * -----------------------------------------------------
                 * LIMITES BLOQUANTES
                 * -----------------------------------------------------
                 *
                 * Ces limites ne sont appliquées QUE si elles sont
                 * activées.
                 *
                 * UseXXX == true
                 *      => limite bloquante
                 *
                 * UseXXX == false
                 *      => limite passable
                 * -----------------------------------------------------
                 */

                if (_currentMovementSpace.UseDepthMin && localTargetPosition.z < _currentMovementSpace.MinDepth) // DEPTH MIN

                    localTargetPosition.z = _currentMovementSpace.MinDepth;

                
                if (_currentMovementSpace.UseDepthMax && localTargetPosition.z > _currentMovementSpace.MaxDepth) // DEPTH MAX
                    localTargetPosition.z = _currentMovementSpace.MaxDepth;

                if (_currentMovementSpace.UseSideLeft && localTargetPosition.x < _currentMovementSpace.MinSide) // SIDE LEFT
                    localTargetPosition.x = _currentMovementSpace.MinSide;

                if (_currentMovementSpace.UseSideRight && localTargetPosition.x > _currentMovementSpace.MaxSide) // SIDE RIGHT
                    localTargetPosition.x = _currentMovementSpace.MaxSide;
            }

            targetPosition = _movementSpace.TransformPoint(localTargetPosition);

            Vector3 allowedMovement = targetPosition - transform.position;

            _controller.Move(allowedMovement);
        }

        private MovementSpace GetConnectedMovementSpace(Vector3 localTargetPosition)
        {
            if (!_currentMovementSpace)
                return null;

            /*
             * ---------------------------------------------------------
             * DEPTH MIN
             * ---------------------------------------------------------
             *
             * Si la limite est passable et que le joueur la dépasse,
             * on cherche le Movement Space connecté.
             */

            if (!_currentMovementSpace.UseDepthMin && localTargetPosition.z < _currentMovementSpace.MinDepth)
                return _currentMovementSpace.DepthMinConnection;

            /*
             * ---------------------------------------------------------
             * DEPTH MAX
             * ---------------------------------------------------------
             */

            if (!_currentMovementSpace.UseDepthMax && localTargetPosition.z > _currentMovementSpace.MaxDepth )
                return _currentMovementSpace.DepthMaxConnection;

            /*
             * ---------------------------------------------------------
             * SIDE LEFT
             * ---------------------------------------------------------
             */

            if (!_currentMovementSpace.UseSideLeft && localTargetPosition.x < _currentMovementSpace.MinSide)
                return _currentMovementSpace.SideLeftConnection;

            /*
             * ---------------------------------------------------------
             * SIDE RIGHT
             * ---------------------------------------------------------
             */

            if (!_currentMovementSpace.UseSideRight && localTargetPosition.x > _currentMovementSpace.MaxSide)
                return _currentMovementSpace.SideRightConnection;

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
                UnityEngine.Debug.LogWarning($"[{nameof(PlayerMovement)}] Le Movement Space '{_movementSpace.name}' ne possède pas de composant MovementSpace.\n"
                                             + "Aucune limite personnalisée ne sera appliquée.", _movementSpace);
        }

        public void SetMovementSpace(Transform newMovementSpace)
        {
            if (!newMovementSpace)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(PlayerMovement)}] " + "Impossible d'assigner un Movement Space null.", this );
                return;
            }

            _movementSpace = newMovementSpace;
            _movementSpaceRotationVelocity = 0f;

            CacheMovementSpaceComponent();

            if (_debugEnabled)
                UnityEngine.Debug.Log( "[PlayerMovement] " + "Movement Space changé : " + $"{_movementSpace.name}", this);
        }

        public Transform GetMovementSpace()
        {
            return _movementSpace;
        }
    }
}
