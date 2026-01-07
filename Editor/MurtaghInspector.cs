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

            // draw non-grouped serialized properties 
            foreach (var property in GetNonGroupedProperties(_serializedProperties))
            {
                if (property.name.Equals("m_Script", System.StringComparison.Ordinal))
                {
                    using (new EditorGUI.DisabledScope(disabled: true))
                    {
                        EditorGUILayout.PropertyField(property);
                    }
                }
                else if (PropertyUtility.IsVisible(property))
                {
                    bool isReadOnly = PropertyUtility.GetAttribute<ReadOnlyAttribute>(property) != null;
                    using (new EditorGUI.DisabledScope(isReadOnly))
                    {
                        MurtaghEditorGUI.PropertyField_Layout(property, true);
                    }
                }
            }
            
            // draw foldout serialized properties
            foreach (var group in GetFoldoutProperties(_serializedProperties))
            {
                IEnumerable<SerializedProperty> visibleProperties = group.Where(p => PropertyUtility.IsVisible(p));
                if (!visibleProperties.Any())
                {
                    continue;
                }

                if (!_foldouts.ContainsKey(group.Key))
                {
                    _foldouts[group.Key] = new SavedBool($"{target.GetInstanceID()}.{group.Key}", false);
                }

                _foldouts[group.Key].Value = EditorGUILayout.Foldout(_foldouts[group.Key].Value, group.Key, true);
                if (_foldouts[group.Key].Value)
                {
                    foreach (var property in visibleProperties)
                    {
                        MurtaghEditorGUI.PropertyField_Layout(property, true);
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
