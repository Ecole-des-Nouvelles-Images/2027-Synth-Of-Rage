using UnityEngine;
using SynthOfRage.Scripts.Player;

namespace _Dev.Vincent.Camera_Setup.Scripts.VCameraSystem
{
    public class CombatKnot : MonoBehaviour
    {
        // =========================================================
        // SPLINE
        // =========================================================

        [Header("Spline")]

        [Tooltip(
            "Index du Knot sur la spline qui déclenche le combat."
        )]
        [SerializeField] private int knotIndex;


        // =========================================================
        // COMBAT
        // =========================================================

        [Header("Combat")]

        [Tooltip(
            "Centre de référence du combat."
        )]
        [SerializeField] private Transform combatCenter;

        [Tooltip(
            "Movement Space utilisé pendant ce combat."
        )]
        [SerializeField] private Transform movementSpace;


        // =========================================================
        // NEXT MOVEMENT SPACE
        // =========================================================

        [Header("Next Movement Space")]

        [Tooltip(
            "Movement Space utilisé après la fin de ce combat."
        )]
        [SerializeField] private MovementSpace nextMovementSpace;


        // =========================================================
        // CAMERA
        // =========================================================

        [Header("Camera")]

        [Tooltip(
            "Preset caméra appliqué lors de l'entrée en combat."
        )]
        [SerializeField] private VCameraPreset cameraPreset;


        // =========================================================
        // DEBUG GIZMOS
        // =========================================================

        [Header("Debug Gizmos")]

        [SerializeField] private bool showGizmos = true;

        [SerializeField] private bool showCameraFrustum = true;

        [SerializeField] private float centerGizmoSize = 0.5f;

        [SerializeField] private float cameraGizmoSize = 0.35f;

        [SerializeField] private float axisLength = 1.5f;


        // =========================================================
        // PUBLIC API
        // =========================================================

        public int KnotIndex =>
            knotIndex;

        public Transform CombatCenter =>
            combatCenter;

        public Transform MovementSpace =>
            movementSpace;

        public MovementSpace NextMovementSpace =>
            nextMovementSpace;

        public VCameraPreset CameraPreset =>
            cameraPreset;


        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            DrawCombatCenter();
            DrawMovementSpace();
            DrawNextMovementSpace();
            DrawPresetCamera();
            DrawKnotLabel();
        }


        // =========================================================
        // COMBAT CENTER
        // =========================================================

        private void DrawCombatCenter()
        {
            if (combatCenter == null)
                return;

            Gizmos.matrix =
                combatCenter.localToWorldMatrix;

            Gizmos.DrawWireSphere(
                Vector3.zero,
                centerGizmoSize
            );

            DrawAxes(
                Vector3.zero,
                Quaternion.identity,
                axisLength
            );

            Gizmos.matrix =
                Matrix4x4.identity;
        }


        // =========================================================
        // COMBAT MOVEMENT SPACE
        // =========================================================

        private void DrawMovementSpace()
        {
            if (movementSpace == null)
                return;

            Gizmos.matrix =
                movementSpace.localToWorldMatrix;

            DrawAxes(
                Vector3.zero,
                Quaternion.identity,
                axisLength
            );

            Gizmos.matrix =
                Matrix4x4.identity;

            Vector3 center =
                movementSpace.position;

            Vector3 right =
                movementSpace.right * 2f;

            Vector3 forward =
                movementSpace.forward * 2f;

            Vector3 p1 =
                center - right;

            Vector3 p2 =
                center + right;

            Vector3 p3 =
                center + right + forward;

            Vector3 p4 =
                center - right + forward;

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
        }


        // =========================================================
        // NEXT MOVEMENT SPACE
        // =========================================================

        private void DrawNextMovementSpace()
        {
            if (nextMovementSpace == null)
                return;

            Vector3 start =
                combatCenter != null
                    ? combatCenter.position
                    : transform.position;

            Vector3 end =
                nextMovementSpace.transform.position;

            Gizmos.DrawLine(
                start,
                end
            );

            Gizmos.DrawWireSphere(
                end,
                0.2f
            );
        }


        // =========================================================
        // PRESET CAMERA
        // =========================================================

        private void DrawPresetCamera()
        {
            if (
                combatCenter == null ||
                cameraPreset == null
            )
            {
                return;
            }

            Vector3 cameraPosition =
                combatCenter.TransformPoint(
                    cameraPreset.CameraOffset
                );

            Quaternion cameraRotation =
                combatCenter.rotation *
                Quaternion.Euler(
                    cameraPreset.CameraRotation
                );

            Gizmos.DrawLine(
                combatCenter.position,
                cameraPosition
            );

            Gizmos.matrix =
                Matrix4x4.TRS(
                    cameraPosition,
                    cameraRotation,
                    Vector3.one
                );

            Gizmos.DrawWireSphere(
                Vector3.zero,
                cameraGizmoSize
            );

            Gizmos.DrawLine(
                Vector3.zero,
                Vector3.forward * axisLength
            );

            if (showCameraFrustum)
            {
                Gizmos.DrawFrustum(
                    Vector3.zero,
                    cameraPreset.FieldOfView,
                    5f,
                    0.1f,
                    1.777f
                );
            }

            Gizmos.matrix =
                Matrix4x4.identity;
        }


        // =========================================================
        // AXES
        // =========================================================

        private void DrawAxes(
            Vector3 position,
            Quaternion rotation,
            float length
        )
        {
            Vector3 right =
                rotation *
                Vector3.right *
                length;

            Vector3 up =
                rotation *
                Vector3.up *
                length;

            Vector3 forward =
                rotation *
                Vector3.forward *
                length;

            Gizmos.DrawLine(
                position,
                position + right
            );

            Gizmos.DrawLine(
                position,
                position + up
            );

            Gizmos.DrawLine(
                position,
                position + forward
            );
        }


        // =========================================================
        // KNOT LABEL
        // =========================================================

        private void DrawKnotLabel()
        {
#if UNITY_EDITOR

            Vector3 labelPosition =
                transform.position;

            if (combatCenter != null)
            {
                labelPosition =
                    combatCenter.position +
                    Vector3.up * 1.5f;
            }

            string nextSpaceLabel =
                nextMovementSpace != null
                    ? nextMovementSpace.name
                    : "NONE";

            UnityEditor.Handles.Label(
                labelPosition,
                $"Combat Knot : {knotIndex}\n" +
                $"Next Space : {nextSpaceLabel}"
            );

#endif
        }
    }
}