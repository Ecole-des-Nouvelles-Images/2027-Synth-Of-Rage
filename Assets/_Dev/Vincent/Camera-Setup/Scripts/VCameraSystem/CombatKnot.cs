using System.Collections.Generic;
using SynthOfRage.Scripts.Gameplay.Unit.Player.Movement.Utils;
using UnityEngine;

namespace SynthOfRage.Scripts.CameraSystem
{
    public class CombatKnot : MonoBehaviour
    {
        [Header("Spline")]
        [Tooltip("Index du Knot sur la spline qui déclenche le combat.")]
        [SerializeField] private int knotIndex;

        [Header("Combat")]
        [Tooltip("Centre de référence du combat.")]
        [SerializeField] private Transform combatCenter;

        [Tooltip("Movement Space utilisé pendant ce combat.")]
        [SerializeField] private Transform movementSpace;

        [Header("Next Movement Space")]
        [Tooltip("Movement Space utilisé après la fin de ce combat.")]
        [SerializeField] private MovementSpace nextMovementSpace;

        [Header("Camera")]
        [Tooltip("Preset caméra appliqué lors de l'entrée en combat.")]
        [SerializeField] private VCameraPreset cameraPreset;

        [Header("Camera Shake")]
        [Tooltip("Shake joué au début du combat.")]
        [SerializeField] private CameraShakePreset startShakePreset;

        [Tooltip("Délai avant le shake de début de combat.")]
        [SerializeField, Min(0f)] private float startShakeDelay;

        [Tooltip("Shake joué à la fin du combat.")]
        [SerializeField] private CameraShakePreset endShakePreset;

        [Tooltip("Délai avant le shake de fin de combat.")]
        [SerializeField, Min(0f)] private float endShakeDelay;

        [Header("Combat Objects")]
        [Tooltip("Objets activés au début du combat.")]
        [SerializeField] private List<GameObject> objectsToEnable = new();

        [Tooltip("Objets désactivés à la fin du combat.")]
        [SerializeField] private List<GameObject> objectsToDisable = new();

        [Header("Debug Gizmos")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showCameraFrustum = true;
        [SerializeField, Min(0f)] private float centerGizmoSize = 0.5f;
        [SerializeField, Min(0f)] private float cameraGizmoSize = 0.35f;
        [SerializeField, Min(0f)] private float axisLength = 1.5f;

        public int KnotIndex => knotIndex;
        public Transform CombatCenter => combatCenter;
        public Transform MovementSpace => movementSpace;
        public MovementSpace NextMovementSpace => nextMovementSpace;
        public VCameraPreset CameraPreset => cameraPreset;

        public CameraShakePreset StartShakePreset => startShakePreset;
        public float StartShakeDelay => startShakeDelay;
        public CameraShakePreset EndShakePreset => endShakePreset;
        public float EndShakeDelay => endShakeDelay;

        public void EnableCombatObjects() => SetObjectsActive(objectsToEnable, true);
        public void DisableCombatObjects() => SetObjectsActive(objectsToDisable, false);

        private static void SetObjectsActive(List<GameObject> objects, bool active)
        {
            if (objects == null) return;

            for (int i = 0; i < objects.Count; i++)
                if (objects[i] != null)
                    objects[i].SetActive(active);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            DrawCombatCenter();
            DrawMovementSpace();
            DrawNextMovementSpace();
            DrawPresetCamera();
            DrawKnotLabel();
        }

        private void DrawCombatCenter()
        {
            if (combatCenter == null) return;

            Gizmos.matrix = combatCenter.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, centerGizmoSize);
            DrawAxes(Vector3.zero, Quaternion.identity, axisLength);
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DrawMovementSpace()
        {
            if (movementSpace == null) return;

            Gizmos.matrix = movementSpace.localToWorldMatrix;
            DrawAxes(Vector3.zero, Quaternion.identity, axisLength);
            Gizmos.matrix = Matrix4x4.identity;

            Vector3 center = movementSpace.position;
            Vector3 right = movementSpace.right * 2f;
            Vector3 forward = movementSpace.forward * 2f;

            Vector3 p1 = center - right;
            Vector3 p2 = center + right;
            Vector3 p3 = center + right + forward;
            Vector3 p4 = center - right + forward;

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
        }

        private void DrawNextMovementSpace()
        {
            if (nextMovementSpace == null) return;

            Vector3 start = combatCenter != null ? combatCenter.position : transform.position;
            Vector3 end = nextMovementSpace.transform.position;

            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.2f);
        }

        private void DrawPresetCamera()
        {
            if (combatCenter == null || cameraPreset == null) return;

            Vector3 cameraPosition = combatCenter.TransformPoint(cameraPreset.CameraOffset);
            Quaternion cameraRotation = combatCenter.rotation * Quaternion.Euler(cameraPreset.CameraRotation);

            Gizmos.DrawLine(combatCenter.position, cameraPosition);

            Gizmos.matrix = Matrix4x4.TRS(cameraPosition, cameraRotation, Vector3.one);
            Gizmos.DrawWireSphere(Vector3.zero, cameraGizmoSize);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * axisLength);

            if (showCameraFrustum)
                Gizmos.DrawFrustum(Vector3.zero, cameraPreset.FieldOfView, 5f, 0.1f, 16f / 9f);

            Gizmos.matrix = Matrix4x4.identity;
        }

        private static void DrawAxes(Vector3 position, Quaternion rotation, float length)
        {
            Gizmos.DrawLine(position, position + rotation * Vector3.right * length);
            Gizmos.DrawLine(position, position + rotation * Vector3.up * length);
            Gizmos.DrawLine(position, position + rotation * Vector3.forward * length);
        }

        private void DrawKnotLabel()
        {
#if UNITY_EDITOR
            Vector3 position = combatCenter != null ? combatCenter.position + Vector3.up * 1.5f : transform.position;
            string nextSpace = nextMovementSpace != null ? nextMovementSpace.name : "NONE";

            UnityEditor.Handles.Label(position, $"Combat Knot : {knotIndex}\nNext Space : {nextSpace}");
#endif
        }
    }
}