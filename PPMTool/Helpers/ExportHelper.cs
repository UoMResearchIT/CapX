using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;

namespace PPMTool.Helpers
{
    public abstract class ExportHelper
    {
        /// <summary>
        /// Represents the summary over the window of the target and recovery for a person
        /// </summary>
        internal class RecoveryDataOverWindow
        {
            public string Name { get; }

            public float TargetFTE { get; private set; }

            public float TargetCosts { get; private set; }

            public float RecoveredIncLeadership { get; private set; }

            public float RecoveredIncLeadershipCosts { get; private set; }

            public float NetCappedIncLead { get; private set; }

            public float NetCappedIncLeadCosts { get; private set; }

            public float PersonCosts { get; private set; }

            public float InBudgetCosts { get; private set; }

            public RecoveryDataOverWindow(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Update the values based on the FTE for the day
            /// </summary>
            /// <param name="targetFTE"></param>
            /// <param name="assignedIncLeadFTE"></param>
            /// <param name="maxCap"></param>
            /// <param name="actualCosts">Costs of the person on the day (zero if left or not start)</param>
            /// <param name="costs">Annual cost / 365 (non-zero even if left or not started)</param>
            /// <param name="inBudget">Amount considered in budget for recovery</param>
            public void Update(
                float targetFTE,
                float assignedIncLeadFTE,
                float maxCap,
                float actualCosts,
                float costs,
                float inBudget)
            {
                TargetFTE += targetFTE;
                TargetCosts += targetFTE * costs;
                RecoveredIncLeadership += assignedIncLeadFTE;
                RecoveredIncLeadershipCosts += assignedIncLeadFTE * costs;

                var net = assignedIncLeadFTE - targetFTE;
                var netCapped = net > maxCap ? maxCap : net;
                NetCappedIncLead += netCapped;
                NetCappedIncLeadCosts += netCapped * costs;

                PersonCosts += actualCosts;
                InBudgetCosts += inBudget;
            }

            /// <summary>
            /// Returns the FTE of the target for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageTarget(int daysInWindow)
            {
                return TargetFTE / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the target FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageTargetCosts()
            {
                return TargetCosts;
            }

            /// <summary>
            /// Returns the FTE of the recovered including leadership for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageRecoveredIncLeadership(int daysInWindow)
            {
                return RecoveredIncLeadership / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the recovered including leadership FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageRecoveredIncLeadershipCosts()
            {
                return RecoveredIncLeadershipCosts;
            }

            /// <summary>
            /// Returns the FTE of the capped net including leadership for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageNetCappedIncLeadership(int daysInWindow)
            {
                return NetCappedIncLead / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the capped net including leadership FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageNetCappedIncLeadershipCosts()
            {
                return NetCappedIncLeadCosts;
            }

            /// <summary>
            /// Gets the costs over the window
            /// </summary>
            /// <returns></returns>
            public float GetEstimatedCosts()
            {
                return PersonCosts;
            }

            /// <summary>
            /// The total budget amounts as accumlated over the days of the window
            /// </summary>
            /// <returns></returns>
            public float GetInBudgetTotals()
            {
                return InBudgetCosts;
            }
        }

        /// <summary>
        /// Method to generate the recovery information for the report
        /// </summary>
        /// <param name="peopleActive">List of people to be considered</param>
        /// <param name="assignmentChunks">Set of assignment chunks for active people split by WLM change and FY change</param>
        /// <param name="contextFactory"></param>
        /// <param name="personService"></param>
        /// <param name="projectService"></param>
        /// <param name="financialReferenceService"></param>
        /// <returns></returns>
        internal static IEnumerable<RecoveryDataOverWindow> GetRecoveryData(
            IEnumerable<Person> peopleActive,
            IList<AssignmentChunk> assignmentChunks,
            IDbContextFactory<PPMToolContext> contextFactory,
            PersonService personService,
            ProjectService projectService,
            FinancialReferenceService financialReferenceService)
        {
            // Set the report length
            var startDate = assignmentChunks.Min(x => x.StartDate.Date).Date;
            var endDate = assignmentChunks.Max(x => x.EndDate.Date).Date;
            var currentDate = startDate;
            var windowRecoveryData = new List<RecoveryDataOverWindow>();

            // Create a context to be accessed on this thread
            using (var context = contextFactory.CreateDbContext())
            {
                // Get data for each person active in the window
                // Get projects active in the window with their subtasks and resources
                var projectsInWindow = projectService.GetAll(context)
                    .Where(x => !x.ProjectStatus.IsCancelled())
                    .Where(x => x.IsWithin(startDate, endDate));

                // Get fin refs
                var currentFY = FinancialReference.GetFinancialYear(startDate);
                var finref = financialReferenceService.GetFinancialReferenceForDate(context, startDate);

                // Initialise the totals
                foreach (var person in peopleActive)
                {
                    windowRecoveryData.Add(new RecoveryDataOverWindow(person.Name));
                }

                // Loop over the days to get day by day data
                while (currentDate <= endDate)
                {
                    // If the FY has changed then update the finref
                    if (FinancialReference.GetFinancialYear(currentDate) != currentFY)
                    {
                        finref = financialReferenceService.GetFinancialReferenceForDate(context, currentDate);
                    }

                    // Loop over each person employed in the window
                    foreach (var person in peopleActive)
                    {
                        // Get chunks belonging to this person and running on the day
                        var chunks = assignmentChunks
                            .Where(x => x.EmployeeName == person.Name && DateRange.IsWithin(currentDate, x.StartDate, x.EndDate));

                        // Get the project work amount on the day
                        var projectWorkTargetFTE = person.GetProjectWorkAvailabilityOnDate(currentDate);
                        var wlmTotal = person.GetWorkloadModelTotalOnDate(currentDate);
                        var gradeOnDay = person.GetGradeOnDate(currentDate);

                        // Get day costs for person based on mid-grade and scaled by any part-time arrangement
                        var actualCostsOnDay = gradeOnDay == null ? 0 : finref.GetMidGradeCosts(gradeOnDay ?? 6);
                        actualCostsOnDay /= 365.0;
                        actualCostsOnDay *= wlmTotal;

                        // If we don't have any costs for the day then need to compute them from first or last grade we know about
                        var referenceCostsForADay = actualCostsOnDay;
                        if (actualCostsOnDay == 0)
                        {
                            WorkloadModelChange wlm = null;
                            // If the grade is null then find the last WLM before the date
                            if (person.StartDate > currentDate)
                            {
                                // Get first WLM after the date
                                wlm = person.GetFirstWorkloadModelAfter(currentDate);

                            }
                            else if (person.EndDate != null && person.EndDate < currentDate)
                            {
                                // Get last WLM before the date
                                wlm = person.GetLastWorkloadModelBefore(currentDate);
                            }
                            referenceCostsForADay = finref.GetMidGradeCosts(wlm?.Grade ?? 6) / 365.0;
                        }

                        // Get the sum of their assignments on the day with and without leadership
                        var projectAssignmentsFTEIncLeadership = chunks
                            .Sum(x => x.BilledFTE);

                        // Net value
                        var netValueIncLeadership = projectAssignmentsFTEIncLeadership - projectWorkTargetFTE;

                        // Net value capped
                        var maxOverAllocation = wlmTotal - projectWorkTargetFTE;
                        if (maxOverAllocation < 0) maxOverAllocation = 0;
                        var netValueCappedIncLeadership = netValueIncLeadership > maxOverAllocation ? maxOverAllocation : netValueIncLeadership;

                        // Amount in budget for the day across all chunks
                        var inBudget = chunks.Sum(x =>
                        {
                            var duration = x.EndDate.Subtract(x.StartDate).TotalDays + 1;
                            return x.AmountCovered / duration;
                        });

                        // Update the totals based on this day
                        windowRecoveryData
                            .First(x => x.Name == person.Name)
                            .Update(
                                (float)projectWorkTargetFTE,
                                (float)projectAssignmentsFTEIncLeadership,
                                (float)maxOverAllocation,
                                (float)actualCostsOnDay,
                                (float)referenceCostsForADay,
                                (float)inBudget
                            );
                    }

                    // Advance the day
                    currentDate = currentDate.AddDays(1);
                }
            }

            return windowRecoveryData;
        }

        /// <summary>
        /// Represents the summary of the financial state of a project over an export period
        /// </summary>
        internal class ProjectBudgetSummary
        {
            public string ProjectName { get; set; }

            public double PlannedCosts { get; set; }

            public double RecoveredCosts { get; set; }
        }
    }
}
