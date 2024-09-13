using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Enums
{
    public enum CostModel
    {
        [Display(Name = "Day Rate")]
        [Description("Uses specific day rate")]
        DayRate,
        [Display(Name = "Grade Based Technical Only (Standard)")]
        [Description("Uses middle of the grade for assigned resources; budget based on standard rate")]
        GradeBasedTechnicalOnlyStandard,
        [Display(Name = "Grade Based Technical Only (Junior)")]
        [Description("Uses middle of the grade for assigned resources; budget based on junior rate")]
        GradeBasedTechnicalOnlyJunior,
        [Display(Name = "Grade Based Tech (Std) + Leadership")]
        [Description("Uses middle of the grade for assigned resources; budget based on standard rate; middle of G7 for PM time over project duration")]
        GradeBasedTechStdAndLeadership,
        [Display(Name = "Grade Based Tech (Jun) + Leadership")]
        [Description("Uses middle of the grade for assigned resources; budget based on junior rate; middle of G7 for PM time over project duration")]
        GradeBasedTechJunAndLeadership
    }
}
