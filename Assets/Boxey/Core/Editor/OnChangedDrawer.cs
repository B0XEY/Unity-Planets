using System;
using UnityEditor;
using UnityEngine;

namespace Boxey.Core.Editor {
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class OnChangedAttribute : PropertyAttribute{
        public readonly string MethodName;
        public OnChangedAttribute(string methodName){
            MethodName = methodName;
        }
    }
    [CustomPropertyDrawer(typeof(OnChangedAttribute))]
    public class OnChangedDrawer : PropertyDrawer{
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label);
            if (EditorGUI.EndChangeCheck()){
                OnChangedAttribute onChanged = (OnChangedAttribute)attribute;
                MonoBehaviour target = property.serializedObject.targetObject as MonoBehaviour;
                if (target != null){
                    System.Reflection.MethodInfo method = target.GetType().GetMethod(onChanged.MethodName,
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (method != null){
                        method.Invoke(target, null);
                    }
                }
            }
        }
    }
}