using UnityEditor;
using UnityEngine;

namespace Boxey.Core.Editor {
    public class LineAttribute : PropertyAttribute{
        public readonly float Thickness;
        public Color Color;

        public LineAttribute(float thickness = 1.5f, float r = .5f, float g = .5f, float b = .5f){
            Thickness = thickness;
            Color = new Color(r, g, b);
        }
    }
    [CustomPropertyDrawer(typeof(LineAttribute))]
    public class LineDrawer : DecoratorDrawer{
        private LineAttribute LineAttribute => (LineAttribute)attribute;
        public override float GetHeight(){
            return LineAttribute.Thickness + EditorGUIUtility.standardVerticalSpacing;
        }
        public override void OnGUI(Rect position){
            position.y += EditorGUIUtility.standardVerticalSpacing * 0.5f;
        
            Color savedColor = GUI.color;
            GUI.color = LineAttribute.Color;

            GUIStyle lineStyle = new GUIStyle(GUI.skin.box);
            lineStyle.border = new RectOffset(0, 0, 0, 0);
            lineStyle.margin = new RectOffset(0, 0, 2, 2);
            lineStyle.padding = new RectOffset(0, 0, 0, 0);
            lineStyle.normal.background = EditorGUIUtility.whiteTexture;

            Rect lineRect = new Rect(position.x, position.y + (LineAttribute.Thickness * 0.5f), position.width, LineAttribute.Thickness);
            GUI.Box(lineRect, GUIContent.none, lineStyle);

            GUI.color = savedColor;
        }
    }
}