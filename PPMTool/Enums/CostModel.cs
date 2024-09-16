using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Enums
{
    public enum CostModel
    {
        [Display(Name = "Day Rate")]
        [Description("Uses specific day rate")]
        DayRate,
        [Display(Name = "Two-Tier Rate Technical Only (Standard)")]
        [Description("Standard or Junior rate for resources; planned cost based on Standard rate")]
        TwoTierRateTechOnlyStd,
        [Display(Name = "Two-Tier Rate Technical Only (Junior)")]
        [Description("Standard or Junior rate for resources; planned cost based on Junior rate")]
        TwoTierRateTechOnlyJun,
        [Display(Name = "Two-Tier Rate (Std) + Leadership")]
        [Description("Standard or Junior rate for resources; planned cost based on Standard rate; middle of G7 for PM time over project duration")]
        TwoTierTechStdAndLeadership,
        [Display(Name = "Two-Tier Rate (Jun) + Leadership")]
        [Description("Standard or Junior rate for resources; planned cost based on Junior rate; middle of G7 for PM time over project duration")]
        TwoTierTechJunAndLeadership
    }
}
