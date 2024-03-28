using UnityEditor;
using UnityEngine;

namespace Boxey.Attributes {
    public class ToggleButtonsAttribute : PropertyAttribute {
        
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ToggleButtonsAttribute))]
    public class ToggleButtonsDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return base.GetPropertyHeight(property, label) - (EditorGUIUtility.singleLineHeight * 1.15f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.propertyType == SerializedPropertyType.Enum) {
                EditorGUI.BeginProperty(position, label, property);

                var enumNames = property.enumNames;
                var enumValue = property.enumValueIndex;

                EditorGUI.BeginChangeCheck();

                Rect buttonPosition = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);

                GUILayout.BeginHorizontal();

                for (var i = 0; i < enumNames.Length; i++) {
                    //if (GUILayout.Toggle(enumValue == i, enumNames[i] + " (" + ((i + 1) * (i + 1)) + "x)", "Button")) {
                    //    enumValue = i;
                    //}
                    if (GUILayout.Toggle(enumValue == i, enumNames[i], "Button")) {
                        enumValue = i;
                    }
                }

                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck()) {
                    property.enumValueIndex = enumValue;
                }

                EditorGUI.EndProperty();
            }else {
                EditorGUI.PropertyField(position, property, label);
            }
        }
        
    }
#endif
}