using SynthOfRage.Scripts.Gameplay.SO.Camera;
using Unity.Cinemachine;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace _Dev.Vincent.Camera_Setup.Scripts.VCameraSystem.CameraShakes
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public partial class CameraShakeManager : MonoBehaviour
    {
        [AutoStaticsCleanup] public static CameraShakeManager Instance { get; private set; }
        [AutoStaticsCleanup] public static bool HasInstance => Instance != null;

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

        public void Play(VCameraShakePresetSO presetSo)
        {
            if (presetSo == null)
                return;

            Play(presetSo, presetSo.Direction, 1f);
        }

        public void Play(VCameraShakePresetSO presetSo, Vector2 direction)
        {
            Play(presetSo, direction, 1f);
        }

        public void Play(VCameraShakePresetSO presetSo, Vector2 direction, float powerMultiplier)
        {
            if (presetSo == null || impulseSource == null || powerMultiplier <= 0f)
                return;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = presetSo.Direction;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            direction.Normalize();

            CinemachineImpulseDefinition definition = impulseSource.ImpulseDefinition;

            definition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            definition.ImpulseShape = GetCinemachineShape(presetSo.Shape);
            definition.ImpulseDuration = presetSo.Duration;

            Vector3 velocity = new(direction.x, direction.y, 0f);
            velocity *= presetSo.Power * powerMultiplier;

            impulseSource.GenerateImpulseWithVelocity(velocity);

            if (debugEnabled)
                UnityEngine.Debug.Log($"[{nameof(CameraShakeManager)}] Play '{presetSo.name}' | Shape={presetSo.Shape} | Duration={presetSo.Duration:F2} | Direction={direction} | Power={presetSo.Power * powerMultiplier:F2}", this);
        }

        public void Stop()
        {
            StopAllShakes();
        }

        public static void PlayShake(VCameraShakePresetSO presetSo)
        {
            Instance?.Play(presetSo);
        }

        public static void PlayShake(VCameraShakePresetSO presetSo, Vector2 direction)
        {
            Instance?.Play(presetSo, direction);
        }

        public static void PlayShake(VCameraShakePresetSO presetSo, Vector2 direction, float powerMultiplier)
        {
            Instance?.Play(presetSo, direction, powerMultiplier);
        }

        public static void StopAllShakes()
        {
            CinemachineImpulseManager.Instance.Clear();
        }

        private static CinemachineImpulseDefinition.ImpulseShapes GetCinemachineShape(VCameraShakePresetSO.ShakeShape shape)
        {
            return shape switch
            {
                VCameraShakePresetSO.ShakeShape.Recoil => CinemachineImpulseDefinition.ImpulseShapes.Recoil,
                VCameraShakePresetSO.ShakeShape.Bump => CinemachineImpulseDefinition.ImpulseShapes.Bump,
                VCameraShakePresetSO.ShakeShape.Explosion => CinemachineImpulseDefinition.ImpulseShapes.Explosion,
                VCameraShakePresetSO.ShakeShape.Rumble => CinemachineImpulseDefinition.ImpulseShapes.Rumble,
                _ => CinemachineImpulseDefinition.ImpulseShapes.Rumble
            };
        }

        public void PlayWorldDirection(VCameraShakePresetSO presetSo, Vector3 worldDirection, float powerMultiplier = 1f)
        {
            if (presetSo == null || worldDirection.sqrMagnitude <= 0.0001f)
            {
                Play(presetSo);
                return;
            }

            Camera camera = Camera.main;

            if (camera == null)
            {
                Play(presetSo, new Vector2(worldDirection.x, worldDirection.y), powerMultiplier);
                return;
            }

            worldDirection.Normalize();

            Vector2 screenDirection = new(
                Vector3.Dot(worldDirection, camera.transform.right),
                Vector3.Dot(worldDirection, camera.transform.up)
            );

            if (screenDirection.sqrMagnitude <= 0.0001f)
                screenDirection = presetSo.Direction;

            Play(presetSo, screenDirection.normalized, powerMultiplier);
        }

        public static void PlayShakeWorld(VCameraShakePresetSO presetSo, Vector3 worldDirection, float powerMultiplier = 1f)
        {
            Instance?.PlayWorldDirection(presetSo, worldDirection, powerMultiplier);
        }
    }
}
