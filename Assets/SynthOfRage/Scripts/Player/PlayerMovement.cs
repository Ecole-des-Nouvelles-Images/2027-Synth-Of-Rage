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

        [Header("Movement Space Rotation")]
        [Tooltip(
            "Permet au personnage de suivre progressivement " +
            "la rotation Y du Movement Space."
        )]
        [SerializeField] private bool followMovementSpaceRotation = true;

        [Tooltip(
            "Temps de lissage de la rotation du personnage."
        )]
        [SerializeField] private float movementSpaceRotationSmoothTime = 0.15f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2f;

        [Tooltip(
            "Gravité utilisée pour calculer la durée de référence du saut."
        )]
        [SerializeField] private float gravity = -20f;

        [Tooltip(
            "Courbe représentant la hauteur normalisée du saut."
        )]
        [SerializeField]
        private AnimationCurve jumpCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f)
            );

        [Tooltip(
            "Modifie la durée calculée à partir de la gravité."
        )]
        [SerializeField] private float jumpDurationMultiplier = 1f;

        [Header("Dash")]
        [SerializeField] private float dashDistance = 4f;

        [SerializeField] private float dashDuration = 0.2f;

        [Tooltip(
            "Temps avant de pouvoir relancer un dash."
        )]
        [SerializeField] private float dashCooldown = 0.5f;

        [SerializeField]
        private AnimationCurve dashCurve =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        private CharacterController controller;

        private Vector2 moveInput;

        private bool isJumping;
        private float jumpTimer;
        private float jumpDuration;
        private float previousJumpHeight;
        private float verticalVelocity;

        private bool isDashing;
        private float dashTimer;
        private float dashCooldownTimer;
        private Vector3 dashDirection;

        private float movementSpaceRotationVelocity;

        private MovementSpace currentMovementSpace;

        private void Awake()
        {
            controller =
                GetComponent<CharacterController>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }

            CacheMovementSpaceComponent();

            CalculateJumpDuration();
        }

        private void OnEnable()
        {
            if (playerObserver == null)
            {
                UnityEngine.Debug.LogError(
                    $"[{nameof(PlayerMovement)}] " +
                    "PlayerObserver reference is missing.",
                    this
                );

                return;
            }

            playerObserver.OnPlayerMove +=
                HandlePlayerMove;

            playerObserver.OnPlayerJump +=
                HandlePlayerJump;

            playerObserver.OnPlayerDash +=
                HandlePlayerDash;
        }

        private void OnDisable()
        {
            if (playerObserver == null)
                return;

            playerObserver.OnPlayerMove -=
                HandlePlayerMove;

            playerObserver.OnPlayerJump -=
                HandlePlayerJump;

            playerObserver.OnPlayerDash -=
                HandlePlayerDash;
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

        private void HandlePlayerMove(Vector2 input)
        {
            moveInput =
                Vector2.ClampMagnitude(
                    input,
                    1f
                );

            UpdateSpriteDirection(input);

            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
                    $"[PlayerMovement] " +
                    $"Move Input : {moveInput}"
                );
            }
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
                UnityEngine.Debug.Log(
                    $"[PlayerMovement] Jump | " +
                    $"Height : {jumpHeight} | " +
                    $"Duration : {jumpDuration}"
                );
            }
        }

        private void HandlePlayerDash()
        {
            if (isDashing)
                return;

            if (dashCooldownTimer > 0f)
            {
                if (debugEnabled)
                {
                    UnityEngine.Debug.Log(
                        $"[PlayerMovement] " +
                        $"Dash unavailable | " +
                        $"Cooldown : {dashCooldownTimer:F2}s"
                    );
                }

                return;
            }

            if (isJumping)
            {
                isJumping = false;
                jumpTimer = 0f;
                previousJumpHeight = 0f;

                if (debugEnabled)
                {
                    UnityEngine.Debug.Log(
                        "[PlayerMovement] " +
                        "Jump interrupted by Dash"
                    );
                }
            }

            if (moveInput.sqrMagnitude > 0.01f)
            {
                dashDirection =
                    GetMovementDirection(
                        moveInput
                    );
            }
            else
            {
                dashDirection =
                    spriteRenderer != null &&
                    spriteRenderer.flipX
                        ? -transform.right
                        : transform.right;
            }

            dashDirection.y = 0f;
            dashDirection.Normalize();

            dashTimer = 0f;
            isDashing = true;
            dashCooldownTimer = dashCooldown;

            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
                    $"[PlayerMovement] Dash : " +
                    $"{dashDirection} | " +
                    $"Cooldown : {dashCooldown}"
                );
            }
        }

        private void HandleMovement()
        {
            if (isDashing)
                return;

            Vector3 movement =
                GetMovementDirection(
                    moveInput
                );

            movement *= moveSpeed;

            MoveWithMovementSpaceLimits(
                movement * Time.deltaTime
            );
        }

        private Vector3 GetMovementDirection(
            Vector2 input
        )
        {
            Vector3 right =
                transform.right;

            Vector3 forward =
                transform.forward;

            Vector3 direction =
                right * input.x +
                forward * input.y;

            direction.y = 0f;

            return Vector3.ClampMagnitude(
                direction,
                1f
            );
        }

        private void UpdateSpriteDirection(
            Vector2 input
        )
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

        private void UpdateCharacterRotation()
        {
            if (!followMovementSpaceRotation)
                return;

            if (movementSpace == null)
                return;

            float targetY =
                movementSpace.eulerAngles.y;

            float currentY =
                transform.eulerAngles.y;

            float smoothedY =
                Mathf.SmoothDampAngle(
                    currentY,
                    targetY,
                    ref movementSpaceRotationVelocity,
                    movementSpaceRotationSmoothTime
                );

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    smoothedY,
                    0f
                );
        }

        private void CalculateJumpDuration()
        {
            if (gravity >= 0f)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(PlayerMovement)}] " +
                    "Gravity must be negative.",
                    this
                );

                jumpDuration = 0.1f;

                return;
            }

            float jumpVelocity =
                Mathf.Sqrt(
                    jumpHeight *
                    -2f *
                    gravity
                );

            float physicalJumpDuration =
                (-2f * jumpVelocity) /
                gravity;

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
            if (isDashing || !isJumping)
                return;

            float progress =
                Mathf.Clamp01(
                    jumpTimer /
                    jumpDuration
                );

            float curveValue =
                jumpCurve.Evaluate(
                    progress
                );

            float currentHeight =
                curveValue *
                jumpHeight;

            float deltaHeight =
                currentHeight -
                previousJumpHeight;

            Vector3 jumpMovement =
                Vector3.up *
                deltaHeight;

            MoveWithMovementSpaceLimits(
                jumpMovement
            );

            previousJumpHeight =
                currentHeight;

            jumpTimer +=
                Time.deltaTime;

            if (jumpTimer >= jumpDuration)
                EndJump();
        }

        private void EndJump()
        {
            isJumping = false;
            jumpTimer = 0f;
            previousJumpHeight = 0f;
            verticalVelocity = -2f;

            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
                    "[PlayerMovement] Jump End"
                );
            }
        }

        private void HandleGravity()
        {
            if (isDashing || isJumping)
                return;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                return;
            }

            verticalVelocity +=
                gravity *
                Time.deltaTime;

            Vector3 gravityMovement =
                Vector3.up *
                (
                    verticalVelocity *
                    Time.deltaTime
                );

            MoveWithMovementSpaceLimits(
                gravityMovement
            );
        }

        private void HandleDash()
        {
            if (!isDashing)
                return;

            float previousTime =
                dashTimer;

            dashTimer +=
                Time.deltaTime;

            float previousProgress =
                Mathf.Clamp01(
                    previousTime /
                    dashDuration
                );

            float currentProgress =
                Mathf.Clamp01(
                    dashTimer /
                    dashDuration
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
                (
                    dashDistance *
                    deltaCurveValue
                );

            MoveWithMovementSpaceLimits(
                dashMovement
            );

            if (dashTimer >= dashDuration)
            {
                dashTimer = 0f;
                isDashing = false;
                verticalVelocity = -2f;

                if (debugEnabled)
                {
                    UnityEngine.Debug.Log(
                        "[PlayerMovement] Dash End"
                    );
                }
            }
        }

        private void HandleDashCooldown()
        {
            if (dashCooldownTimer <= 0f)
                return;

            dashCooldownTimer -=
                Time.deltaTime;

            if (dashCooldownTimer < 0f)
                dashCooldownTimer = 0f;
        }

        private void MoveWithMovementSpaceLimits(
            Vector3 movement
        )
        {
            if (movementSpace == null)
            {
                controller.Move(movement);
                return;
            }

            Vector3 targetPosition =
                transform.position +
                movement;

            Vector3 localTargetPosition =
                movementSpace.InverseTransformPoint(
                    targetPosition
                );

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

            if (currentMovementSpace != null)
            {
                MovementSpace connectedMovementSpace =
                    GetConnectedMovementSpace(
                        localTargetPosition
                    );

                if (connectedMovementSpace != null)
                {
                    SetMovementSpace(
                        connectedMovementSpace.transform
                    );

                    /*
                     * Le Movement Space vient de changer.
                     *
                     * On doit donc recalculer la position cible
                     * dans le nouveau repère local.
                     */
                    localTargetPosition =
                        movementSpace.InverseTransformPoint(
                            targetPosition
                        );
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

                // DEPTH MIN
                if (
                    currentMovementSpace.UseDepthMin &&
                    localTargetPosition.z <
                    currentMovementSpace.MinDepth
                )
                {
                    localTargetPosition.z =
                        currentMovementSpace.MinDepth;
                }

                // DEPTH MAX
                if (
                    currentMovementSpace.UseDepthMax &&
                    localTargetPosition.z >
                    currentMovementSpace.MaxDepth
                )
                {
                    localTargetPosition.z =
                        currentMovementSpace.MaxDepth;
                }

                // SIDE LEFT
                if (
                    currentMovementSpace.UseSideLeft &&
                    localTargetPosition.x <
                    currentMovementSpace.MinSide
                )
                {
                    localTargetPosition.x =
                        currentMovementSpace.MinSide;
                }

                // SIDE RIGHT
                if (
                    currentMovementSpace.UseSideRight &&
                    localTargetPosition.x >
                    currentMovementSpace.MaxSide
                )
                {
                    localTargetPosition.x =
                        currentMovementSpace.MaxSide;
                }
            }

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

        private MovementSpace GetConnectedMovementSpace(
            Vector3 localTargetPosition
        )
        {
            if (currentMovementSpace == null)
                return null;

            /*
             * ---------------------------------------------------------
             * DEPTH MIN
             * ---------------------------------------------------------
             *
             * Si la limite est passable et que le joueur la dépasse,
             * on cherche le Movement Space connecté.
             */

            if (
                !currentMovementSpace.UseDepthMin &&
                localTargetPosition.z <
                currentMovementSpace.MinDepth
            )
            {
                return currentMovementSpace.DepthMinConnection;
            }

            /*
             * ---------------------------------------------------------
             * DEPTH MAX
             * ---------------------------------------------------------
             */

            if (
                !currentMovementSpace.UseDepthMax &&
                localTargetPosition.z >
                currentMovementSpace.MaxDepth
            )
            {
                return currentMovementSpace.DepthMaxConnection;
            }

            /*
             * ---------------------------------------------------------
             * SIDE LEFT
             * ---------------------------------------------------------
             */

            if (
                !currentMovementSpace.UseSideLeft &&
                localTargetPosition.x <
                currentMovementSpace.MinSide
            )
            {
                return currentMovementSpace.SideLeftConnection;
            }

            /*
             * ---------------------------------------------------------
             * SIDE RIGHT
             * ---------------------------------------------------------
             */

            if (
                !currentMovementSpace.UseSideRight &&
                localTargetPosition.x >
                currentMovementSpace.MaxSide
            )
            {
                return currentMovementSpace.SideRightConnection;
            }

            return null;
        }

        private void CacheMovementSpaceComponent()
        {
            if (movementSpace == null)
            {
                currentMovementSpace = null;
                return;
            }

            currentMovementSpace =
                movementSpace.GetComponent<MovementSpace>();

            if (currentMovementSpace == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(PlayerMovement)}] " +
                    $"Le Movement Space '{movementSpace.name}' " +
                    "ne possède pas de composant MovementSpace. " +
                    "Aucune limite personnalisée ne sera appliquée.",
                    movementSpace
                );
            }
        }

        public void SetMovementSpace(
            Transform newMovementSpace
        )
        {
            if (newMovementSpace == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(PlayerMovement)}] " +
                    "Impossible d'assigner un Movement Space null.",
                    this
                );

                return;
            }

            movementSpace =
                newMovementSpace;

            movementSpaceRotationVelocity =
                0f;

            CacheMovementSpaceComponent();

            if (debugEnabled)
            {
                UnityEngine.Debug.Log(
                    $"[PlayerMovement] " +
                    $"Movement Space changé : " +
                    $"{movementSpace.name}",
                    this
                );
            }
        }

        public Transform GetMovementSpace()
        {
            return movementSpace;
        }
    }
}