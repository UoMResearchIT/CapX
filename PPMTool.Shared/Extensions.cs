using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace PPMTool
{
    public static class Extensions
    {
        /// <summary>
        /// Extension method to get all messages from an exception and its inner exceptions.
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static string GetAllMessages(this Exception exp)
        {
            string message = string.Empty;
            Exception innerException = exp;

            do
            {
                message = message + (string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message);
                innerException = innerException.InnerException;
            }
            while (innerException != null);

            return message;
        }

        /// <summary>
        /// Extension method to get enum value from description attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="myEnum"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static T GetValueFromDescription<T>(this T myEnum, string description) where T : Enum
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found", nameof(description));
        }

        /// <summary>
        /// Extension method to get the description attribute of an enum value
        /// </summary>
        /// <param name="genericEnum"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum genericEnum)
        {
            Type genericEnumType = genericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(genericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }
            return genericEnum.ToString();
        }

        /// <summary>
        /// Cleans a string with some basic formatting, generally
        /// used to compare items to check for duplicates
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Clean(this string item)
        {
            return item.Trim().ToLower();
        }
    }
}
