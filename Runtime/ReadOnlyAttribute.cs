using System;

namespace Murtagh
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : Attribute, IMurtaghAttribute
    {
        
    }
}