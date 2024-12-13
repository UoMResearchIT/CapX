using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum AssessmentStatus
    {
        Unmet,
        [Description("Partially Met")]
        PartiallyMet,
        [Description("Fully Met")]
        FullyMet
    }
}
