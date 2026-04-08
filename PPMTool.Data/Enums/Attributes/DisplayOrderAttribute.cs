using System;

namespace PPMTool.Enums.Attributes
{
    /// <summary>
    /// Specifies the display order for an enum value in selection controls and grid sorting.
    /// Lower values appear first.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DisplayOrderAttribute : Attribute
    {
        public int Order { get; }

        public DisplayOrderAttribute(int order)
        {
            Order = order;
        }
    }
}
