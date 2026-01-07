using System;
using System.Reflection;
using UnityEditor;

namespace Murtagh.Editor
{
    public class ValidateInputPropertyValidator : PropertyValidatorBase
    {
        public override void ValidateProperty(SerializedProperty property)
        {
            ValidateInputAttribute validateInputAttribute =
                PropertyUtility.GetAttribute<ValidateInputAttribute>(property);
            object target = PropertyUtility.GetTargetObjectWithProperty(property);

            MethodInfo validationCallback = ReflectionUtility.GetMethod(target, validateInputAttribute.CallbackName);

            if (validationCallback == null || validationCallback.ReturnType != typeof(bool))
            {
                return;
            }

            ParameterInfo[] callbackParameters = validationCallback.GetParameters();

            if (callbackParameters.Length == 0)
            {
                if (!(bool)validationCallback.Invoke(target, null))
                {
                    if (string.IsNullOrEmpty(validateInputAttribute.Message))
                    {
                        MurtaghEditorGUI.HelpBox_Layout(property.name + " is not valid.", MessageType.Error, context: property.serializedObject.targetObject);
                    }
                    else
                    {
                        MurtaghEditorGUI.HelpBox_Layout(validateInputAttribute.Message, MessageType.Error, context: property.serializedObject.targetObject);
                    }
                }
            }
            else if (callbackParameters.Length == 1)
            {
                FieldInfo fieldInfo = ReflectionUtility.GetField(target, property.name);
                Type fieldType = fieldInfo.FieldType;
                Type parameterType = callbackParameters[0].ParameterType;

                if (fieldType == parameterType)
                {
                    if (!(bool)validationCallback.Invoke(target, new object[] { fieldInfo.GetValue(target) }))
                    {
                        if (string.IsNullOrEmpty(validateInputAttribute.Message))
                        {
                            MurtaghEditorGUI.HelpBox_Layout(property.name + " is not valid.", MessageType.Error, context: property.serializedObject.targetObject);
                        }
                        else
                        {
                            MurtaghEditorGUI.HelpBox_Layout(validateInputAttribute.Message, MessageType.Error, context: property.serializedObject.targetObject);
                        }
                    }
                }
                else
                {
                    string warning = "Field type not same as callback parameter type";
                    MurtaghEditorGUI.HelpBox_Layout(warning, MessageType.Warning, context: property.serializedObject.targetObject);
                }
            }
            else
            {
                string warning = validateInputAttribute.GetType().Name +
                                 " needs callback with bool return type and optional single parameter of same type as field";
                
                MurtaghEditorGUI.HelpBox_Layout(warning, MessageType.Warning, context: property.serializedObject.targetObject);
            }
        }
    }
}