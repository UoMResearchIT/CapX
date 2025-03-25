using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum FundingSourceType
    {
        [Description("Directly Allocated")]
        DA,
        [Description("Directly Incurred")]
        DI,
        [Description("Other")]
        Other
    }
}
