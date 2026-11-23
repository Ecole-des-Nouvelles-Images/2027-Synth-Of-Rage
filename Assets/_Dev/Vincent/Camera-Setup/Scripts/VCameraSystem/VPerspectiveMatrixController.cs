
using UnityEngine;

namespace _Dev.Vincent.Camera_Setup.Scripts.VCameraSystem
{
    [ExecuteAlways]
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class VPerspectiveMatrixController : MonoBehaviour
    {
        [Header("Projection")]
        [SerializeField] private bool enableCustomProjection = true;

        [Tooltip("Positive = Perspective compression. Negative = Optical zoom. 0 = Standard perspective.")]
        [SerializeField, Range(-50f, 50f)]
        private float depthCompression = 0f;

        [Header("Zoom")]
        [Tooltip("Zoom sensitivity for negative compression values.")]
        [SerializeField, Min(0f)]
        private float zoomSensitivity = 0.05f;

        [Header("Projection Scale")]
        [SerializeField, Range(0.5f, 1.5f)]
        private float horizontalScale = 1f;

        [SerializeField, Range(0.5f, 1.5f)]
        private float verticalScale = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Camera targetCamera;

        public float DepthCompression => depthCompression;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            ApplyProjection();
        }

        private void OnDisable()
        {
            RestoreProjection();
        }

        private void OnValidate()
        {
            depthCompression = Mathf.Clamp(depthCompression, -50f, 50f);
            zoomSensitivity = Mathf.Max(0f, zoomSensitivity);

            if (isActiveAndEnabled)
                ApplyProjection();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();

            ApplyProjection();
        }

        private void ApplyProjection()
        {
            if (targetCamera == null)
                return;

            if (!enableCustomProjection)
            {
                RestoreProjection();
                return;
            }

            if (targetCamera.orthographic)
                return;

            float near = targetCamera.nearClipPlane;
            float far = targetCamera.farClipPlane;

            if (near <= 0f || far <= near)
                return;

            float compression = depthCompression;

            // Base perspective projection.
            Matrix4x4 projection = Matrix4x4.Perspective(
                targetCamera.fieldOfView,
                targetCamera.aspect,
                near,
                far
            );

            // Positive compression:
            // Custom perspective with an offset homogeneous denominator.
            if (compression > 0f)
            {
                float n = near;
                float f = far;
                float k = compression;

                // Preserve the near and far clipping planes.
                float a = (f + n + 2f * k) / (f - n);
                float b = -n - k - a * n;

                projection.m22 = -a;
                projection.m23 = b;

                projection.m32 = -1f;
                projection.m33 = k;
            }
            // Negative compression:
            // Standard perspective with optical zoom.
            else if (compression < 0f)
            {
                float zoom = 1f + Mathf.Abs(compression) * zoomSensitivity;

                projection.m00 *= zoom;
                projection.m11 *= zoom;
            }

            // Independent horizontal / vertical scaling.
            projection.m00 *= horizontalScale;
            projection.m11 *= verticalScale;

            targetCamera.projectionMatrix = projection;

            if (showDebug)
            {
                Debug.Log(
                    $"Custom Projection Applied | " +
                    $"Compression: {compression:F2} | " +
                    $"Zoom: {(compression < 0f ? 1f + Mathf.Abs(compression) * zoomSensitivity : 1f):F2} | " +
                    $"FOV: {targetCamera.fieldOfView:F2} | " +
                    $"Aspect: {targetCamera.aspect:F2}",
                    this
                );
            }
        }

        public void RestoreProjection()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();

            if (targetCamera != null)
                targetCamera.ResetProjectionMatrix();
        }
    }
}
