using Unity.Cinemachine;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SynthOfRage.Scripts.CameraSystem
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShakeManager : MonoBehaviour
    {
        public static CameraShakeManager Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        [Header("References")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Debug")]
        [SerializeField] private bool debugEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                UnityEngine.Debug.LogError($"[{nameof(CameraShakeManager)}] Plusieurs CameraShakeManager présents dans la scène.", this);
                enabled = false;
                return;
            }

            Instance = this;

            if (impulseSource == null)
                impulseSource = GetComponent<CinemachineImpulseSource>();

            if (impulseSource == null)
                UnityEngine.Debug.LogError($"[{nameof(CameraShakeManager)}] CinemachineImpulseSource introuvable.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Reset()
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        public void Play(CameraShakePreset preset)
        {
            if (preset == null)
                return;

            Play(preset, preset.Direction, 1f);
        }

        public void Play(CameraShakePreset preset, Vector2 direction)
        {
            Play(preset, direction, 1f);
        }

        public void Play(CameraShakePreset preset, Vector2 direction, float powerMultiplier)
        {
            if (preset == null || impulseSource == null || powerMultiplier <= 0f)
                return;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = preset.Direction;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            direction.Normalize();

            CinemachineImpulseDefinition definition = impulseSource.ImpulseDefinition;

            definition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            definition.ImpulseShape = GetCinemachineShape(preset.Shape);
            definition.ImpulseDuration = preset.Duration;

            Vector3 velocity = new(direction.x, direction.y, 0f);
            velocity *= preset.Power * powerMultiplier;

            impulseSource.GenerateImpulseWithVelocity(velocity);

            if (debugEnabled)
                UnityEngine.Debug.Log($"[{nameof(CameraShakeManager)}] Play '{preset.name}' | Shape={preset.Shape} | Duration={preset.Duration:F2} | Direction={direction} | Power={preset.Power * powerMultiplier:F2}", this);
        }

        public void Stop()
        {
            StopAllShakes();
        }

        public static void PlayShake(CameraShakePreset preset)
        {
            Instance?.Play(preset);
        }

        public static void PlayShake(CameraShakePreset preset, Vector2 direction)
        {
            Instance?.Play(preset, direction);
        }

        public static void PlayShake(CameraShakePreset preset, Vector2 direction, float powerMultiplier)
        {
            Instance?.Play(preset, direction, powerMultiplier);
        }

        public static void StopAllShakes()
        {
            CinemachineImpulseManager.Instance.Clear();
        }

        private static CinemachineImpulseDefinition.ImpulseShapes GetCinemachineShape(CameraShakePreset.ShakeShape shape)
        {
            return shape switch
            {
                CameraShakePreset.ShakeShape.Recoil => CinemachineImpulseDefinition.ImpulseShapes.Recoil,
                CameraShakePreset.ShakeShape.Bump => CinemachineImpulseDefinition.ImpulseShapes.Bump,
                CameraShakePreset.ShakeShape.Explosion => CinemachineImpulseDefinition.ImpulseShapes.Explosion,
                CameraShakePreset.ShakeShape.Rumble => CinemachineImpulseDefinition.ImpulseShapes.Rumble,
                _ => CinemachineImpulseDefinition.ImpulseShapes.Rumble
            };
        }
        
        public void PlayWorldDirection(CameraShakePreset preset, Vector3 worldDirection, float powerMultiplier = 1f)
        {
            if (preset == null || worldDirection.sqrMagnitude <= 0.0001f)
            {
                Play(preset);
                return;
            }

            Camera camera = Camera.main;

            if (camera == null)
            {
                Play(preset, new Vector2(worldDirection.x, worldDirection.y), powerMultiplier);
                return;
            }

            worldDirection.Normalize();

            Vector2 screenDirection = new(
                Vector3.Dot(worldDirection, camera.transform.right),
                Vector3.Dot(worldDirection, camera.transform.up)
            );

            if (screenDirection.sqrMagnitude <= 0.0001f)
                screenDirection = preset.Direction;

            Play(preset, screenDirection.normalized, powerMultiplier);
        }

        public static void PlayShakeWorld(CameraShakePreset preset, Vector3 worldDirection, float powerMultiplier = 1f)
        {
            Instance?.PlayWorldDirection(preset, worldDirection, powerMultiplier);
        }
    }
}