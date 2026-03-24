using System.ComponentModel;
using PPMTool.Data.Enums.Attributes;
using Radzen;

namespace PPMTool.Data.Enums
{
    public enum AssessmentStatus
    {
        [BadgeStyle(BadgeStyle.Danger)]
        Unmet,
        [BadgeStyle(BadgeStyle.Warning)]
        [Description("Partially Met")]
        PartiallyMet,
        [BadgeStyle(BadgeStyle.Success)]
        [Description("Fully Met")]
        FullyMet
    }
}
