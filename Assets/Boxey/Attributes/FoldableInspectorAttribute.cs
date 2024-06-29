using UnityEngine;
using UnityEditor;

namespace Boxey.Attributes {
    public class FoldableInspectorAttribute : PropertyAttribute {
        public bool UnFolded;
        public FoldableInspectorAttribute(bool startUnFolded = false){
            UnFolded = startUnFolded;
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(FoldableInspectorAttribute))]
    public class FoldableInspectorDrawer : PropertyDrawer {
        private FoldableInspectorAttribute Attribute => (FoldableInspectorAttribute)attribute;
        private const float RightPadding = 15f; // Adjust padding as needed
        private const float BottomPadding = -20f; // Adjust bottom padding as needed
        private const float BoxPadding = 2f; // Padding inside the box

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            // Calculate the position for the foldout toggle including the box
            var foldoutBoxRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight + BoxPadding * 2);
            // Draw darker box around the foldout label and object field
            EditorGUI.DrawRect(foldoutBoxRect, new Color(0.1f, 0.1f, 0.1f, 0.4f));
            // Check if the object reference value is null
            if (property.objectReferenceValue == null) {
                var objectFieldRectNull = new Rect(position.x + 3f, position.y + BoxPadding, position.width - 10f, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(objectFieldRectNull, property, label);
                EditorGUI.EndProperty();
                return;
            }
            // Calculate the position for the foldout arrow
            var foldoutArrowRect = new Rect(position.x + 15f, position.y + BoxPadding, 13f, EditorGUIUtility.singleLineHeight);
            // Draw the foldout toggle manually to include the arrow inside the darker box
            Attribute.UnFolded = EditorGUI.Foldout(foldoutArrowRect, Attribute.UnFolded, GUIContent.none, true);
            // Draw the label manually so we can handle clicks on it
            var labelRect = new Rect(position.x + 17f, position.y + BoxPadding, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, property.displayName, EditorStyles.label);
            // Make the label toggle the foldout
            if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition)) {
                Attribute.UnFolded = !Attribute.UnFolded;
                Event.current.Use();
            }
            // Draw the object field
            var objectFieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y + BoxPadding, position.width - EditorGUIUtility.labelWidth - 5f, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(objectFieldRect, property, GUIContent.none);
            if (Attribute.UnFolded && property.objectReferenceValue != null) {
                // Draw a box around the foldable content
                var boxRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + BoxPadding * 2, position.width, GetFoldableContentHeight(property) + BottomPadding);
                EditorGUI.DrawRect(boxRect, new Color(0.8f, 0.8f, 0.8f, 0.2f));
                EditorGUI.LabelField(boxRect, GUIContent.none, GUI.skin.box);
                // Indent the content inside the box
                EditorGUI.indentLevel++;
                var serializedObject = new SerializedObject(property.objectReferenceValue);
                var prop = serializedObject.GetIterator();
                prop.NextVisible(true); // Skip generic field
                var yOffset = position.y + EditorGUIUtility.singleLineHeight + BoxPadding * 2 + EditorGUIUtility.standardVerticalSpacing - 7.5f;
                while (prop.NextVisible(false)) {
                    var height = EditorGUI.GetPropertyHeight(prop, true);
                    var fieldRect = new Rect(position.x + EditorGUI.indentLevel * 15f, yOffset, position.width - EditorGUI.indentLevel * 15f - RightPadding, height);
                    EditorGUI.PropertyField(fieldRect, prop, true);
                    yOffset += height + EditorGUIUtility.standardVerticalSpacing;
                }
                serializedObject.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (property.objectReferenceValue == null) {
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            var totalHeight = EditorGUIUtility.singleLineHeight + BoxPadding * 2;
            if (Attribute.UnFolded) {
                totalHeight += EditorGUIUtility.standardVerticalSpacing + GetFoldableContentHeight(property) + BottomPadding;
            }
            return totalHeight;
        }
        private float GetFoldableContentHeight(SerializedProperty property) {
            var height = EditorGUIUtility.singleLineHeight; // Start with foldout toggle height
            if (property.objectReferenceValue == null || !Attribute.UnFolded) return height;
            var serializedObject = new SerializedObject(property.objectReferenceValue);
            var prop = serializedObject.GetIterator();
            prop.NextVisible(true); // Skip generic field

            while (prop.NextVisible(false)) {
                height += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
#endif
}