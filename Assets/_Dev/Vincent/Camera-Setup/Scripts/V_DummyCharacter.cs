using UnityEngine;
using UnityEngine.InputSystem;

namespace _Dev.Vincent.Camera_Setup.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class VDummyCharacter : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Movement Limits")]
        [SerializeField] private float minZ = -3.5f;
        [SerializeField] private float maxZ = 3.5f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -20f;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference horizontalAction;
        [SerializeField] private InputActionReference verticalAction;
        [SerializeField] private InputActionReference jumpAction;

        private CharacterController controller;
        private Vector3 velocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            horizontalAction.action.Enable();
            verticalAction.action.Enable();
            jumpAction.action.Enable();
        }

        private void OnDisable()
        {
            horizontalAction.action.Disable();
            verticalAction.action.Disable();
            jumpAction.action.Disable();
        }

        private void Update()
        {
            Move();
            ApplyGravity();
            Jump();
        }
        
        private void Move()
        {
            float horizontal = horizontalAction.action.ReadValue<float>();
            float vertical = verticalAction.action.ReadValue<float>();

            Vector3 movement = new Vector3(horizontal, 0f, vertical);

            // Évite d'aller plus vite en diagonale
            movement = Vector3.ClampMagnitude(movement, 1f);

            movement *= moveSpeed * Time.deltaTime;

            // Calcule la position finale souhaitée
            Vector3 targetPosition = transform.position + movement;

            // Clamp la position finale sur Z
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);

            // Recalcule le mouvement réel autorisé
            movement = targetPosition - transform.position;

            controller.Move(movement);
        }
        
        private void Jump()
        {
            if (jumpAction.action.WasPressedThisFrame() && controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            velocity.y += gravity * Time.deltaTime;

            controller.Move(velocity * Time.deltaTime);
        }
    }
}

