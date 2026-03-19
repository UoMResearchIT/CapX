using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums.Attributes;

namespace PPMTool.Enums
{
    public enum CostModel
    {
        [Display(Name = "Day Rate")]
        [Description("Uses a specific day rate")]
        [DisplayOrder(4)]
        DayRate,
        [Display(Name = "Technical Only")]
        [Description("Planned costs computed from resource mid-grades or rates; no leadership charge")]
        [DisplayOrder(3)]
        TechOnly,
        [Display(Name = "Technical and Leadership")]
        [Description("Planned costs computed from resource mid-grades or rates; leadership charge added over duration")]
        [DisplayOrder(1)]
        TechAndLeadership,
        [Display(Name = "Technical Only with Indirects")]
        [Description("Planned costs computed from resource mid-grades or rates; no leadership charge; indirects computed based on global rate")]
        [DisplayOrder(2)]
        TechOnlyWithIndirects,
        [Display(Name = "Technical and Leadership with Indirects")]
        [Description("Planned costs computed from resource mid-grades or rates; leadership charge added over duration; indirects computed based on global rate")]
        [DisplayOrder(0)]
        TechAndLeadershipWithIndirects
    }
}
