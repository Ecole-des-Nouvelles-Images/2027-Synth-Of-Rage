#if UNITY_EDITOR

using UnityEditor;

namespace SynthOfRage.Scripts.CameraSystem.Editor
{
    [CustomEditor(typeof(CameraShakeTrigger))]
    public class CameraShakeTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty preset;
        private SerializedProperty targetTag;
        private SerializedProperty playOnce;
        private SerializedProperty directionMode;
        private SerializedProperty customDirection;
        private SerializedProperty directionOrigin;

        private void OnEnable()
        {
            preset = serializedObject.FindProperty("preset");
            targetTag = serializedObject.FindProperty("targetTag");
            playOnce = serializedObject.FindProperty("playOnce");
            directionMode = serializedObject.FindProperty("directionMode");
            customDirection = serializedObject.FindProperty("customDirection");
            directionOrigin = serializedObject.FindProperty("directionOrigin");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Shake", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(preset);

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel);
            targetTag.stringValue = EditorGUILayout.TagField("Target Tag", targetTag.stringValue);
            EditorGUILayout.PropertyField(playOnce);

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("Direction", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(directionMode);

            CameraShakeTrigger.DirectionMode mode = (CameraShakeTrigger.DirectionMode)directionMode.enumValueIndex;

            switch (mode)
            {
                case CameraShakeTrigger.DirectionMode.Custom:
                    EditorGUILayout.PropertyField(customDirection);
                    break;

                case CameraShakeTrigger.DirectionMode.TowardTarget:
                case CameraShakeTrigger.DirectionMode.TowardSource:
                    EditorGUILayout.PropertyField(directionOrigin);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif