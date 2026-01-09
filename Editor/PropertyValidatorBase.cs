using System;
using System.Collections.Generic;
using UnityEditor;

namespace Murtagh.Editor
{
    /// <summary>
    /// Holds the result of a validation check.
    /// </summary>
    public struct ValidationResult
    {
        public string Message;
        public MessageType Type;

        public ValidationResult(string message, MessageType type = MessageType.Error)
        {
            Message = message;
            Type = type;
        }
    }

    public abstract class PropertyValidatorBase
    {
        /// <summary>
        /// Validates a property and returns a result if invalid, null if valid.
        /// </summary>
        public abstract ValidationResult? ValidateProperty(SerializedProperty property);
    }

    public static class ValidatorAttributeExtensions
    {
        private static Dictionary<Type, PropertyValidatorBase> _validatorsByAttributeType;

        static ValidatorAttributeExtensions()
        {
            _validatorsByAttributeType = new Dictionary<Type, PropertyValidatorBase>();
            _validatorsByAttributeType[typeof(MinValueAttribute)] = new MinValuePropertyValidator();
            _validatorsByAttributeType[typeof(MaxValueAttribute)] = new MaxValuePropertyValidator();
            _validatorsByAttributeType[typeof(RequiredAttribute)] = new RequiredPropertyValidator();
            _validatorsByAttributeType[typeof(ValidateInputAttribute)] = new ValidateInputPropertyValidator();
        }

        public static PropertyValidatorBase GetValidator(this ValidatorAttribute attribute)
        {
            return _validatorsByAttributeType.GetValueOrDefault(attribute.GetType());
        }
    }
}