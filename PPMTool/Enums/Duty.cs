using System;
using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// Duty within which a particular timesheet code is categorised for WLM analysis
    /// </summary>
    public enum Duty
    {
        [Description("Other (inc. leave)")]
        [Colour("#CCC")]
        Other,
        [Description("Project Work")]
        [Colour("#FF4560")]
        ProjectWork,
        [Description("BAU, Training Delivery, Community Work and Coaching")]
        [Colour("#CCC")]
        BAU,
        [Description("Personal Development")]
        [Colour("#CCC")]
        PersonalDevelopment,
        [Description("Staff Management")]
        [Colour("#00E396")]
        StaffMgmt,
        [Description("Project and Service Management")]
        [Colour("#FEB019")]
        ProjectAndServiceMgmt,
        [Description("Research Software Architecture")]
        [Colour("#008FFB")]
        RSA
    }

    /// <summary>
    /// Add a hex colour string to an enum
    /// </summary>
    public class ColourAttribute : Attribute
    {
        public string ColourCode { get; }

        public ColourAttribute(string colourCode)
        {
            ColourCode = colourCode;
        }
    }
}
