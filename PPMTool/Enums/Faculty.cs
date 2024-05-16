using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// Faculty to which a project belongs
    /// </summary>
    public enum Faculty
    {
        None,
        [Description("Research IT")]
        Internal,
        [Description("Professional Services and Cultural Institutions")]
        PSCI,
        [Description("Biology, Medicine and Health")]
        FBMH,
        [Description("Humanities")]
        FHUMS,
        [Description("Science and Engineering")]
        FSE,
        [Description("Research Lifecycle Programme")]
        RLP
    }
}
