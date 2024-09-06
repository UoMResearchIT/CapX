using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum CostModel
    {
        [Description("Technical Only using a specific Day Rate")]
        DayRate,
        [Description("Technical Only using the middle of the grade for assigned resources")]
        GradeBasedTechnicalOnly,
        [Description("Technical using the middle of the grade for assigned resources + leadership over project duration using at middle of the grade for PM")]
        GradeBasedTechAndLeadership
    }
}
