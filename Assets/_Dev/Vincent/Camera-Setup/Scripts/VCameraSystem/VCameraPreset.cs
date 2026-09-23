using UnityEngine;

namespace _Dev.Vincent.Camera_Setup.Scripts.VCameraSystem
{
    [CreateAssetMenu(
        fileName = "VCameraPreset",
        menuName = "SynthOfRage/VCameraSystem/VCamera Preset"
    )]
    public class VCameraPreset : ScriptableObject
    {
        [Header("Camera Position")]
        [Tooltip(
            "Position locale de la caméra par rapport au Combat Center."
        )]
        [SerializeField] private Vector3 cameraOffset;

        [Header("Camera Rotation")]
        [Tooltip(
            "Rotation locale de la caméra par rapport au Combat Center."
        )]
        [SerializeField] private Vector3 cameraRotation;

        [Header("Movement Space")]
        [Tooltip(
            "Rotation appliquée au Movement Space pendant ce preset."
        )]
        [SerializeField] private Vector3 movementSpaceRotation;

        [Header("Lens")]
        [Tooltip(
            "Vertical Field Of View de la caméra."
        )]
        [SerializeField] private float fieldOfView = 60f;

        public Vector3 CameraOffset => cameraOffset;

        public Vector3 CameraRotation => cameraRotation;

        public Vector3 MovementSpaceRotation =>
            movementSpaceRotation;

        public float FieldOfView => fieldOfView;
    }
}