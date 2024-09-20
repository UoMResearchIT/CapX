using System;
using System.Linq;
using System.Reflection;
using DotNetExtensions;

namespace PPMTool.Enums
{
    public static class Extensions
    {
        public static string ToNiceString(this Enum me)
        {
            return me.GetDescription() ?? me.ToString();
        }

        /// <summary>
        ///     A generic extension method that aids in reflecting 
        ///     and retrieving any attribute that is applied to an `Enum`.
        ///     https://stackoverflow.com/questions/13099834/how-to-get-the-display-name-attribute-of-an-enum-member-via-mvc-razor-code
        /// </summary>
        public static TAttribute GetAttribute<TAttribute>(this Enum enumValue)
                where TAttribute : Attribute
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<TAttribute>();
        }
    }
}
