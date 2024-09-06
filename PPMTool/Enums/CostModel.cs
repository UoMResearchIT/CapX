using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Enums
{
    public enum CostModel
    {
        [Display(Name = "Day Rate Technical Only")]
        [Description("Uses specific day rate")]
        DayRate,
        [Display(Name = "Grade Based Technical Only")]
        [Description("Uses middle of the grade for assigned resources")]
        GradeBasedTechnicalOnly,
        [Display(Name = "Grade Based Tech + Leadership")]
        [Description("Uses middle of the grade for assigned resources + middle of G7 over project duration for PM time")]
        GradeBasedTechAndLeadership
    }
}
