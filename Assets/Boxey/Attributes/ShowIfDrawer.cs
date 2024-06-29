﻿using System;
using UnityEditor;
using UnityEngine;

namespace Boxey.Attributes {
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class ShowIfAttribute : PropertyAttribute{
        public readonly string BoolName;
        public ShowIfAttribute(string boolName){
            BoolName = boolName;
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer {
        private ShowIfAttribute ShowIfAttribute => (ShowIfAttribute)attribute;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            var boolProperty = property.serializedObject.FindProperty(ShowIfAttribute.BoolName);
            if (boolProperty != null && boolProperty.propertyType == SerializedPropertyType.Boolean){
                EditorGUI.BeginProperty(position, label, property);
                var showField = boolProperty.boolValue;
                if (showField){
                    EditorGUI.PropertyField(position, property, label);
                }
                EditorGUI.EndProperty();
            }else{
                EditorGUI.LabelField(position, "ShowIf attribute error: Boolean field not found");
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
            var boolProperty = property.serializedObject.FindProperty(ShowIfAttribute.BoolName);
            if (boolProperty != null && boolProperty.propertyType == SerializedPropertyType.Boolean){
                return boolProperty.boolValue ? EditorGUI.GetPropertyHeight(property, label) : 0f;
            }else{
                return EditorGUIUtility.singleLineHeight;
            }
        }
    }
#endif
}