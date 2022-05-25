using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    /// <summary>
    /// The type of task. This influences which of the three parameters remains fixed during scheduling.
    /// </summary>
    public enum TaskType
    {
        FixedWork,
        FixedDuration,
        FixedUnits
    }
}
