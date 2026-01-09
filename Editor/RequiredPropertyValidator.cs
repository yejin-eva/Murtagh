using UnityEditor;

namespace Murtagh.Editor
{
    public class RequiredPropertyValidator : PropertyValidatorBase
    {
        public override ValidationResult? ValidateProperty(SerializedProperty property)
        {
            RequiredAttribute requiredAttribute = PropertyUtility.GetAttribute<RequiredAttribute>(property);

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (property.objectReferenceValue == null)
                {
                    string errorMessage = property.name + " is required.";
                    if (!string.IsNullOrEmpty(requiredAttribute.Message))
                    {
                        errorMessage = requiredAttribute.Message;
                    }

                    return new ValidationResult(errorMessage, MessageType.Error);
                }
            }
            else
            {
                string warning = requiredAttribute.GetType().Name + " works only on reference types.";
                return new ValidationResult(warning, MessageType.Warning);
            }

            return null;
        }
    }
}