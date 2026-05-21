using System.ComponentModel;

namespace PPMTool.Data.Enums
{
    /// <summary>
    /// This is the rate that a resource is to be costed at when using a non-day-rate approach to costing.
    /// It is used to drive the salary cost choice for the resource from the financial reference library.
    /// </summary>
    public enum Rate
    {
        [Description("Senior Bottom")]
        Standard,
        [Description("Junior Bottom")]
        Junior,
        [Description("Senior Mid")]
        Senior
    }
}
