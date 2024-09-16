using System;
using UnityEditor;
using UnityEngine;

namespace Boxey.Attributes {
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class OnChangedAttribute : PropertyAttribute{
        public readonly string MethodName;
        public OnChangedAttribute(string methodName){
            MethodName = methodName;
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(OnChangedAttribute))]
    public class OnChangedDrawer : PropertyDrawer{
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label);
            if (!EditorGUI.EndChangeCheck()) {
                return;
            }
            var onChanged = (OnChangedAttribute)attribute;
            var target = property.serializedObject.targetObject as MonoBehaviour;
            if (target == null) {
                return;
            }
            var method = target.GetType().GetMethod(onChanged.MethodName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (method != null){
                method.Invoke(target, null);
            }
        }
    }
#endif
}