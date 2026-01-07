using System;

namespace Murtagh
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ValidateInputAttribute : ValidatorAttribute
    {
        public string Callbackname { get; private set; }
        public string Message { get; private set; }

        public ValidateInputAttribute(string callbackName, string message = null)
        {
            Callbackname = callbackName;
            Message = message;
        }
    }
}