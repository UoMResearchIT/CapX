using System.ComponentModel;
using PPMTool.Data.Enums.Attributes;

namespace PPMTool.Data.Enums
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
