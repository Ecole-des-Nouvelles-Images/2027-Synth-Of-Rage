using UnityEngine;

namespace SynthOfRage.Scripts.CameraSystem
{
    [CreateAssetMenu(fileName = "VCameraShake_", menuName = "SynthOfRage/VCameraSystem/Camera Shake Preset")]
    public class CameraShakePreset : ScriptableObject
    {
        public enum ShakeShape
        {
            Recoil,
            Bump,
            Explosion,
            Rumble
        }

        [Header("Shake")]
        [SerializeField] private ShakeShape shape = ShakeShape.Rumble;
        [SerializeField, Min(0.01f)] private float duration = 0.2f;
        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField, Min(0f)] private float power = 1f;

        public ShakeShape Shape => shape;
        public float Duration => duration;
        public float Power => power;
        public Vector2 Direction => direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;

        private void OnValidate()
        {
            duration = Mathf.Max(0.01f, duration);
            power = Mathf.Max(0f, power);

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();
        }
    }
}