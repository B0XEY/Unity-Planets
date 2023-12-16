using UnityEditor;
using UnityEngine;

namespace Boxey.Core.Editor {
    public class RequiredAttribute : PropertyAttribute { }
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public class RequiredDrawer : PropertyDrawer{
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label);
            if (property.objectReferenceValue == null){
                GUIStyle warningStyle = new GUIStyle(EditorStyles.helpBox);
                warningStyle.normal.textColor = Color.red;
                GUIContent warningContent = new GUIContent("Field is required!");
                Rect warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(warningRect, warningContent, warningStyle);
                if (Event.current.type == EventType.Layout){
                    property.serializedObject.ApplyModifiedProperties(); // This will prevent the null value from being applied.
                }
            }
            EditorGUI.EndProperty();
        }
    }
}