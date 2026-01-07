using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

namespace Murtagh.Editor
{
    public static class MurtaghEditorGUI
    {
        private static Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();

        public static void PropertyField_Layout(SerializedProperty property, bool includeChildren)
        {
            // Check if visible
            if (!PropertyUtility.IsVisible(property))
                return;
            
            bool isReadOnly = PropertyUtility.GetAttribute<ReadOnlyAttribute>(property) != null;
            bool isEnabled = PropertyUtility.IsEnabled(property);
            using (new EditorGUI.DisabledScope(!isEnabled || isReadOnly))
            {
                // Arrays need special handling for nested visibility
                if (includeChildren && property.isArray && property.propertyType == SerializedPropertyType.Generic)
                {
                    DrawArrayWithReorderableList(property);
                    return;
                }

                // Generic types (nested classes) need child visibility checks
                if (includeChildren && property.hasVisibleChildren && property.propertyType == SerializedPropertyType.Generic)
                {
                    DrawNestedClass(property);
                    return;
                }

                // Default Unity drawer for everything else
                EditorGUILayout.PropertyField(property, new GUIContent(property.displayName), includeChildren);
            }
        }

        private static void DrawArrayWithReorderableList(SerializedProperty property)
        {
            string key = property.serializedObject.targetObject.GetInstanceID() + "." + property.propertyPath;

            if (!_reorderableLists.TryGetValue(key, out ReorderableList list) || list.serializedProperty.serializedObject != property.serializedObject)
            {
                list = new ReorderableList(property.serializedObject, property, true, true, true, true);

                list.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, property.displayName);
                };

                list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = property.GetArrayElementAtIndex(index);

                    // Leave space for drag handle
                    rect.x += 10;
                    rect.width -= 10;
                    rect.y += 2;

                    if (element.hasVisibleChildren && element.propertyType == SerializedPropertyType.Generic)
                    {
                        // Draw nested class with visibility checks
                        DrawNestedClassInRect(rect, element, index);
                    }
                    else
                    {
                        rect.height = EditorGUIUtility.singleLineHeight;
                        EditorGUI.PropertyField(rect, element, GUIContent.none);
                    }
                };

                list.elementHeightCallback = (int index) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    if (element.hasVisibleChildren && element.propertyType == SerializedPropertyType.Generic)
                    {
                        return GetNestedClassHeight(element);
                    }
                    return EditorGUIUtility.singleLineHeight + 4;
                };

                _reorderableLists[key] = list;
            }

            list.DoLayoutList();
        }
        
        public static float GetIndentLength(Rect sourceRect)
        {
            Rect indentRect = EditorGUI.IndentedRect(sourceRect);
            float indentLength = indentRect.x - sourceRect.x;

            return indentLength;
        }

        private static void DrawNestedClass(SerializedProperty property)
        {
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName, true);

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;
            DrawChildren(property);
            EditorGUI.indentLevel--;
        }

        private static void DrawNestedClassInRect(Rect rect, SerializedProperty property, int index)
        {
            Rect foldoutRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"Element {index}", true);

            if (!property.isExpanded)
                return;

            float yOffset = EditorGUIUtility.singleLineHeight + 2;
            var iterator = property.Copy();
            var endProperty = property.GetEndProperty();

            if (!iterator.NextVisible(true))
                return;

            do
            {
                if (SerializedProperty.EqualContents(iterator, endProperty))
                    break;

                if (!PropertyUtility.IsVisible(iterator))
                    continue;

                Rect propRect = new Rect(rect.x, rect.y + yOffset, rect.width, EditorGUI.GetPropertyHeight(iterator, true));
                
                bool isReadOnly = PropertyUtility.GetAttribute<ReadOnlyAttribute>(iterator) != null;
                bool isEnabled = PropertyUtility.IsEnabled(iterator);
                using (new EditorGUI.DisabledScope(!isEnabled || isReadOnly))
                {
                    EditorGUI.PropertyField(propRect, iterator, new GUIContent(iterator.displayName), true);
                }
                
                yOffset += propRect.height + 2;

            } while (iterator.NextVisible(false));
        }

        private static float GetNestedClassHeight(SerializedProperty property)
        {
            float height = EditorGUIUtility.singleLineHeight + 4; // Foldout

            if (!property.isExpanded)
                return height;

            var iterator = property.Copy();
            var endProperty = property.GetEndProperty();

            if (!iterator.NextVisible(true))
                return height;

            do
            {
                if (SerializedProperty.EqualContents(iterator, endProperty))
                    break;

                if (!PropertyUtility.IsVisible(iterator))
                    continue;

                height += EditorGUI.GetPropertyHeight(iterator, true) + 2;

            } while (iterator.NextVisible(false));

            return height;
        }

        private static void DrawChildren(SerializedProperty parentProperty)
        {
            var iterator = parentProperty.Copy();
            var endProperty = parentProperty.GetEndProperty();

            if (!iterator.NextVisible(true))
                return;

            do
            {
                if (SerializedProperty.EqualContents(iterator, endProperty))
                    break;

                if (!PropertyUtility.IsVisible(iterator))
                    continue;

                // Recursive call for nested support
                PropertyField_Layout(iterator.Copy(), true);

            } while (iterator.NextVisible(false));
        }

        public static void BeginBoxGroup_Layout(string label = "")
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            }
        }
        
        public static void HelpBox(Rect rect, string message, MessageType type, UnityEngine.Object context = null, bool logToConsole = false)
        {
            EditorGUI.HelpBox(rect, message, type);

            if (logToConsole)
            {
                DebugLogMessage(message, type, context);
            }
        }

        public static void HelpBox_Layout(string message, MessageType type, UnityEngine.Object context = null, bool logToConsole = false)
        {
            EditorGUILayout.HelpBox(message, type);

            if (logToConsole)
            {
                DebugLogMessage(message, type, context);
            }
        }

        public static void EndBoxGroup_Layout()
        {
            EditorGUILayout.EndVertical();
        }

        public static void ClearCache()
        {
            _reorderableLists.Clear();
        }
        
        private static void DebugLogMessage(string message, MessageType type, UnityEngine.Object context)
        {
            switch (type)
            {
                case MessageType.None:
                case MessageType.Info:
                    Debug.Log(message, context);
                    break;
                case MessageType.Warning:
                    Debug.LogWarning(message, context);
                    break;
                case MessageType.Error:
                    Debug.LogError(message, context);
                    break;
            }
        }
    }
}
