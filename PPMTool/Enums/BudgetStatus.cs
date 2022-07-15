using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Enums
{
    public enum BudgetStatus
    {
        [Description("On Budget")]
        OnBudget,
        Overspend,
        Underspend
    }
}
