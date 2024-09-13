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
        internal double GetJuniorOrStandardAnnualCosts(int grade)
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

        /// <summary>
        /// Returns the mid grade salary costs from the reference (grade 4 always bottom of grade)
        /// </summary>
        /// <param name="grade"></param>
        /// <returns></returns>
        /// <exception cref="Exception">If grade is not a valid grade</exception>
        internal double GetMidGradeCosts(int grade)
        {
            if (grade == 4)
            {
                return Grade41Costs;
            }
            else if (grade == 5)
            {
                return Grade55Costs;
            }
            else if (grade == 6)
            {
                return Grade65Costs;
            }
            else if (grade == 7)
            {
                return Grade75Costs;
            }
            else
            {
                throw new Exception($"Grade {grade} is invalid!");
            }
        }
    }
}
