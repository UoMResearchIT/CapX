using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// Duty within which a particular timesheet code is categorised for WLM analysis
    /// </summary>
    public enum Duty
    {
        [Description("Other (inc. leave)")]
        Other,
        [Description("Project Work")]
        ProjectWork,
        [Description("BAU, Training Delivery, Community Work and Coaching")]
        BAU,
        [Description("Personal Development")]
        PersonalDevelopment,
        [Description("Staff Management")]
        StaffMgmt,
        [Description("Project and Service Management")]
        ProjectAndServiceMgmt,
        [Description("Research Software Architecture")]
        RSA
    }
}
