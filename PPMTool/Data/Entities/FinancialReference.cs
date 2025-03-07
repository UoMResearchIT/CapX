using System;

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

        /// <summary>
        /// Returns a number between 0 and 1 depending on how much of a financial year takes place within the given window
        /// </summary>
        /// <param name="currentFY"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <exception cref="ArgumentException">If start date is after end date</exception>
        /// <returns></returns>
        internal static float GetProportionOfFinancialYearInRange(int currentFY, DateTime startDate, DateTime endDate)
        {
            var startFY = new DateTime(currentFY, 8, 1);
            var endFY = new DateTime(currentFY + 1, 7, 31);
            if (startDate.Date > endDate.Date) throw new ArgumentException("Start Date is after the End Date!");

            // Range starts before the FY
            if (startDate.Date < startFY)
            {
                // Range starts and ends before FY starts
                if (endDate.Date < startFY)
                {
                    return 0;
                }

                // Range starts before FY starts but ends in middle of FY
                else if (endDate.Date <= endFY)
                {
                    return (float)endDate.Subtract(startFY).TotalDays / 365f;
                }

                // Range starts before FY starts and ends after FY ends so range spans whole FY
                else
                {
                    return 1f;
                }
            }

            // Range starts in FY
            else if (startDate.Date <= endFY)
            {
                // Range starts and ends within FY
                if (endDate.Date <= endFY)
                {
                    return (float)endDate.Date.Subtract(startDate.Date).TotalDays / 365f;
                }

                // Range starts within FY and ends after FY ends
                else
                {
                    return (float)endFY.Subtract(startDate.Date).TotalDays / 365f;
                }
            }

            // Range starts after FY ends
            return 0f;
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
        /// <exception cref="ArgumentException">If grade is not a valid grade</exception>
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
                throw new ArgumentException($"Grade {grade} is invalid!");
            }
        }
    }
}
