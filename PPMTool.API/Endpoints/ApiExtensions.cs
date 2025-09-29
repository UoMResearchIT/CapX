using System.ComponentModel;

namespace PPMTool.API.Endpoints
{
    /// <summary>
    /// Provides general extensions methods for common repeatedable actions in minimal API endpoints.
    /// </summary>
    public static class APIExtensions
    {
        /// <summary>
        /// Returns the date of the Monday of the week for the given date.
        /// </summary>
        public static DateTime StartOfWeek(this DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Gets the string from an Enum's [Description] attribute.
        /// </summary>
        public static string GetDescription<T>(this T enumValue) where T : Enum
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field == null) return enumValue.ToString();

            var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

            return attribute == null ? enumValue.ToString() : attribute.Description;
        }
    }
}