using UnityEngine;

namespace SynthOfRage.Scripts.CameraSystem
{
    [RequireComponent(typeof(Collider))]
    public class CameraShakeTrigger : MonoBehaviour
    {
        public enum DirectionMode
        {
            Preset,
            Custom,
            TowardTarget,
            TowardSource
        }

        [Header("Shake")]
        [SerializeField] private CameraShakePreset preset;

        [Header("Trigger")]
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private bool playOnce = true;

        [Header("Direction")]
        [SerializeField] private DirectionMode directionMode = DirectionMode.Preset;

        [Tooltip("Utilisé uniquement avec Direction Mode = Custom.")]
        [SerializeField] private Vector2 customDirection = Vector2.right;

        [Tooltip("Origine utilisée pour calculer Toward Target / Toward Source. Si vide, le Transform du Trigger est utilisé.")]
        [SerializeField] private Transform directionOrigin;

        private bool hasPlayed;

        private void OnTriggerEnter(Collider other)
        {
            if (playOnce && hasPlayed) return;
            if (!other.CompareTag(targetTag)) return;
            if (preset == null) return;

            hasPlayed = true;

            switch (directionMode)
            {
                case DirectionMode.Preset:
                    CameraShakeManager.PlayShake(preset);
                    break;

                case DirectionMode.Custom:
                    CameraShakeManager.PlayShake(preset, customDirection);
                    break;

                case DirectionMode.TowardTarget:
                    PlayRelativeShake(other, false);
                    break;

                case DirectionMode.TowardSource:
                    PlayRelativeShake(other, true);
                    break;
            }
        }

        private void PlayRelativeShake(Collider other, bool reverse)
        {
            Vector3 origin = directionOrigin != null ? directionOrigin.position : transform.position;
            Vector3 targetPosition = other.bounds.center;
            Vector3 direction = targetPosition - origin;

            if (reverse) direction = -direction;

            CameraShakeManager.PlayShakeWorld(preset, direction);
        }

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();

            if (trigger != null)
                trigger.isTrigger = true;
        }

        public void ResetTrigger() => hasPlayed = false;
    }
}