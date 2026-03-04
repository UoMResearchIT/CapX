using System.ComponentModel;
using PPMTool.Enums.Attributes;

namespace PPMTool.Enums
{
    public enum FundingSourceType
    {
        [Description("Directly Allocated")]
        [ShortDescription("DA")]
        DA,
        [Description("Directly Incurred")]
        [ShortDescription("DI")]
        DI,
        [Description("Other")]
        [ShortDescription("Other")]
        Other
    }
}
