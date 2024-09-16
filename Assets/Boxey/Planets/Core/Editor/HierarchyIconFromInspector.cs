using UnityEditor;
using UnityEngine;

namespace Boxey.Planets.Core.Editor
{
    [InitializeOnLoad]
    public class HierarchyIconFromInspector : MonoBehaviour {
        private const float IconSize = 12.5f;

        static HierarchyIconFromInspector() {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        }

        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect) {
            var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) {
                return;
            }
            var icon = EditorGUIUtility.GetIconForObject(obj);
            if (icon == null) {
                return;
            }

            // Calculate the position to draw the icon, so it's centered in the row.
            var rect = new Rect(
                selectionRect.x + selectionRect.width - IconSize - 4,
                selectionRect.y + (selectionRect.height - IconSize) / 2,
                IconSize, IconSize
            );
            GUI.DrawTexture(rect, icon);
        }
    }
}