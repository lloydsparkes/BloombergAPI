using System;

namespace Bloomberg.API
{
    public class ObjectHelpers
    {
        public static object GetDefault(Type type)
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }
    }
}
