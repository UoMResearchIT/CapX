using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    /// <summary>
    /// Portfolio to which a project belongs
    /// </summary>
    public enum Portfolio
    {
        Internal,
        [Description("Apps & Training")]
        AppsAndTraining,
        FBMH,
        FHUMS,
        FSE,
        MDS,
        WADS,
        [Description("Data Science & AI")]
        DataScienceAI,
        [Description("Digital Solutions")]
        DigitalSolutions,
        [Description("Research Lifecycle Programme")]
        RLP
    }
}
