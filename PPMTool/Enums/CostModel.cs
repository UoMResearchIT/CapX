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
        [Description("Planned costs computed from resource rates; no leadership charge")]
        TechOnly,
        [Display(Name = "Technical and Leadership")]
        [Description("Planned costs computed from resource rates; leadership charge added over duration")]
        TechAndLeadership
    }
}
