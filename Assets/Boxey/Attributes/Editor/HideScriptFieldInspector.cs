using UnityEditor;
using UnityEngine;

namespace Boxey.Attributes.Editor {
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class HideScriptFieldInspector : UnityEditor.Editor {
        private const float OffsetAmount = 0f;

        public override void OnInspectorGUI() {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            var firstField = true;
            do {
                if (property.name == "m_Script") continue;
                if (firstField) {
                    GUILayout.Space(-OffsetAmount);
                    firstField = false;
                }

                EditorGUILayout.PropertyField(property, true);
            } while (property.NextVisible(false));

            serializedObject.ApplyModifiedProperties();
        }
    }
}