#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace SynthOfRage.Scripts.CameraSystem.Editor
{
    [CustomEditor(typeof(CameraShakePreset))]
    public class CameraShakePresetEditor : UnityEditor.Editor
    {
        private SerializedProperty shape;
        private SerializedProperty duration;
        private SerializedProperty direction;
        private SerializedProperty power;

        private const float PadSize = 150f;

        private void OnEnable()
        {
            shape = serializedObject.FindProperty("shape");
            duration = serializedObject.FindProperty("duration");
            direction = serializedObject.FindProperty("direction");
            power = serializedObject.FindProperty("power");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Shake", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(shape);
            EditorGUILayout.PropertyField(duration);
            EditorGUILayout.PropertyField(power);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Direction", EditorStyles.boldLabel);

            DrawDirectionPad();

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(direction);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(12f);

            DrawTestControls();
        }

        private void DrawDirectionPad()
        {
            Rect rect = GUILayoutUtility.GetRect(PadSize, PadSize, GUILayout.ExpandWidth(false));
            rect.x += (EditorGUIUtility.currentViewWidth - PadSize) * 0.5f - 10f;

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

            Vector2 center = rect.center;
            float radius = rect.width * 0.42f;

            Handles.BeginGUI();

            Handles.color = new Color(1f, 1f, 1f, 0.25f);
            Handles.DrawWireDisc(center, Vector3.forward, radius);

            Handles.DrawLine(new Vector3(center.x - radius, center.y), new Vector3(center.x + radius, center.y));
            Handles.DrawLine(new Vector3(center.x, center.y - radius), new Vector3(center.x, center.y + radius));

            Vector2 value = direction.vector2Value;

            if (value.sqrMagnitude > 0.0001f)
                value.Normalize();

            Vector2 guiDirection = new(value.x, -value.y);
            Vector2 end = center + guiDirection * radius;

            Handles.color = Color.cyan;
            Handles.DrawAAPolyLine(4f, center, end);

            Vector2 perpendicular = new(-guiDirection.y, guiDirection.x);
            Vector2 arrowA = end - guiDirection * 14f + perpendicular * 7f;
            Vector2 arrowB = end - guiDirection * 14f - perpendicular * 7f;

            Handles.DrawAAPolyLine(4f, arrowA, end, arrowB);

            Handles.EndGUI();

            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDown && currentEvent.type != EventType.MouseDrag)
                return;

            if (!rect.Contains(currentEvent.mousePosition))
                return;

            Vector2 mouseDirection = currentEvent.mousePosition - center;

            mouseDirection.x /= radius;
            mouseDirection.y /= -radius;

            if (mouseDirection.sqrMagnitude > 1f)
                mouseDirection.Normalize();

            direction.vector2Value = mouseDirection;

            serializedObject.ApplyModifiedProperties();

            currentEvent.Use();
            Repaint();
        }

        private void DrawTestControls()
        {
            CameraShakePreset preset = (CameraShakePreset)target;

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Passe en Play Mode pour tester le Camera Shake.", MessageType.Info);
            }
            else if (!CameraShakeManager.HasInstance)
            {
                EditorGUILayout.HelpBox("Aucun CameraShakeManager actif dans la scène.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!CameraShakeManager.HasInstance);

            if (GUILayout.Button("TEST SHAKE", GUILayout.Height(34f)))
                CameraShakeManager.PlayShake(preset);

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("STOP SHAKE", GUILayout.Height(34f)))
                CameraShakeManager.StopAllShakes();

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }
    }
}

#endif