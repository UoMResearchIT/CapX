using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    /// <summary>
    /// Faculty to which a project belongs
    /// </summary>
    public enum Faculty
    {
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
