using Radzen;

namespace PPMTool.Data.Enums.Attributes
{
    /// <summary>
    /// Custom attribute to assign a badge style to an enum
    /// </summary>
    public class BadgeStyleAttribute : Attribute
    {
        public BadgeStyleAttribute(BadgeStyle style)
        {
            Style = style;
        }

        public BadgeStyle Style { get; }
    }
}
