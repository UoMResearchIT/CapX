using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// Compile-time reference to a feature in the system.
    /// </summary>
    public enum FeatureType
    {
        People = 0,
        [Description("Projects & Capacity")]
        ProjectsAndCapacity = 1,
        Absences = 2,
        Skills = 3,
        [Description("Development Journey")]
        DevelopmentJourney = 4,
        API = 5,
        Timesheets = 6,
        [Description("Project Finance")]
        ProjectFinance = 7,
        [Description("Data Dashboard")]
        DataDashboard = 8,
        None = 9
    }
}
