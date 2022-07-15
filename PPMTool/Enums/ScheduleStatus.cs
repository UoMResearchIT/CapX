using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    public enum ScheduleStatus
    {
        [Description("On Schedule")]
        OnSchedule,
        Ahead,
        Late
    }
}
