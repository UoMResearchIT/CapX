using System;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    public class FinancialReference : ILoggableClass
    {
        public int FinancialReferenceId { get; set; }

        public int FinancialYear { get; set; } = DateTime.Today.Year;

        public float Grade41Costs { get; set; }

        public float Grade55Costs { get; set; }

        public float Grade65Costs { get; set; }

        public float Grade71Costs { get; set; }

        public float Grade75Costs { get; set; }

        public float RecoveryTarget { get; set; }

        public string GetSensibleObjectName()
        {
            return $"Financial Reference [{FinancialReferenceId}] - {FinancialYear}";
        }
    }
}
