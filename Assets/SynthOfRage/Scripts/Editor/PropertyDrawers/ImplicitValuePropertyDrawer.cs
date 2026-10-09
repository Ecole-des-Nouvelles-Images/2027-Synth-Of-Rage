using System;
using SynthOfRage.Scripts.Utilities.Attributes;
using UnityEditor;
using UnityEngine;

namespace SynthOfRage.Scripts.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ImplicitValueAttribute))]
    public class ImplicitValuePropertyDrawer : PropertyDrawer
    {
        private readonly Color _goldenColor = new (1f, 0.75f, 0.2f, 1f);
        private readonly Color _boxBackgroundColor = new(0.18f, 0.18f, 0.18f, 1f);
        private readonly Color _borderColor = new(1f, 0.75f, 0.2f, 0.85f);

        private const float BoxHeight = 20f;
        private const float SpacingAboveBox = 4f;
        private const float SpacingBelowBox = 6f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);

            if (attribute is ImplicitValueAttribute { ComparisonValue: not null } attr && ValuesEqual(property, attr.ComparisonValue))
            {
                return baseHeight + SpacingAboveBox + BoxHeight + SpacingBelowBox;
            }

            return baseHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ImplicitValueAttribute attr = attribute as ImplicitValueAttribute;
            bool isImplicit = attr is { ComparisonValue: not null } && ValuesEqual(property, attr.ComparisonValue);

            if (isImplicit)
            {
                float propHeight = EditorGUI.GetPropertyHeight(property, label, true);

                Rect fieldRect = new(
                    position.x,
                    position.y,
                    position.width,
                    propHeight
                );

                Rect boxRect = new(
                    position.x,
                    position.y + propHeight + SpacingAboveBox,
                    position.width,
                    BoxHeight
                );

                Color originalColor = GUI.contentColor;
                GUI.contentColor = _goldenColor;
                EditorGUI.PropertyField(fieldRect, property, label, true);
                GUI.contentColor = originalColor;

                EditorGUI.DrawRect(boxRect, _boxBackgroundColor);

                Handles.color = _borderColor;
                Handles.DrawPolyLine(
                    new Vector3(boxRect.xMin, boxRect.yMin),
                    new Vector3(boxRect.xMax, boxRect.yMin),
                    new Vector3(boxRect.xMax, boxRect.yMax),
                    new Vector3(boxRect.xMin, boxRect.yMax),
                    new Vector3(boxRect.xMin, boxRect.yMin)
                );

                GUIStyle bannerStyle = new (EditorStyles.label)
                {
                    richText = true,
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(6, 6, 0, 0)
                };

                string message = $"<color=#FFBF33>⚡ Implicit:</color> {attr.Tooltip}";
                GUI.Label(boxRect, message, bannerStyle);

                return;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        private static bool ValuesEqual(SerializedProperty property, object comparisonValue)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (comparisonValue is IConvertible intConv)
                        return property.intValue == Convert.ToInt32(intConv);
                    return false;

                case SerializedPropertyType.Float:
                    if (comparisonValue is IConvertible floatConv)
                        return Mathf.Approximately(property.floatValue, Convert.ToSingle(floatConv));
                    return false;

                case SerializedPropertyType.Boolean:
                    return comparisonValue is bool b && property.boolValue == b;

                case SerializedPropertyType.String:
                    return comparisonValue is string s && property.stringValue == s;

                case SerializedPropertyType.Color:
                    return comparisonValue is Color c && property.colorValue == c;

                case SerializedPropertyType.Vector2:
                    return comparisonValue is Vector2 v2 && property.vector2Value == v2;

                case SerializedPropertyType.Vector3:
                    return comparisonValue is Vector3 v3 && property.vector3Value == v3;

                case SerializedPropertyType.Vector4:
                    return comparisonValue is Vector4 v4 && property.vector4Value == v4;

                case SerializedPropertyType.Quaternion:
                    return comparisonValue is Quaternion q && property.quaternionValue == q;

                case SerializedPropertyType.Rect:
                    return comparisonValue is Rect r && property.rectValue == r;

                case SerializedPropertyType.ArraySize:
                    return comparisonValue is int arraySize && property.arraySize == arraySize;

                case SerializedPropertyType.Character:
                    return comparisonValue is char ch && property.intValue == ch;

                case SerializedPropertyType.AnimationCurve:
                    return comparisonValue is AnimationCurve curve && property.animationCurveValue != null && property.animationCurveValue.Equals(curve);

                case SerializedPropertyType.Bounds:
                    return comparisonValue is Bounds bounds && property.boundsValue == bounds;

                case SerializedPropertyType.ObjectReference:
                    return ReferenceEquals(property.objectReferenceValue, comparisonValue);

                case SerializedPropertyType.Enum:
                    return property.enumValueIndex == Convert.ToInt32(comparisonValue);

                default:
                    return false;
            }
        }
    }
}
