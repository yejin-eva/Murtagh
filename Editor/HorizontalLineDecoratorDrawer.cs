using UnityEditor;
using UnityEngine;

namespace Murtagh.Editor
{
    [CustomPropertyDrawer(typeof(HorizontalLineAttribute))]
    public class HorizontalLineDecoratorDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            HorizontalLineAttribute lineAttribute = (HorizontalLineAttribute)attribute;
            return EditorGUIUtility.singleLineHeight + lineAttribute.Height;
        }

        public override void OnGUI(Rect position)
        {
            Rect rect = EditorGUI.IndentedRect(position);
            rect.y += EditorGUIUtility.singleLineHeight / 3.0f;
            HorizontalLineAttribute lineAttribute = (HorizontalLineAttribute)attribute;
            MurtaghEditorGUI.HorizontalLine(rect, lineAttribute.Height, lineAttribute.Color.GetColor());
        }
    }
}