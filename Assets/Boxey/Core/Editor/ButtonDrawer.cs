using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Boxey.Core.Editor {
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ButtonAttribute : PropertyAttribute{
        public readonly string CustomLabel;
        public ButtonAttribute(string customLabel = ""){
            CustomLabel = customLabel;
        }
    }
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonDrawer : UnityEditor.Editor {
        public override void OnInspectorGUI(){
            DrawDefaultInspector();
            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (MethodInfo method in methods){
                ButtonAttribute[] buttonAttributes = (ButtonAttribute[])method.GetCustomAttributes(typeof(ButtonAttribute), true);
                if (buttonAttributes.Length > 0){
                    string label = string.IsNullOrEmpty(buttonAttributes[0].CustomLabel) ? ObjectNames.NicifyVariableName(method.Name) : buttonAttributes[0].CustomLabel;
                    if (GUILayout.Button(label)){
                        method.Invoke(target, null);
                    }
                }
            }
        }
    }
}