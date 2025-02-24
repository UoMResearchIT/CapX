using System.ComponentModel;
using Radzen;
using static PPMTool.Enums.Extensions;

namespace PPMTool.Enums
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
