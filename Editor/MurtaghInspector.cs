using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Murtagh.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class MurtaghInspector : UnityEditor.Editor
    {
        private List<SerializedProperty> _serializedProperties = new();
        private Dictionary<string, SavedBool> _foldouts = new();

        protected virtual void OnDisable()
        {
            MurtaghEditorGUI.ClearCache();
        }

        public override void OnInspectorGUI()
        {
            GetSerializedProperties(ref _serializedProperties);

            bool anyMurtaghAttribute = HasAnyMurtaghAttribute(_serializedProperties);

            if (!anyMurtaghAttribute)
            {
                DrawDefaultInspector();
            }
            else
            {
                DrawSerializedProperties();
            }
        }

        private bool HasAnyMurtaghAttribute(List<SerializedProperty> properties)
        {
            foreach (var property in properties)
            {
                // Check top-level
                if (PropertyUtility.GetAttribute<IMurtaghAttribute>(property) != null)
                    return true;

                // Check nested children (for arrays and nested classes)
                if (property.hasVisibleChildren)
                {
                    var iterator = property.Copy();
                    var endProperty = property.GetEndProperty();

                    if (iterator.NextVisible(true))
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(iterator, endProperty))
                                break;

                            if (PropertyUtility.GetAttribute<IMurtaghAttribute>(iterator) != null)
                                return true;

                        } while (iterator.NextVisible(true));
                    }
                }
            }
            return false;
        }

        protected void GetSerializedProperties(ref List<SerializedProperty> outSerializedProperties)
        {
            outSerializedProperties.Clear();
            using (var iterator = serializedObject.GetIterator())
            {
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        outSerializedProperties.Add(serializedObject.FindProperty(iterator.name));
                    } while (iterator.NextVisible(false));
                }
            }
        }

        protected void DrawSerializedProperties()
        {
            serializedObject.Update();

            // Track which foldout groups we've already drawn
            HashSet<string> drawnFoldoutGroups = new HashSet<string>();

            foreach (var property in _serializedProperties)
            {
                // Handle m_Script specially
                if (property.name.Equals("m_Script", System.StringComparison.Ordinal))
                {
                    using (new EditorGUI.DisabledScope(disabled: true))
                    {
                        EditorGUILayout.PropertyField(property);
                    }
                    continue;
                }

                // Check if this property belongs to a foldout group
                var foldoutAttr = PropertyUtility.GetAttribute<FoldoutAttribute>(property);

                if (foldoutAttr == null)
                {
                    // Non-grouped property - draw normally
                    if (PropertyUtility.IsVisible(property))
                    {
                        bool isReadOnly = PropertyUtility.GetAttribute<ReadOnlyAttribute>(property) != null;
                        using (new EditorGUI.DisabledScope(isReadOnly))
                        {
                            MurtaghEditorGUI.PropertyField_Layout(property, true);
                        }
                    }
                }
                else
                {
                    // Foldout property - draw the group if we haven't already
                    string groupName = foldoutAttr.Name;

                    if (!drawnFoldoutGroups.Contains(groupName))
                    {
                        drawnFoldoutGroups.Add(groupName);

                        // Get all properties in this foldout group
                        var groupProperties = _serializedProperties
                            .Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p)?.Name == groupName)
                            .Where(p => PropertyUtility.IsVisible(p))
                            .ToList();

                        if (groupProperties.Any())
                        {
                            if (!_foldouts.ContainsKey(groupName))
                            {
                                _foldouts[groupName] = new SavedBool($"{target.GetInstanceID()}.{groupName}", false);
                            }

                            _foldouts[groupName].Value = EditorGUILayout.Foldout(_foldouts[groupName].Value, groupName, true);

                            if (_foldouts[groupName].Value)
                            {
                                foreach (var groupProperty in groupProperties)
                                {
                                    MurtaghEditorGUI.PropertyField_Layout(groupProperty, true);
                                }
                            }
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        private static IEnumerable<SerializedProperty> GetNonGroupedProperties(IEnumerable<SerializedProperty> properties)
        {
            return properties.Where(p => PropertyUtility.GetAttribute<IGroupAttribute>(p) == null);
        }

        private static IEnumerable<IGrouping<string, SerializedProperty>> GetFoldoutProperties(
            IEnumerable<SerializedProperty> properties)
        {
            return properties
                .Where(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p) != null)
                .GroupBy(p => PropertyUtility.GetAttribute<FoldoutAttribute>(p).Name);
        }
    }
}
