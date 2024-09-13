using System;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    public class FinancialReference : ILoggableClass
    {
        public int FinancialReferenceId { get; set; }

        public int FinancialYear { get; set; } = DateTime.Today.Year;

        public float Grade41Costs { get; set; }

        public float Grade51Costs { get; set; }

        public float Grade55Costs { get; set; }

        public float Grade65Costs { get; set; }

        public float Grade71Costs { get; set; }

        public float Grade75Costs { get; set; }

        public float RecoveryTarget { get; set; }

        /// <summary>
        /// Helper to get a financial year from a DateTime
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static int GetFinancialYear(DateTime date)
        {
            return date.Date.Month < 8 ? date.Date.Year - 1 : date.Date.Year;
        }

        public string GetSensibleObjectName()
        {
            return $"Financial Reference [{FinancialReferenceId}] - {FinancialYear}";
        }

        /// <summary>
        /// Gets a suitable standard or junior figure from the financial references for annual costs
        /// </summary>
        /// <param name="grade"></param>
        /// <returns></returns>
        /// <exception cref="Exception">If a grade lower than 4 is found</exception>
        internal double GetSuitableCostForGrade(int grade)
        {
            // Junior Rate
            if (grade == 4 || grade == 5)
            {
                return Grade51Costs;
            }

            // Standard Rate
            else if (grade > 5)
            {
                return Grade71Costs;
            }

            else
            {
                throw new Exception($"Grade {grade} is invalid!");
            }
        }
    }
}
