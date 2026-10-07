using SynthOfRage.Scripts.Utilities.Attributes;
using UnityEditor;
using UnityEngine;

namespace SynthOfRage.Scripts.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;

            if (attribute is ReadOnlyAttribute readOnlyAttribute && !string.IsNullOrEmpty(readOnlyAttribute.Tooltip))
            {
                label.tooltip = "[ReadOnly] " + readOnlyAttribute.Tooltip;
            }

            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = wasEnabled;
        }
    }
}
