using System;
using System.Reflection;
using UnityEditor;

namespace Murtagh.Editor
{
    public class ValidateInputPropertyValidator : PropertyValidatorBase
    {
        public override ValidationResult? ValidateProperty(SerializedProperty property)
        {
            ValidateInputAttribute validateInputAttribute =
                PropertyUtility.GetAttribute<ValidateInputAttribute>(property);
            object target = PropertyUtility.GetTargetObjectWithProperty(property);

            MethodInfo validationCallback = ReflectionUtility.GetMethod(target, validateInputAttribute.CallbackName);

            if (validationCallback == null || validationCallback.ReturnType != typeof(bool))
            {
                return null;
            }

            ParameterInfo[] callbackParameters = validationCallback.GetParameters();

            if (callbackParameters.Length == 0)
            {
                if (!(bool)validationCallback.Invoke(target, null))
                {
                    string message = string.IsNullOrEmpty(validateInputAttribute.Message)
                        ? property.name + " is not valid."
                        : validateInputAttribute.Message;
                    return new ValidationResult(message, MessageType.Error);
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
                        string message = string.IsNullOrEmpty(validateInputAttribute.Message)
                            ? property.name + " is not valid."
                            : validateInputAttribute.Message;
                        return new ValidationResult(message, MessageType.Error);
                    }
                }
                else
                {
                    return new ValidationResult("Field type not same as callback parameter type", MessageType.Warning);
                }
            }
            else
            {
                string warning = validateInputAttribute.GetType().Name +
                                 " needs callback with bool return type and optional single parameter of same type as field";
                return new ValidationResult(warning, MessageType.Warning);
            }

            return null;
        }
    }
}