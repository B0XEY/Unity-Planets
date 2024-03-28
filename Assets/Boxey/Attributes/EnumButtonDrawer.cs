using UnityEditor;
using UnityEngine;

namespace Boxey.Attributes {
    public class EnumButtonsAttribute : PropertyAttribute { }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(EnumButtonsAttribute))]
    public class EnumButtonDrawer : PropertyDrawer{
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            EditorGUI.BeginProperty(position, label, property);
            if (property.propertyType == SerializedPropertyType.Enum){
                EnumButtonsAttribute enumButtonsAttribute = (EnumButtonsAttribute)attribute;
                System.Enum enumValue = System.Enum.ToObject(fieldInfo.FieldType, property.enumValueIndex) as System.Enum;
                GUILayout.BeginHorizontal();
                foreach (System.Enum value in System.Enum.GetValues(fieldInfo.FieldType)){
                    bool isCurrentValue = enumValue.Equals(value);
                    GUI.enabled = !isCurrentValue;
                    if (GUILayout.Toggle(false, value.ToString(), "Button")){
                        property.enumValueIndex = System.Array.IndexOf(System.Enum.GetValues(fieldInfo.FieldType), value);
                    }
                }
                GUILayout.EndHorizontal();
            }else{
                EditorGUI.LabelField(position, "EnumButtons attribute error: Field is not an Enum");
            }

            EditorGUI.EndProperty();
        }
    }
#endif
}