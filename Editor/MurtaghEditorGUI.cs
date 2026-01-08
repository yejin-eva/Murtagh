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

        // ============== ATTRIBUTE CACHE ==============
        // Caches reflection results to avoid repeated lookups every frame
        private static Dictionary<string, FoldoutAttribute> _foldoutAttrCache = new();
        private static Dictionary<string, ReadOnlyAttribute> _readOnlyAttrCache = new();
        private static Dictionary<string, ValidatorAttribute[]> _validatorAttrCache = new();
        private static HashSet<string> _attrCacheChecked = new(); // Track properties we've already looked up

        private static FoldoutAttribute GetCachedFoldoutAttribute(SerializedProperty property)
        {
            string key = GetPropertyTypeKey(property);

            if (_foldoutAttrCache.TryGetValue(key, out var cached))
                return cached;

            if (_attrCacheChecked.Contains(key))
                return null; // Already checked, no attribute

            _attrCacheChecked.Add(key);
            var attr = PropertyUtility.GetAttribute<FoldoutAttribute>(property);
            if (attr != null)
                _foldoutAttrCache[key] = attr;

            return attr;
        }

        private static bool GetCachedIsReadOnly(SerializedProperty property)
        {
            string key = GetPropertyTypeKey(property);

            if (_readOnlyAttrCache.ContainsKey(key))
                return true;

            string checkedKey = key + "_ro";
            if (_attrCacheChecked.Contains(checkedKey))
                return false;

            _attrCacheChecked.Add(checkedKey);
            var attr = PropertyUtility.GetAttribute<ReadOnlyAttribute>(property);
            if (attr != null)
            {
                _readOnlyAttrCache[key] = attr;
                return true;
            }
            return false;
        }

        private static ValidatorAttribute[] GetCachedValidators(SerializedProperty property)
        {
            string key = GetPropertyTypeKey(property);

            if (_validatorAttrCache.TryGetValue(key, out var cached))
                return cached;

            var attrs = PropertyUtility.GetAttributes<ValidatorAttribute>(property);
            _validatorAttrCache[key] = attrs;
            return attrs;
        }

        // Key based on declaring type + property path for stable caching
        private static string GetPropertyTypeKey(SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            return $"{targetType.FullName}.{property.propertyPath}";
        }

        /// <summary>
        /// Pre-processes children into foldout groups to avoid repeated LINQ queries.
        /// Returns a tuple of (non-grouped properties, dictionary of group name -> properties).
        /// </summary>
        private static (List<SerializedProperty> nonGrouped, Dictionary<string, List<SerializedProperty>> groups, List<string> groupOrder)
            GetChildrenGrouped(List<SerializedProperty> children)
        {
            var nonGrouped = new List<SerializedProperty>();
            var groups = new Dictionary<string, List<SerializedProperty>>();
            var groupOrder = new List<string>(); // Preserve order of first appearance

            foreach (var child in children)
            {
                var foldoutAttr = GetCachedFoldoutAttribute(child);
                if (foldoutAttr == null)
                {
                    nonGrouped.Add(child);
                }
                else
                {
                    string groupName = foldoutAttr.Name;
                    if (!groups.TryGetValue(groupName, out var list))
                    {
                        list = new List<SerializedProperty>();
                        groups[groupName] = list;
                        groupOrder.Add(groupName);
                    }
                    list.Add(child);
                }
            }

            return (nonGrouped, groups, groupOrder);
        }
        // ============== END ATTRIBUTE CACHE ==============

        public static void PropertyField_Layout(SerializedProperty property, bool includeChildren)
        {
            // Check if visible
            if (!PropertyUtility.IsVisible(property))
                return;

            // validate
            ValidatorAttribute[] validators = GetCachedValidators(property);
            foreach (var validator in validators)
            {
                validator.GetValidator()?.ValidateProperty(property);
            }

            bool isReadOnly = GetCachedIsReadOnly(property);
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
                // ReorderableList calculates its height as:
                // - headerHeight (0 since displayHeader=false, but still has some padding)
                // - element heights
                // - footerHeight

                // Get the actual list to calculate precise height
                string key = property.serializedObject.targetObject.GetInstanceID() + "." + property.propertyPath;
                if (_reorderableLists.TryGetValue(key, out ReorderableList list))
                {
                    height += list.GetHeight();
                }
                else
                {
                    // Estimate if list not created yet
                    height += 7; // top padding

                    if (property.arraySize == 0)
                    {
                        height += EditorGUIUtility.singleLineHeight + 2; // empty list placeholder
                    }
                    else
                    {
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            var element = property.GetArrayElementAtIndex(i);
                            height += GetElementHeight(element) + 4;
                        }
                    }

                    height += EditorGUIUtility.singleLineHeight + 4; // footer
                }
            }

            return height;
        }

        private static void DrawChildrenInRect(Rect rect, SerializedProperty parentProperty)
        {
            var children = GetChildProperties(parentProperty);
            var (nonGrouped, groups, groupOrder) = GetChildrenGrouped(children);
            float yPos = rect.y;
            HashSet<string> drawnFoldoutGroups = new HashSet<string>();

            foreach (var child in children)
            {
                var foldoutAttr = GetCachedFoldoutAttribute(child);

                if (foldoutAttr == null)
                {
                    // Non-foldout property
                    if (!PropertyUtility.IsVisible(child))
                        continue;

                    yPos += DrawPropertyWithAttributes(rect, child, yPos, 0);
                }
                else
                {
                    // Foldout group - draw when first encountered
                    string groupName = foldoutAttr.Name;
                    if (drawnFoldoutGroups.Contains(groupName))
                        continue;

                    drawnFoldoutGroups.Add(groupName);

                    // Use pre-computed group, filter only by visibility
                    var groupProps = groups[groupName].Where(p => PropertyUtility.IsVisible(p)).ToList();

                    if (groupProps.Count == 0)
                        continue;

                    // Draw foldout header
                    string foldoutKey = $"{parentProperty.propertyPath}.{groupName}";
                    if (!_nestedFoldouts.ContainsKey(foldoutKey))
                        _nestedFoldouts[foldoutKey] = false;

                    Rect foldoutRect = new Rect(rect.x, yPos, rect.width, EditorGUIUtility.singleLineHeight);
                    _nestedFoldouts[foldoutKey] = EditorGUI.Foldout(foldoutRect, _nestedFoldouts[foldoutKey], groupName, true);
                    yPos += EditorGUIUtility.singleLineHeight + 2;

                    // Draw foldout contents
                    if (_nestedFoldouts[foldoutKey])
                    {
                        foreach (var prop in groupProps)
                        {
                            yPos += DrawPropertyWithAttributes(rect, prop, yPos, 15);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws a property with all attribute handling (visibility, readonly, validators).
        /// Returns the height consumed.
        /// </summary>
        private static float DrawPropertyWithAttributes(Rect rect, SerializedProperty prop, float yPos, float indent)
        {
            bool isReadOnly = GetCachedIsReadOnly(prop);
            bool isEnabled = PropertyUtility.IsEnabled(prop);

            // Run validators
            ValidatorAttribute[] validators = GetCachedValidators(prop);
            foreach (var validator in validators)
            {
                validator.GetValidator()?.ValidateProperty(prop);
            }

            using (new EditorGUI.DisabledScope(!isEnabled || isReadOnly))
            {
                return DrawPropertyInRect(rect, prop, yPos, indent);
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
            var (nonGrouped, groups, groupOrder) = GetChildrenGrouped(children);
            HashSet<string> countedFoldoutGroups = new HashSet<string>();

            foreach (var child in children)
            {
                var foldoutAttr = GetCachedFoldoutAttribute(child);

                if (foldoutAttr == null)
                {
                    if (!PropertyUtility.IsVisible(child))
                        continue;

                    height += GetPropertyHeightInRect(child);
                }
                else
                {
                    string groupName = foldoutAttr.Name;
                    if (countedFoldoutGroups.Contains(groupName))
                        continue;

                    countedFoldoutGroups.Add(groupName);

                    // Use pre-computed group, filter only by visibility
                    var visibleProps = groups[groupName].Where(p => PropertyUtility.IsVisible(p)).ToList();

                    if (visibleProps.Count == 0)
                        continue;

                    height += EditorGUIUtility.singleLineHeight + 2; // foldout header

                    string foldoutKey = $"{parentProperty.propertyPath}.{groupName}";
                    if (_nestedFoldouts.TryGetValue(foldoutKey, out bool isExpanded) && isExpanded)
                    {
                        foreach (var prop in visibleProps)
                        {
                            height += GetPropertyHeightInRect(prop);
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
            var (nonGrouped, groups, groupOrder) = GetChildrenGrouped(children);
            HashSet<string> drawnFoldoutGroups = new HashSet<string>();

            foreach (var child in children)
            {
                var foldoutAttr = GetCachedFoldoutAttribute(child);

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

                        // Use pre-computed group, filter only by visibility
                        var groupProps = groups[groupName].Where(p => PropertyUtility.IsVisible(p)).ToList();

                        if (groupProps.Count == 0)
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
        
        // ============== HELPER METHODS FOR PROPERTY DRAWING/HEIGHT ==============

        private static bool IsArray(SerializedProperty prop) =>
            prop.isArray && prop.propertyType == SerializedPropertyType.Generic;

        private static bool IsNestedClass(SerializedProperty prop) =>
            prop.hasVisibleChildren && prop.propertyType == SerializedPropertyType.Generic && !prop.isArray;

        /// <summary>
        /// Draws a single property (array, nested class, or simple) and returns the height used.
        /// </summary>
        private static float DrawPropertyInRect(Rect rect, SerializedProperty prop, float yPos, float indent)
        {
            float startY = yPos;

            if (IsArray(prop))
            {
                float arrayHeight = GetReorderableListHeight(prop);
                Rect arrayRect = new Rect(rect.x + indent, yPos, rect.width - indent, arrayHeight);
                DrawArrayInRect(arrayRect, prop);
                yPos += arrayHeight + 2;
            }
            else if (IsNestedClass(prop))
            {
                Rect foldoutRect = new Rect(rect.x + indent, yPos, rect.width - indent, EditorGUIUtility.singleLineHeight);
                prop.isExpanded = EditorGUI.Foldout(foldoutRect, prop.isExpanded, prop.displayName, true);
                yPos += EditorGUIUtility.singleLineHeight + 2;

                if (prop.isExpanded)
                {
                    float childrenHeight = GetChildrenHeight(prop);
                    Rect childrenRect = new Rect(rect.x + indent + 15, yPos, rect.width - indent - 15, childrenHeight);
                    DrawChildrenInRect(childrenRect, prop);
                    yPos += childrenHeight;
                }
            }
            else
            {
                float propHeight = EditorGUI.GetPropertyHeight(prop, true);
                Rect propRect = new Rect(rect.x + indent, yPos, rect.width - indent, propHeight);
                EditorGUI.PropertyField(propRect, prop, true);
                yPos += propHeight + 2;
            }

            return yPos - startY;
        }

        /// <summary>
        /// Gets the height of a single property (array, nested class, or simple).
        /// </summary>
        private static float GetPropertyHeightInRect(SerializedProperty prop)
        {
            if (IsArray(prop))
            {
                return GetReorderableListHeight(prop) + 2;
            }
            else if (IsNestedClass(prop))
            {
                float height = EditorGUIUtility.singleLineHeight + 2; // foldout
                if (prop.isExpanded)
                {
                    height += GetChildrenHeight(prop);
                }
                return height;
            }
            else
            {
                return EditorGUI.GetPropertyHeight(prop, true) + 2;
            }
        }

        // ============== END HELPER METHODS ==============

        public static void ClearCache()
        {
            _reorderableLists.Clear();
            _nestedFoldouts.Clear();
            _foldoutAttrCache.Clear();
            _readOnlyAttrCache.Clear();
            _validatorAttrCache.Clear();
            _attrCacheChecked.Clear();
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
