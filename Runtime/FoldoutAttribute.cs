using System;

namespace Murtagh
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class FoldoutAttribute : Attribute, IMurtaghAttribute, IGroupAttribute
    {
        public string Name { get; private set; }

        public FoldoutAttribute(string name)
        {
            Name = name;
        }
    }
}