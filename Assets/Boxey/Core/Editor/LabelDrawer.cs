using UnityEditor;
using UnityEngine;

namespace Boxey.Core.Editor {
    public class LabelAttribute : PropertyAttribute{
        public readonly string CustomLabel;
        public LabelAttribute(string customLabel = ""){
            CustomLabel = customLabel;
        }
    }
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelDrawer : PropertyDrawer{
        private LabelAttribute LabelAttribute => (LabelAttribute)attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            EditorGUI.BeginProperty(position, label, property);

            GUIContent customLabelContent = new GUIContent(LabelAttribute.CustomLabel);
            EditorGUI.PropertyField(position, property, customLabelContent);

            EditorGUI.EndProperty();
        }
    }
}