using System;

namespace Murtagh
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class MaxValueAttribute : ValidatorAttribute
    {
        public float MaxValue { get; private set; }

        public MaxValueAttribute(float minValue)
        {
            MaxValue = minValue;
        }

        public MaxValueAttribute(int minValue)
        {
            MaxValue = minValue;
        }
    }
}