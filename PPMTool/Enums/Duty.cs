using System.ComponentModel;
using PPMTool.Enums.Attributes;

namespace PPMTool.Enums
{
    /// <summary>
    /// Duty within which a particular timesheet code is categorised for WLM analysis
    /// </summary>
    public enum Duty
    {
        [Description("Other (inc. leave)")]
        [Colour("#CCC", "#000")]
        Other,
        [Description("Project Work")]
        [Colour("#008FFB")]
        ProjectWork,
        [Description("BAU, Training Delivery, Community Work and Coaching")]
        [Colour("#00E396", "#000")]
        BAU,
        [Description("Personal Development")]
        [Colour("#FEB019", "#000")]
        PersonalDevelopment,
        [Description("Staff Management")]
        [Colour("#FF4560")]
        StaffMgmt,
        [Description("Project and Service Management")]
        [Colour("#775DD0")]
        ProjectAndServiceMgmt,
        [Description("Technical Leadership")]
        [Colour("#2E294E")]
        RSA
    }
}
