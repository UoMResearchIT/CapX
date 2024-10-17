using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum CompetencyCategory
    {
        General,
        [Description("Department Culture and Cohesion")]
        Culture,
        Communication,
        [Description("Version Control")]
        VersionControl,
        [Description("Software Architecture")]
        Architecture,
        [Description("Engineering Process")]
        EngineeringProcess,
        [Description("Coding and Development Practices")]
        DevelopmentPractice,
        [Description("Departmental Processes and Practices")]
        DepartmentalProcess,
        [Description("Data Management")]
        DataManagement,
        Leadership,
        [Description("Training, Coaching and Community Work")]
        Training,
        [Description("Staff and Operational Management")]
        StaffManagement,
        [Description("Service and Project Management")]
        ServiceProjectManagement
    }
}
