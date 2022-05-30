using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    /// <summary>
    /// The type of task. This influences which of the three parameters remains fixed during scheduling.
    /// </summary>
    public enum TaskType
    {
        /// <summary>
        /// When scheduling, maintain work and vary duration and units
        /// </summary>
        [Description("Fixed Work")]
        FixedWork,

        /// <summary>
        /// When scheduling, maintain duration and vary work and units
        /// </summary>
        [Description("Fixed Duration")]
        FixedDuration,

        /// <summary>
        /// When scheduling, maintain resource units and vary work and duration
        /// </summary>
        [Description("Fixed Units")]
        FixedUnits
    }
}
