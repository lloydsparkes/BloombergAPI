using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Bloomberg.API
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Given a string, turn it into the apporiate Enum value represented by T. Returns defaultValue (which is default(T) by default) if there is no valid value
        /// 
        /// Note, given that Enums are structs, it is not possible to return null. So returning a default is better than an exception.
        /// 
        /// It utilises [Description] attributes where apporiate.
        /// </summary>
        /// <typeparam name="T">The type of the Enum to return</typeparam>
        /// <param name="s">The string to convert to enum</param>
        /// <param name="defaultValue">The default value to return, if there is no value</param>
        /// <returns></returns>
        public static T ToEnum<T>(this string s, T defaultValue = default(T)) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            if (string.IsNullOrEmpty(s))
            {
                return defaultValue;
            }

            var toFind = s.ToLower().Trim();

            foreach(T item in Enum.GetValues(typeof(T)))
            {
                if(item.ToString(CultureInfo.InvariantCulture).ToLower() == toFind)
                {
                    return item;
                }
                if (item.ToDescription().ToLower() == toFind)
                {
                    return item;
                }
            }

            return defaultValue;         
        }

        public static object ToEnum(this string s, Type t)
        {
            if (!t.IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            if (string.IsNullOrEmpty(s))
            {
                return ObjectHelpers.GetDefault(t);
            }

            var toFind = s.ToLower().Trim();

            foreach (object item in Enum.GetValues(t))
            {
                if (item.ToString().ToLower() == toFind)
                {
                    return item;
                }
                if (GetDescription(item).ToLower() == toFind)
                {
                    return item;
                }
            }

            return ObjectHelpers.GetDefault(t);
        }

        public static string GetDescription(object item)
        {
            var desc = GetAttributeOfType<DescriptionAttribute>(item);

            if (desc == null)
            {
                return item.ToString();
            }
            return desc.Description;
        }

        /// <summary>
        /// Given an Enum value, returns a string, representing that enum value.
        /// 
        /// Utilises any Description attributes, otherwise just returns .ToString()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string ToDescription<T>(this T item) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            var desc = GetAttributeOfType<DescriptionAttribute>(item);

            if (desc == null)
            {
                return item.ToString(CultureInfo.InvariantCulture);
            }
            return desc.Description;
        }

        /// <summary>
        /// Retrieves Attributes of a particular type from an Object. In a type safe manner
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumVal"></param>
        /// <returns></returns>
        public static T GetAttributeOfType<T>(object enumVal) where T : Attribute
        {
            if (enumVal == null)
            {
                throw new ArgumentException("enumVal must not be null");
            }

            var attrs = enumVal.GetType().GetMember(enumVal.ToString())[0].GetCustomAttributes(typeof(T), true);

            return (attrs.Length > 0) ? (T)attrs[0] : null;
        }

        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException($"Argumnent {typeof(T).FullName} is not an Enum");

            T[] arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf(arr, src) + 1;
            return (arr.Length == j) ? arr[0] : arr[j];
        }

        public static IEnumerable<T> ToEnumerable<T>()
            where T : struct 
        {
            if (typeof(T).IsEnum)
            {
                return Enum.GetValues(typeof(T)).Cast<T>().ToList();
            }
            throw new ArgumentException("T should be of type Enum");
        }

        public static IDictionary<string, object> GetDict(this Type enumType)
        {
            var typeList = new Dictionary<string, object>();

            //get the description attribute of each enum field
            DescriptionAttribute descriptionAttribute;
            foreach (var value in Enum.GetValues(enumType))
            {
                FieldInfo fieldInfo = enumType.GetField((value.ToString()));
                descriptionAttribute =
                    (DescriptionAttribute)Attribute.GetCustomAttribute(fieldInfo,
                        typeof(DescriptionAttribute));
                if (descriptionAttribute != null)
                {
                    typeList.Add(descriptionAttribute.Description, value);
                }
            }

            return typeList;
        }
    }
}
