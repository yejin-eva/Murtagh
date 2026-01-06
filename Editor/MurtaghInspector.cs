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

            bool anyMurtaghAttribute =
                _serializedProperties.Any(p => PropertyUtility.GetAttribute<IMurtaghAttribute>(p) != null);

            if (!anyMurtaghAttribute)
            {
                DrawDefaultInspector();
            }
            else
            {
                DrawSerializedProperties();
            }
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
