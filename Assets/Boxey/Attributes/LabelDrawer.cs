using UnityEditor;
using UnityEngine;

namespace Boxey.Attributes {
    public class LabelAttribute : PropertyAttribute{
        public readonly string CustomLabel;
        public LabelAttribute(string customLabel = ""){
            CustomLabel = customLabel;
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelDrawer : PropertyDrawer{
        private LabelAttribute LabelAttribute => (LabelAttribute)attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            EditorGUI.BeginProperty(position, label, property);

            var customLabelContent = new GUIContent(LabelAttribute.CustomLabel);
            EditorGUI.PropertyField(position, property, customLabelContent);

            EditorGUI.EndProperty();
        }
    }
#endif
}