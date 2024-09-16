using UnityEditor;
using UnityEngine;

namespace Boxey.Attributes {
    public class EnumButtonsAttribute : PropertyAttribute { }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(EnumButtonsAttribute))]
    public class EnumButtonDrawer : PropertyDrawer {
        private static readonly Color SelectedColor = new Color(0.8f, 0.8f, 0.8f);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.propertyType != SerializedPropertyType.Enum) {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            var enumNames = property.enumDisplayNames;
            var currentValue = property.enumValueIndex;
            var buttonCount = enumNames.Length;
            var buttonWidth = position.width / buttonCount;
            
            for (var i = 0; i < buttonCount; i++) {
                var buttonRect = new Rect(position.x + i * buttonWidth, position.y, buttonWidth, position.height);
                var originalColor = GUI.backgroundColor;
                if (i == currentValue) {
                    GUI.backgroundColor = SelectedColor;
                }
                if (GUI.Button(buttonRect, enumNames[i], EditorStyles.miniButton)) {
                    property.enumValueIndex = i;
                }

                GUI.backgroundColor = originalColor;
            }
        }
    }
#endif
}