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
            // Draw foldout with size field
            Rect headerRect = EditorGUILayout.GetControlRect();
            Rect foldoutRect = new Rect(headerRect.x, headerRect.y, headerRect.width - 60, headerRect.height);
            Rect sizeRect = new Rect(headerRect.xMax - 55, headerRect.y, 55, headerRect.height);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, property.displayName, true);

            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUI.DelayedIntField(sizeRect, property.arraySize);
            if (EditorGUI.EndChangeCheck())
            {
                property.arraySize = newSize;
            }

            if (!property.isExpanded)
                return;

            string key = property.serializedObject.targetObject.GetInstanceID() + "." + property.propertyPath;

            if (!_reorderableLists.TryGetValue(key, out ReorderableList list) || list.serializedProperty.serializedObject != property.serializedObject)
            {
                list = new ReorderableList(property.serializedObject, property, true, false, true, true);

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

        // Rect-based array drawing using ReorderableList.DoList(Rect)
        // Matches top-level structure: foldout+size outside, box only around elements
        private static void DrawArrayInRect(Rect rect, SerializedProperty property)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Draw foldout + size field OUTSIDE the box (like top-level)
            Rect foldoutRect = new Rect(rect.x, rect.y, rect.width - 60, lineHeight);
            Rect sizeRect = new Rect(rect.xMax - 55, rect.y, 55, lineHeight);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, property.displayName, true);

            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUI.DelayedIntField(sizeRect, property.arraySize);
            if (EditorGUI.EndChangeCheck())
            {
                property.arraySize = newSize;
            }

            if (!property.isExpanded)
                return;

            // Calculate rect for the list (below the foldout)
            float listY = rect.y + lineHeight + 2;
            float listHeight = rect.height - lineHeight - 2;
            Rect listRect = new Rect(rect.x, listY, rect.width, listHeight);

            string key = property.serializedObject.targetObject.GetInstanceID() + "." + property.propertyPath;

            if (!_reorderableLists.TryGetValue(key, out ReorderableList list) || list.serializedProperty.serializedObject != property.serializedObject)
            {
                // displayHeader = false - no header, just elements box
                list = new ReorderableList(property.serializedObject, property, true, false, true, true);

                list.drawElementCallback = (Rect elemRect, int index, bool isActive, bool isFocused) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    elemRect.x += 10;
                    elemRect.width -= 10;
                    elemRect.y += 2;

                    DrawElementInRect(elemRect, element, index);
                };

                list.elementHeightCallback = (int index) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    return GetElementHeight(element) + 4;
                };

                _reorderableLists[key] = list;
            }

            list.DoList(listRect);
        }

        private static float GetReorderableListHeight(SerializedProperty property)
        {
            // Foldout + size field (always visible, outside box)
            float height = EditorGUIUtility.singleLineHeight + 2;

            if (property.isExpanded)
            {
                // ReorderableList with no header: elements + footer
                height += 4; // top padding of list box

                for (int i = 0; i < property.arraySize; i++)
                {
                    var element = property.GetArrayElementAtIndex(i);
                    height += GetElementHeight(element) + 4;
                }

                height += EditorGUIUtility.singleLineHeight + 4; // footer with +/- buttons
            }

            return height;
        }

        private static void DrawChildrenInRect(Rect rect, SerializedProperty parentProperty)
        {
            var children = GetChildProperties(parentProperty);
            float yPos = rect.y;

            // Track which foldout groups we've already drawn
            HashSet<string> drawnFoldoutGroups = new HashSet<string>();

            foreach (var child in children)
            {
                var foldoutAttr = PropertyUtility.GetAttribute<FoldoutAttribute>(child);

                if (foldoutAttr == null)
                {
                    // Non-foldout property
                    if (!PropertyUtility.IsVisible(child))
                        continue;

                    bool isReadOnly = PropertyUtility.GetAttribute<ReadOnlyAttribute>(child) != null;
                    bool isEnabled = PropertyUtility.IsEnabled(child);

                    ValidatorAttribute[] validators = PropertyUtility.GetAttributes<ValidatorAttribute>(child);
                    foreach (var validator in validators)
                    {
                        validator.GetValidator()?.ValidateProperty(child);
                    }

                    using (new EditorGUI.DisabledScope(!isEnabled || isReadOnly))
                    {
                        // Nested array - use ReorderableList
                        if (child.isArray && child.propertyType == SerializedPropertyType.Generic)
                        {
                            float arrayHeight = GetReorderableListHeight(child);
                            Rect arrayRect = new Rect(rect.x, yPos, rect.width, arrayHeight);
                            DrawArrayInRect(arrayRect, child);
                            yPos += arrayHeight + 2;
                        }
                        // Nested class - recurse
                        else if (child.hasVisibleChildren && child.propertyType == SerializedPropertyType.Generic)
                        {
                            float nestedHeight = EditorGUIUtility.singleLineHeight + 2;
                            if (child.isExpanded)
                            {
                                nestedHeight += GetChildrenHeight(child);
                            }

                            Rect foldoutRect = new Rect(rect.x, yPos, rect.width, EditorGUIUtility.singleLineHeight);
                            child.isExpanded = EditorGUI.Foldout(foldoutRect, child.isExpanded, child.displayName, true);
                            yPos += EditorGUIUtility.singleLineHeight + 2;

                            if (child.isExpanded)
                            {
                                Rect childrenRect = new Rect(rect.x + 15, yPos, rect.width - 15, GetChildrenHeight(child));
                                DrawChildrenInRect(childrenRect, child);
                                yPos += GetChildrenHeight(child);
                            }
                        }
                        // Simple property
                        else
                        {
                            float propHeight = EditorGUI.GetPropertyHeight(child, true);
                            Rect propRect = new Rect(rect.x, yPos, rect.width, propHeight);
                            EditorGUI.PropertyField(propRect, child, true);
                            yPos += propHeight + 2;
                        }
                    }
                }
                else
                {
                    // Foldout property - draw group when first encountered
                    string groupName = foldoutAttr.Name;

                    if (!drawnFoldoutGroups.Contains(groupName))
                    {
                        drawnFoldoutGroups.Add(groupName);

                        var groupProps = children
                            .Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p)?.Name == groupName)
                            .Where(p => PropertyUtility.IsVisible(p))
                            .ToList();

                        if (!groupProps.Any())
                            continue;

                        string foldoutKey = $"{parentProperty.propertyPath}.{groupName}";
                        if (!_nestedFoldouts.ContainsKey(foldoutKey))
                        {
                            _nestedFoldouts[foldoutKey] = false;
                        }

                        Rect foldoutRect = new Rect(rect.x, yPos, rect.width, EditorGUIUtility.singleLineHeight);
                        _nestedFoldouts[foldoutKey] = EditorGUI.Foldout(foldoutRect, _nestedFoldouts[foldoutKey], groupName, true);
                        yPos += EditorGUIUtility.singleLineHeight + 2;

                        if (_nestedFoldouts[foldoutKey])
                        {
                            foreach (var prop in groupProps)
                            {
                                bool isReadOnly = PropertyUtility.GetAttribute<ReadOnlyAttribute>(prop) != null;
                                bool isEnabled = PropertyUtility.IsEnabled(prop);

                                ValidatorAttribute[] validators = PropertyUtility.GetAttributes<ValidatorAttribute>(prop);
                                foreach (var validator in validators)
                                {
                                    validator.GetValidator()?.ValidateProperty(prop);
                                }

                                using (new EditorGUI.DisabledScope(!isEnabled || isReadOnly))
                                {
                                    // Nested array inside foldout
                                    if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
                                    {
                                        float arrayHeight = GetReorderableListHeight(prop);
                                        Rect arrayRect = new Rect(rect.x + 15, yPos, rect.width - 15, arrayHeight);
                                        DrawArrayInRect(arrayRect, prop);
                                        yPos += arrayHeight + 2;
                                    }
                                    // Nested class inside foldout
                                    else if (prop.hasVisibleChildren && prop.propertyType == SerializedPropertyType.Generic)
                                    {
                                        Rect nestedFoldoutRect = new Rect(rect.x + 15, yPos, rect.width - 15, EditorGUIUtility.singleLineHeight);
                                        prop.isExpanded = EditorGUI.Foldout(nestedFoldoutRect, prop.isExpanded, prop.displayName, true);
                                        yPos += EditorGUIUtility.singleLineHeight + 2;

                                        if (prop.isExpanded)
                                        {
                                            Rect childrenRect = new Rect(rect.x + 30, yPos, rect.width - 30, GetChildrenHeight(prop));
                                            DrawChildrenInRect(childrenRect, prop);
                                            yPos += GetChildrenHeight(prop);
                                        }
                                    }
                                    // Simple property
                                    else
                                    {
                                        float propHeight = EditorGUI.GetPropertyHeight(prop, true);
                                        Rect propRect = new Rect(rect.x + 15, yPos, rect.width - 15, propHeight);
                                        EditorGUI.PropertyField(propRect, prop, true);
                                        yPos += propHeight + 2;
                                    }
                                }
                            }
                        }
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
            HashSet<string> countedFoldoutGroups = new HashSet<string>();

            foreach (var child in children)
            {
                var foldoutAttr = PropertyUtility.GetAttribute<FoldoutAttribute>(child);

                if (foldoutAttr == null)
                {
                    if (!PropertyUtility.IsVisible(child))
                        continue;

                    // Nested array
                    if (child.isArray && child.propertyType == SerializedPropertyType.Generic)
                    {
                        height += GetReorderableListHeight(child) + 2;
                    }
                    // Nested class
                    else if (child.hasVisibleChildren && child.propertyType == SerializedPropertyType.Generic)
                    {
                        height += EditorGUIUtility.singleLineHeight + 2; // foldout
                        if (child.isExpanded)
                        {
                            height += GetChildrenHeight(child);
                        }
                    }
                    // Simple property
                    else
                    {
                        height += EditorGUI.GetPropertyHeight(child, true) + 2;
                    }
                }
                else
                {
                    string groupName = foldoutAttr.Name;
                    if (!countedFoldoutGroups.Contains(groupName))
                    {
                        countedFoldoutGroups.Add(groupName);

                        var visibleProps = children
                            .Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p)?.Name == groupName)
                            .Where(p => PropertyUtility.IsVisible(p))
                            .ToList();

                        if (!visibleProps.Any())
                            continue;

                        height += EditorGUIUtility.singleLineHeight + 2; // foldout header

                        string foldoutKey = $"{parentProperty.propertyPath}.{groupName}";
                        if (_nestedFoldouts.TryGetValue(foldoutKey, out bool isExpanded) && isExpanded)
                        {
                            foreach (var prop in visibleProps)
                            {
                                // Nested array inside foldout
                                if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
                                {
                                    height += GetReorderableListHeight(prop) + 2;
                                }
                                // Nested class inside foldout
                                else if (prop.hasVisibleChildren && prop.propertyType == SerializedPropertyType.Generic)
                                {
                                    height += EditorGUIUtility.singleLineHeight + 2;
                                    if (prop.isExpanded)
                                    {
                                        height += GetChildrenHeight(prop);
                                    }
                                }
                                // Simple property
                                else
                                {
                                    height += EditorGUI.GetPropertyHeight(prop, true) + 2;
                                }
                            }
                        }
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
            var children = GetChildProperties(parentProperty);
            HashSet<string> drawnFoldoutGroups = new HashSet<string>();

            foreach (var child in children)
            {
                var foldoutAttr = PropertyUtility.GetAttribute<FoldoutAttribute>(child);

                if (foldoutAttr == null)
                {
                    // Non-foldout property
                    if (!PropertyUtility.IsVisible(child))
                        continue;

                    PropertyField_Layout(child, true);
                }
                else
                {
                    // Foldout property - draw group when first encountered
                    string groupName = foldoutAttr.Name;

                    if (!drawnFoldoutGroups.Contains(groupName))
                    {
                        drawnFoldoutGroups.Add(groupName);

                        var groupProps = children
                            .Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p)?.Name == groupName)
                            .Where(p => PropertyUtility.IsVisible(p))
                            .ToList();

                        if (!groupProps.Any())
                            continue;

                        string foldoutKey = $"{parentProperty.propertyPath}.{groupName}";
                        if (!_nestedFoldouts.ContainsKey(foldoutKey))
                        {
                            _nestedFoldouts[foldoutKey] = false;
                        }

                        _nestedFoldouts[foldoutKey] = EditorGUILayout.Foldout(_nestedFoldouts[foldoutKey], groupName, true);

                        if (_nestedFoldouts[foldoutKey])
                        {
                            EditorGUI.indentLevel++;
                            foreach (var prop in groupProps)
                            {
                                PropertyField_Layout(prop, true);
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }
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
