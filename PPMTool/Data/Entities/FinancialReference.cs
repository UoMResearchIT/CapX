// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Enums;

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
        /// <param name="rate"></param>
        /// <returns></returns>
        internal double GetJuniorOrStandardAnnualCosts(Rate rate)
        {
            // Junior Rate
            if (rate == Rate.Junior)
            {
                return Grade51Costs;
            }

            // Standard Rate
            else if (rate == Rate.Standard)
            {
                return Grade71Costs;
            }

            // Senior rate
            else
            {
                return Grade75Costs;
            }
        }

        /// <summary>
        /// Returns the mid grade salary costs from the reference.
        /// Grade 4 always bottom of grade.
        /// Less than Grade4 returns G4.1.
        /// Greater than Grade 7 returns G7.1.
        /// </summary>
        /// <param name="grade"></param>
        /// <returns></returns>
        internal double GetMidGradeCosts(int grade)
        {
            if (grade <= 4)
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
            return Grade75Costs;
        }
    }
}
