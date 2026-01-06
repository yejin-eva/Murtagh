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

            foreach (var property in _serializedProperties)
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
                    MurtaghEditorGUI.PropertyField_Layout(property, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
