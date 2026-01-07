using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Murtagh.Editor
{
    public static class MurtaghEditorGUI
    {
        private static Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();
        private static Dictionary<string, bool> _nestedFoldouts = new();
        
        public static void PropertyField_Layout(SerializedProperty property, bool includeChildren)
        {
            // Check if visible
            if (!PropertyUtility.IsVisible(property))
                return;
           
            // validate
            ValidatorAttribute[] validators = PropertyUtility.GetAttributes<ValidatorAttribute>(property);
            foreach (var validator in validators)
            {
                validator.GetValidator()?.ValidateProperty(property);
            }
            
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

                    DrawElementInRect(rect, element, index);
                };

                list.elementHeightCallback = (int index) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    return GetElementHeight(element) + 4;
                };

                _reorderableLists[key] = list;
            }

            list.DoLayoutList();
        }

        private static void DrawElementInRect(Rect rect, SerializedProperty element, int index)
        {
            if (element.hasVisibleChildren && element.propertyType == SerializedPropertyType.Generic)
            {
                // Nested class element: draw foldout header then children
                Rect foldoutRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

                string foldoutKey = element.propertyPath;
                if (!_nestedFoldouts.ContainsKey(foldoutKey))
                {
                    _nestedFoldouts[foldoutKey] = false;
                }

                _nestedFoldouts[foldoutKey] = EditorGUI.Foldout(foldoutRect, _nestedFoldouts[foldoutKey], $"Element {index}", true);
                element.isExpanded = _nestedFoldouts[foldoutKey];

                if (element.isExpanded)
                {
                    float yOffset = EditorGUIUtility.singleLineHeight + 2;
                    Rect childrenRect = new Rect(rect.x + 15, rect.y + yOffset, rect.width - 15, rect.height - yOffset);
                    DrawChildrenInRect(childrenRect, element);
                }
            }
            else
            {
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            }
        }

        private static void DrawChildrenInRect(Rect rect, SerializedProperty parentProperty)
        {
            var children = GetChildProperties(parentProperty);
            float yPos = rect.y;

            // Draw non-foldout properties first
            foreach (var child in children.Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p) == null))
            {
                if (!PropertyUtility.IsVisible(child))
                    continue;

                bool isReadOnly = PropertyUtility.GetAttribute<ReadOnlyAttribute>(child) != null;
                bool isEnabled = PropertyUtility.IsEnabled(child);

                // Run validators
                ValidatorAttribute[] validators = PropertyUtility.GetAttributes<ValidatorAttribute>(child);
                foreach (var validator in validators)
                {
                    validator.GetValidator()?.ValidateProperty(child);
                }

                float propHeight = EditorGUI.GetPropertyHeight(child, true);
                Rect propRect = new Rect(rect.x, yPos, rect.width, propHeight);

                using (new EditorGUI.DisabledScope(!isEnabled || isReadOnly))
                {
                    EditorGUI.PropertyField(propRect, child, true);
                }

                yPos += propHeight + 2;
            }

            // Draw foldout groups
            var foldoutGroups = children
                .Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p) != null)
                .GroupBy(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p).Name);

            foreach (var group in foldoutGroups)
            {
                var visibleProps = group.Where(p => PropertyUtility.IsVisible(p)).ToList();
                if (!visibleProps.Any())
                    continue;

                string foldoutKey = $"{parentProperty.propertyPath}.{group.Key}";
                if (!_nestedFoldouts.ContainsKey(foldoutKey))
                {
                    _nestedFoldouts[foldoutKey] = false;
                }

                Rect foldoutRect = new Rect(rect.x, yPos, rect.width, EditorGUIUtility.singleLineHeight);
                _nestedFoldouts[foldoutKey] = EditorGUI.Foldout(foldoutRect, _nestedFoldouts[foldoutKey], group.Key, true);
                yPos += EditorGUIUtility.singleLineHeight + 2;

                if (_nestedFoldouts[foldoutKey])
                {
                    foreach (var prop in visibleProps)
                    {
                        bool isReadOnly = PropertyUtility.GetAttribute<ReadOnlyAttribute>(prop) != null;
                        bool isEnabled = PropertyUtility.IsEnabled(prop);

                        ValidatorAttribute[] validators = PropertyUtility.GetAttributes<ValidatorAttribute>(prop);
                        foreach (var validator in validators)
                        {
                            validator.GetValidator()?.ValidateProperty(prop);
                        }

                        float propHeight = EditorGUI.GetPropertyHeight(prop, true);
                        Rect propRect = new Rect(rect.x + 15, yPos, rect.width - 15, propHeight);

                        using (new EditorGUI.DisabledScope(!isEnabled || isReadOnly))
                        {
                            EditorGUI.PropertyField(propRect, prop, true);
                        }

                        yPos += propHeight + 2;
                    }
                }
            }
        }

        private static float GetElementHeight(SerializedProperty element)
        {
            if (element.hasVisibleChildren && element.propertyType == SerializedPropertyType.Generic)
            {
                float height = EditorGUIUtility.singleLineHeight; // foldout header

                if (element.isExpanded)
                {
                    height += GetChildrenHeight(element);
                }

                return height;
            }
            
            return EditorGUIUtility.singleLineHeight;
        }

        private static float GetChildrenHeight(SerializedProperty parentProperty)
        {
            float height = 0f;

            var children = GetChildProperties(parentProperty);
            
            // non-foldout properties
            foreach (var child in children.Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p) == null))
            {
                if (!PropertyUtility.IsVisible(child))
                {
                    continue;
                }
                height += EditorGUI.GetPropertyHeight(child, true) + 2;
            }
            
            // foldout groups
            var foldoutGroups = children.Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p) != null)
                .GroupBy(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p).Name);

            foreach (var group in foldoutGroups)
            {
                var visibleProps = group.Where(p => PropertyUtility.IsVisible(p)).ToList();
                if (!visibleProps.Any())
                {
                    continue;
                }

                height += EditorGUIUtility.singleLineHeight + 2; // foldout header
                
                string foldoutKey = $"{parentProperty.propertyPath}.{group.Key}";
                if (_nestedFoldouts.TryGetValue(foldoutKey, out bool isExpanded) && isExpanded)
                {
                    foreach (var prop in visibleProps)
                    {
                        height += EditorGUI.GetPropertyHeight(prop, true) + 2;
                    }
                }
            }

            return height;
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

        private static List<SerializedProperty> GetChildProperties(SerializedProperty parentProperty)
        {
            var children = new List<SerializedProperty>();
            var iterator = parentProperty.Copy();
            var endProperty = parentProperty.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        break;
                    }
                    children.Add(iterator.Copy());
                } while (iterator.NextVisible(false));
            }

            return children;
        }
        
        
        public static float GetIndentLength(Rect sourceRect)
        {
            Rect indentRect = EditorGUI.IndentedRect(sourceRect);
            float indentLength = indentRect.x - sourceRect.x;

            return indentLength;
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
        
        public static void EndBoxGroup_Layout()
        {
            EditorGUILayout.EndVertical();
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

        public static void ClearCache()
        {
            _reorderableLists.Clear();
            _nestedFoldouts.Clear();
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
