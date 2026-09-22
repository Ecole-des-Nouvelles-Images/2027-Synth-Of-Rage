using UnityEngine;

namespace SynthOfRage.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerObserver playerObserver;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform movementSpace;

        [Header("Dev Settings")]
        [SerializeField] private bool debugEnabled;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Movement Limits")]
        [SerializeField] private bool useDepthLimits = true;
        [SerializeField] private float minDepth = -3.5f;
        [SerializeField] private float maxDepth = 3.5f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2f;

        [Tooltip("Gravité utilisée pour calculer la durée de référence du saut.")]
        [SerializeField] private float gravity = -20f;

        [Tooltip("Courbe représentant la hauteur normalisée du saut.")]
        [SerializeField]
        private AnimationCurve jumpCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f)
            );

        [Tooltip("Modifie la durée calculée à partir de la gravité.")]
        [SerializeField] private float jumpDurationMultiplier = 1f;

        [Header("Dash")]
        [SerializeField] private float dashDistance = 4f;
        [SerializeField] private float dashDuration = 0.2f;

        [Tooltip("Temps avant de pouvoir relancer un dash.")]
        [SerializeField] private float dashCooldown = 0.5f;

        [SerializeField]
        private AnimationCurve dashCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private CharacterController controller;

        private Vector2 moveInput;

        // ==================================================
        // JUMP
        // ==================================================

        private bool isJumping;
        private float jumpTimer;
        private float jumpDuration;
        private float previousJumpHeight;

        // ==================================================
        // GRAVITY
        // ==================================================

        private float verticalVelocity;

        // ==================================================
        // DASH
        // ==================================================

        private bool isDashing;
        private float dashTimer;
        private float dashCooldownTimer;
        private Vector3 dashDirection;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            CalculateJumpDuration();
        }

        private void OnEnable()
        {
            if (playerObserver == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerMovement)}] PlayerObserver reference is missing.",
                    this
                );

                return;
            }

            playerObserver.OnPlayerMove += HandlePlayerMove;
            playerObserver.OnPlayerJump += HandlePlayerJump;
            playerObserver.OnPlayerDash += HandlePlayerDash;
        }

        private void OnDisable()
        {
            if (playerObserver == null)
                return;

            playerObserver.OnPlayerMove -= HandlePlayerMove;
            playerObserver.OnPlayerJump -= HandlePlayerJump;
            playerObserver.OnPlayerDash -= HandlePlayerDash;
        }

        private void Update()
        {
            HandleMovement();
            HandleJump();
            HandleGravity();
            HandleDashCooldown();
            HandleDash();
        }

        // ==================================================
        // INPUT
        // ==================================================

        private void HandlePlayerMove(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);

            UpdateSpriteDirection(input);

            if (debugEnabled)
                Debug.Log(
                    $"[PlayerMovement] Move Input : {moveInput}"
                );
        }

        private void HandlePlayerJump()
        {
            if (!controller.isGrounded)
                return;

            if (isJumping)
                return;

            if (isDashing)
                return;

            if (jumpHeight <= 0f)
                return;

            CalculateJumpDuration();

            isJumping = true;
            jumpTimer = 0f;
            previousJumpHeight = 0f;

            verticalVelocity = 0f;

            if (debugEnabled)
            {
                Debug.Log(
                    $"[PlayerMovement] Jump | " +
                    $"Height : {jumpHeight} | " +
                    $"Duration : {jumpDuration}"
                );
            }
        }

        private void HandlePlayerDash()
        {
            // Impossible de démarrer un autre dash
            // pendant le dash actuel.
            if (isDashing)
                return;

            // Cooldown encore actif.
            if (dashCooldownTimer > 0f)
            {
                if (debugEnabled)
                {
                    Debug.Log(
                        $"[PlayerMovement] Dash unavailable | " +
                        $"Cooldown : {dashCooldownTimer:F2}s"
                    );
                }

                return;
            }

            // --------------------------------------------------
            // Interruption du saut
            // --------------------------------------------------

            if (isJumping)
            {
                isJumping = false;
                jumpTimer = 0f;
                previousJumpHeight = 0f;

                if (debugEnabled)
                {
                    Debug.Log(
                        "[PlayerMovement] Jump interrupted by Dash"
                    );
                }
            }

            // --------------------------------------------------
            // Direction du dash
            // --------------------------------------------------

            if (moveInput.sqrMagnitude > 0.01f)
            {
                dashDirection =
                    GetMovementDirection(moveInput);
            }
            else
            {
                dashDirection =
                    spriteRenderer != null && spriteRenderer.flipX
                        ? -transform.right
                        : transform.right;
            }

            dashDirection.y = 0f;
            dashDirection.Normalize();

            // --------------------------------------------------
            // Start Dash
            // --------------------------------------------------

            dashTimer = 0f;
            isDashing = true;

            // Le cooldown commence au début du dash.
            dashCooldownTimer = dashCooldown;

            if (debugEnabled)
            {
                Debug.Log(
                    $"[PlayerMovement] Dash : {dashDirection} | " +
                    $"Cooldown : {dashCooldown}"
                );
            }
        }

        // ==================================================
        // MOVEMENT
        // ==================================================

        private void HandleMovement()
        {
            if (isDashing)
                return;

            Vector3 movement =
                GetMovementDirection(moveInput);

            movement *= moveSpeed;

            MoveWithDepthLimit(
                movement * Time.deltaTime
            );
        }

        private Vector3 GetMovementDirection(Vector2 input)
        {
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;

            Vector3 direction =
                right * input.x +
                forward * input.y;

            direction.y = 0f;

            return Vector3.ClampMagnitude(
                direction,
                1f
            );
        }

        // ==================================================
        // SPRITE
        // ==================================================

        private void UpdateSpriteDirection(Vector2 input)
        {
            if (spriteRenderer == null)
                return;

            if (input.x < -0.01f)
            {
                spriteRenderer.flipX = true;
            }
            else if (input.x > 0.01f)
            {
                spriteRenderer.flipX = false;
            }
        }

        // ==================================================
        // JUMP
        // ==================================================

        private void CalculateJumpDuration()
        {
            if (gravity >= 0f)
            {
                Debug.LogWarning(
                    $"[{nameof(PlayerMovement)}] Gravity must be negative.",
                    this
                );

                jumpDuration = 0.1f;
                return;
            }

            float jumpVelocity =
                Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );

            float physicalJumpDuration =
                (-2f * jumpVelocity) / gravity;

            jumpDuration =
                physicalJumpDuration *
                jumpDurationMultiplier;

            jumpDuration =
                Mathf.Max(
                    jumpDuration,
                    0.01f
                );
        }

        private void HandleJump()
        {
            // Le dash a la priorité sur le saut.
            if (isDashing)
                return;

            if (!isJumping)
                return;

            float progress =
                Mathf.Clamp01(
                    jumpTimer / jumpDuration
                );

            float curveValue =
                jumpCurve.Evaluate(progress);

            float currentHeight =
                curveValue * jumpHeight;

            float deltaHeight =
                currentHeight -
                previousJumpHeight;

            Vector3 jumpMovement =
                Vector3.up * deltaHeight;

            MoveWithDepthLimit(
                jumpMovement
            );

            previousJumpHeight =
                currentHeight;

            jumpTimer += Time.deltaTime;

            if (jumpTimer >= jumpDuration)
            {
                EndJump();
            }
        }

        private void EndJump()
        {
            isJumping = false;

            jumpTimer = 0f;
            previousJumpHeight = 0f;

            verticalVelocity = -2f;

            if (debugEnabled)
                Debug.Log(
                    "[PlayerMovement] Jump End"
                );
        }

        // ==================================================
        // GRAVITY
        // ==================================================

        private void HandleGravity()
        {
            // Le dash bloque complètement la gravité.
            if (isDashing)
                return;

            // Pendant le saut, la Jump Curve contrôle
            // entièrement le mouvement vertical.
            if (isJumping)
                return;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                return;
            }

            verticalVelocity +=
                gravity * Time.deltaTime;

            Vector3 gravityMovement =
                Vector3.up *
                (verticalVelocity * Time.deltaTime);

            MoveWithDepthLimit(
                gravityMovement
            );
        }

        // ==================================================
        // DASH
        // ==================================================

        private void HandleDash()
        {
            if (!isDashing)
                return;

            float previousTime =
                dashTimer;

            dashTimer += Time.deltaTime;

            float previousProgress =
                Mathf.Clamp01(
                    previousTime / dashDuration
                );

            float currentProgress =
                Mathf.Clamp01(
                    dashTimer / dashDuration
                );

            float previousCurveValue =
                dashCurve.Evaluate(
                    previousProgress
                );

            float currentCurveValue =
                dashCurve.Evaluate(
                    currentProgress
                );

            float deltaCurveValue =
                currentCurveValue -
                previousCurveValue;

            Vector3 dashMovement =
                dashDirection *
                (dashDistance * deltaCurveValue);

            MoveWithDepthLimit(
                dashMovement
            );

            if (dashTimer >= dashDuration)
            {
                dashTimer = 0f;
                isDashing = false;

                // La gravité pourra reprendre
                // au prochain Update.
                verticalVelocity = -2f;

                if (debugEnabled)
                {
                    Debug.Log(
                        "[PlayerMovement] Dash End"
                    );
                }
            }
        }

        private void HandleDashCooldown()
        {
            if (dashCooldownTimer <= 0f)
                return;

            dashCooldownTimer -= Time.deltaTime;

            if (dashCooldownTimer < 0f)
                dashCooldownTimer = 0f;
        }

        // ==================================================
        // DEPTH LIMIT
        // ==================================================

        private void MoveWithDepthLimit(Vector3 movement)
        {
            if (!useDepthLimits || movementSpace == null)
            {
                controller.Move(movement);
                return;
            }

            Vector3 targetPosition =
                transform.position + movement;

            Vector3 localTargetPosition =
                movementSpace.InverseTransformPoint(
                    targetPosition
                );

            localTargetPosition.z =
                Mathf.Clamp(
                    localTargetPosition.z,
                    minDepth,
                    maxDepth
                );

            targetPosition =
                movementSpace.TransformPoint(
                    localTargetPosition
                );

            Vector3 allowedMovement =
                targetPosition -
                transform.position;

            controller.Move(
                allowedMovement
            );
        }
    }
}