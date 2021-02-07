using System;
using System.Collections.Generic;
using System.Text;

namespace Bloomberg.API.Model.Enriched
{
    public static class BloombergFieldExtensions
    {
        public static string BloombergFieldName(this BloombergFields field)
        {
            var t = EnumExtensions.GetAttributeOfType<BloombergFieldAttribute>(field);

            if (t == null)
            {
                return field.ToString();
            }
            return t.Name;
        }

        public static BloombergFields BloombergField(this string fieldName)
        {
            foreach (var field in Enum.GetValues(typeof(BloombergFields)))
            {
                if (BloombergFieldName((BloombergFields) field) == fieldName)
                {
                    return (BloombergFields) field;
                }    
            }

            return BloombergFields.UnknownField;    
        }

        public static Type BloombergType(this BloombergFields field)
        {
            var t = EnumExtensions.GetAttributeOfType<BloombergFieldAttribute>(field);

            if (t == null)
            {
                return typeof(string);
            }
            return t.DataType;
        }

        public static bool UseIndexAsKeyDictionary(this BloombergFields field)
        {
            var t = EnumExtensions.GetAttributeOfType<BloombergFieldAttribute>(field);

            if (t == null)
            {
                return false;
            }
            return t.UseIndexAsKeyDictionary;
        }

        public static bool UseProvidedIndexes(this BloombergFields field)
        {
            var t = EnumExtensions.GetAttributeOfType<BloombergFieldAttribute>(field);

            if (t == null)
            {
                return false;
            }
            return t.UseProvidedIndexes;
        }

        public static (int keyIndex, int valueIndex) ProvidedIndexes(this BloombergFields field)
        {
            var t = EnumExtensions.GetAttributeOfType<BloombergFieldAttribute>(field);

            if (t == null)
            {
                return (0, 0);
            }
            return (t.KeyIndex, t.ValueIndex);
        }
        
        public static bool IsEnumerableOf(Type bloombergType, Type type)
        {
            if (bloombergType.UnderlyingSystemType != null) bloombergType = bloombergType.UnderlyingSystemType;

            return bloombergType.IsGenericType && bloombergType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                   type.IsAssignableFrom(bloombergType.GetGenericArguments()[0]);
        }

        public static Type GetInnerType(Type bloombergType)
        {
            if (bloombergType.UnderlyingSystemType != null) bloombergType = bloombergType.UnderlyingSystemType;


            if(bloombergType.IsGenericType && bloombergType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return bloombergType.GetGenericArguments()[0];

            return null;
        }
    }
}
