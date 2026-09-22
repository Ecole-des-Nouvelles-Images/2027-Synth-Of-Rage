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
        [SerializeField] private float gravity = -20f;

        [Header("Dash")]
        [SerializeField] private float dashDistance = 4f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField]
        private AnimationCurve dashCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private CharacterController controller;

        private Vector2 moveInput;
        private float verticalVelocity;

        private bool isDashing;
        private float dashTimer;
        private Vector3 dashDirection;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
            HandleGravity();
            HandleDash();
        }

        // ─────────────────────────────────────────────
        // Input
        // ─────────────────────────────────────────────

        private void HandlePlayerMove(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);

            UpdateSpriteDirection(input);

            if (debugEnabled)
                Debug.Log($"[PlayerMovement] Move Input : {moveInput}");
        }

        private void HandlePlayerJump()
        {
            if (!controller.isGrounded)
                return;

            verticalVelocity =
                Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (debugEnabled)
                Debug.Log("[PlayerMovement] Jump");
        }

        private void HandlePlayerDash()
        {
            if (isDashing)
                return;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                // Dash dans la direction donnée par le joueur.
                dashDirection = GetMovementDirection(moveInput);
            }
            else
            {
                // Aucun input :
                // dash dans le sens du personnage.
                dashDirection = transform.right;
            }

            dashDirection.y = 0f;
            dashDirection.Normalize();

            dashTimer = 0f;
            isDashing = true;

            if (debugEnabled)
                Debug.Log($"[PlayerMovement] Dash : {dashDirection}");
        }

        // ─────────────────────────────────────────────
        // Sprite
        // ─────────────────────────────────────────────

        private void UpdateSpriteDirection(Vector2 input)
        {
            if (spriteRenderer == null)
                return;

            // Gauche
            if (input.x < -0.01f)
            {
                spriteRenderer.flipX = true;
            }
            // Droite
            else if (input.x > 0.01f)
            {
                spriteRenderer.flipX = false;
            }

            // Aucun changement pour :
            // - Avant
            // - Arrière
            // - Aucun input
        }

        // ─────────────────────────────────────────────
        // Movement
        // ─────────────────────────────────────────────

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

        // ─────────────────────────────────────────────
        // Gravity / Jump
        // ─────────────────────────────────────────────

        private void HandleGravity()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 verticalMovement =
                Vector3.up *
                verticalVelocity *
                Time.deltaTime;

            MoveWithDepthLimit(verticalMovement);
        }

        // ─────────────────────────────────────────────
        // Dash
        // ─────────────────────────────────────────────

        private void HandleDash()
        {
            if (!isDashing)
                return;

            float previousTime = dashTimer;

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

            MoveWithDepthLimit(dashMovement);

            if (dashTimer >= dashDuration)
            {
                dashTimer = 0f;
                isDashing = false;

                if (debugEnabled)
                    Debug.Log("[PlayerMovement] Dash End");
            }
        }

        // ─────────────────────────────────────────────
        // Depth Limits
        // ─────────────────────────────────────────────

        private void MoveWithDepthLimit(Vector3 movement)
        {
            // Si les limites sont désactivées ou qu'aucun
            // MovementSpace n'est renseigné, déplacement normal.
            if (!useDepthLimits || movementSpace == null)
            {
                controller.Move(movement);
                return;
            }

            Vector3 targetPosition =
                transform.position + movement;

            // Convertit la position monde dans le référentiel
            // du MovementSpace.
            Vector3 localTargetPosition =
                movementSpace.InverseTransformPoint(
                    targetPosition
                );

            // Limite uniquement la profondeur.
            localTargetPosition.z =
                Mathf.Clamp(
                    localTargetPosition.z,
                    minDepth,
                    maxDepth
                );

            // Reconvertit la position dans le monde.
            targetPosition =
                movementSpace.TransformPoint(
                    localTargetPosition
                );

            // Calcule le mouvement réellement autorisé.
            Vector3 allowedMovement =
                targetPosition -
                transform.position;

            controller.Move(allowedMovement);
        }
    }
}