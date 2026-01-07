using UnityEditor;
using UnityEngine;

namespace Murtagh.Editor
{
    [CustomPropertyDrawer(typeof(ResizableTextAreaAttribute))]
    public class ResizableTextAreaPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float labelHeight = EditorGUIUtility.singleLineHeight;

            GUIContent textContent = new GUIContent(property.stringValue);
            GUIStyle textAreaStyle = EditorStyles.textArea;
            float textAreaHeight = textAreaStyle.CalcHeight(textContent, EditorGUIUtility.currentViewWidth);
            
            return labelHeight + textAreaHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [ResizableTextArea] with string.");
                return;
            }

            Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);

            Rect textAreaRect = new Rect(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight,
                position.width,
                position.height - EditorGUIUtility.singleLineHeight);

            property.stringValue = EditorGUI.TextArea(textAreaRect, property.stringValue);
        }
    }
}