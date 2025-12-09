using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Enums
{
    public enum CostModel
    {
        [Display(Name = "Day Rate")]
        [Description("Uses a specific day rate")]
        DayRate,
        [Display(Name = "Technical Only")]
        [Description("Planned costs computed from resource mid-grades; no leadership charge")]
        TechOnly,
        [Display(Name = "Technical and Leadership")]
        [Description("Planned costs computed from resource mid-grades; leadership charge added over duration")]
        TechAndLeadership,
        [Display(Name = "Technical Only with Indirects")]
        [Description("Planned costs computed from resource mid-grades; no leadership charge; indirects computed based on global rate")]
        TechOnlyWithIndirects,
        [Display(Name = "Technical and Leadership with Indirects")]
        [Description("Planned costs computed from resource mid-grades; leadership charge added over duration; indirects computed based on global rate")]
        TechAndLeadershipWithIndirects
    }
}
