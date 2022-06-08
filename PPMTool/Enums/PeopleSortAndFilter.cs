using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    public enum PeopleSortAndFilter
    {
        None,
        Name,
        [Description("Short Name")]
        ShortName,
        [Description("Hourly Rate")]
        HourlyRate,
        FTE,
        [Description("Next Available")]
        NextAvailable
    }
}
