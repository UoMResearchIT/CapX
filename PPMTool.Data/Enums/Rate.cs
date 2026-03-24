using System.ComponentModel;

namespace PPMTool.Data.Enums
{
    /// <summary>
    /// This is the rate that a resource is to be costed at when using a non-day-rate approach to costing.
    /// It is used to drive the salary cost choice for the resource from the financial reference library.
    /// </summary>
    public enum Rate
    {
        [Description("Grade 7.1")]
        Standard,
        [Description("Grade 5.1")]
        Junior,
        [Description("Grade 7.5")]
        Senior
    }
}
