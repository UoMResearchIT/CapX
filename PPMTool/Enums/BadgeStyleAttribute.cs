using System;
using Radzen;

namespace PPMTool.Enums
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
