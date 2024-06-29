using UnityEditor;
using UnityEngine;

namespace Boxey.Attributes {
    public class RequiredAttribute : PropertyAttribute { }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public class RequiredDrawer : PropertyDrawer{
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label);
            if (property.objectReferenceValue == null){
                var warningStyle = new GUIStyle(EditorStyles.helpBox) {
                    normal = {
                        textColor = Color.red
                    }
                };
                var warningContent = new GUIContent("Field is required!");
                var warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(warningRect, warningContent, warningStyle);
                if (Event.current.type == EventType.Layout){
                    property.serializedObject.ApplyModifiedProperties(); // This will prevent the null value from being applied.
                }
            }
            EditorGUI.EndProperty();
        }
    }
#endif
}