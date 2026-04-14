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

            public float StaffFTE { get; private set; }

            public float StaffCosts { get; private set; }

            public float PMFTE { get; private set; }

            public float PMCosts { get; private set; }

            public float SMFTE { get; private set; }

            public float SMCosts { get; private set; }

            public float BAUFTE { get; private set; }

            public float BAUCosts { get; private set; }

            public float PDFTE { get; private set; }

            public float PDCosts { get; private set; }

            public float TLFTE { get; private set; }

            public float TLCosts { get; private set; }

            public float ProjectWorkFTE { get; private set; }

            public float ProjectWorkCosts { get; private set; }

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
            /// <param name="staffFTE"></param>
            /// <param name="projManFTE"></param>
            /// <param name="servManFTE"></param>
            /// <param name="bauFTE"></param>
            /// <param name="pdFTE"></param>
            /// <param name="techLeadFTE"></param>
            /// <param name="assignmentFTE">Total FTE of the assignments for this person</param>
            /// <param name="wlmTotal">Capacity of the person based on their WLM</param>
            /// <param name="actualCosts">Costs of the person on the day (zero if left or not start)</param>
            /// <param name="dailyCosts">Annual cost / 365 (non-zero even if left or not started)</param>
            /// <param name="inBudget">Amount considered in budget for recovery</param>
            public void Update(
                float staffFTE,
                float projManFTE,
                float servManFTE,
                float bauFTE,
                float pdFTE,
                float techLeadFTE,
                float assignmentFTE,
                float wlmTotal,
                float actualCosts,
                float dailyCosts,
                float inBudget)
            {
                // Update the WLM categories
                StaffFTE += staffFTE;
                StaffCosts += staffFTE * dailyCosts;
                PMFTE += projManFTE;
                PMCosts += projManFTE * dailyCosts;
                SMFTE += servManFTE;
                SMCosts += servManFTE * dailyCosts;
                BAUFTE += bauFTE;
                BAUCosts += bauFTE * dailyCosts;
                PDFTE += pdFTE;
                PDCosts += pdFTE * dailyCosts;
                TLFTE += techLeadFTE;
                TLCosts += techLeadFTE * dailyCosts;

                // Project work is then the rest
                var nonProjectTime = staffFTE + projManFTE + servManFTE + bauFTE + pdFTE + techLeadFTE;
                var projectWorkFTE = wlmTotal - nonProjectTime;
                ProjectWorkFTE += projectWorkFTE;
                ProjectWorkCosts += projectWorkFTE * dailyCosts;

                // Update the amount recovered based on assignments
                RecoveredIncLeadership += assignmentFTE;
                RecoveredIncLeadershipCosts += assignmentFTE * dailyCosts;

                // Difference between recovered and target (cap as cannot recover more than wlmTotal for a person)
                var net = assignmentFTE - projectWorkFTE;
                var netCapped = net > nonProjectTime ? nonProjectTime : net;
                NetCappedIncLead += netCapped;
                NetCappedIncLeadCosts += netCapped * dailyCosts;

                PersonCosts += actualCosts;
                InBudgetCosts += inBudget;
            }

            /// <summary>
            /// Returns the FTE of the target for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageProjectWorkTarget(int daysInWindow)
            {
                return ProjectWorkFTE / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the target FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageProjectWorkTargetCosts()
            {
                return ProjectWorkCosts;
            }

            /// <summary>
            /// Returns the FTE of the staff management duty for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageStaffFTE(int daysInWindow)
            {
                return StaffFTE / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the staff management duty FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageStaffMgmtCosts()
            {
                return StaffCosts;
            }

            /// <summary>
            /// Returns the FTE of the project management duty for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageProjectManagementFTE(int daysInWindow)
            {
                return PMFTE / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the project management duty FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageProjectManagementCosts()
            {
                return PMCosts;
            }

            /// <summary>
            /// Returns the FTE of the service management duty for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageServiceManagementFTE(int daysInWindow)
            {
                return SMFTE / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the service management duty FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageServiceManagementCosts()
            {
                return SMCosts;
            }

            /// <summary>
            /// Returns the FTE of the BAU duty for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageBAUFTE(int daysInWindow)
            {
                return BAUFTE / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the BAU duty FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageBAUFTECosts()
            {
                return BAUCosts;
            }

            /// <summary>
            /// Returns the FTE of the personal development duty for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAveragePersonalDevelopmentFTE(int daysInWindow)
            {
                return PDFTE / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the personal development duty FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAveragePersonalDevelopmentFTECosts()
            {
                return PDCosts;
            }

            /// <summary>
            /// Returns the FTE of the tech leadership duty for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageTechLeadershipFTE(int daysInWindow)
            {
                return TLFTE / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the tech leadership duty FTE for the whole window
            /// </summary>
            /// <returns></returns>
            public float GetAverageTechLeadershipFTECosts()
            {
                return TLCosts;
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

                        // Get the WLM active on the day (or null if no WLM active)
                        var wlm = person.GetWorkloadModelOnDate(currentDate);
                        var gradeOnDay = wlm?.Grade ?? null;
                        var wlmTotal = wlm?.Total() ?? 0;

                        // Get day costs for person based on mid-grade
                        var actualCostsOnDay = (gradeOnDay == null || gradeOnDay > 7) ? 0 : finref.GetMidGradeCosts(gradeOnDay ?? 6);
                        actualCostsOnDay /= 365.0;
                        var referenceCostsForADay = actualCostsOnDay;

                        // Scale actual costs for any part-time arrangement or planned absence
                        actualCostsOnDay *= wlmTotal;

                        // If we don't have a grade for the day then we won't have reference costs so
                        // need to compute them from first or last grade we know about
                        if (gradeOnDay == null)
                        {
                            WorkloadModelChange tempWlm = null;
                            // If the grade is null then find the last WLM before the date
                            if (person.StartDate > currentDate)
                            {
                                // Get first WLM after the date
                                tempWlm = person.GetFirstWorkloadModelAfter(currentDate);

                            }
                            else if (person.EndDate != null && person.EndDate < currentDate)
                            {
                                // Get last WLM before the date
                                tempWlm = person.GetLastWorkloadModelBefore(currentDate);
                            }

                            // Default to G6 if we still can't find a WLM to use
                            referenceCostsForADay = finref.GetMidGradeCosts(tempWlm?.Grade ?? 6) / 365.0;
                        }

                        // Build out the values to update the totals with based on the WLM
                        var projectWorkFTE = wlm?.ProjectWorkFTE ?? 0;
                        var staffFTE = wlm?.StaffManagementFTE ?? 0;
                        var pmFTE = wlm?.ProjectManagementFTE ?? 0;
                        var smFTE = wlm?.ServiceManagementFTE ?? 0;
                        var bauFTE = wlm?.BusinessAsUsualFTE ?? 0;
                        var pdFTE = wlm?.PersonalDevelopmentFTE ?? 0;
                        var tlFTE = wlm?.ArchitectureFTE ?? 0;
                        wlmTotal = wlm?.Total() ?? 0;

                        // Get the sum of their assignments on the day (including leadership)
                        var projectAssignmentsFTEIncLeadership = chunks
                            .Sum(x => x.BilledFTE);

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
                                (float)staffFTE,
                                (float)pmFTE,
                                (float)smFTE,
                                (float)bauFTE,
                                (float)pdFTE,
                                (float)tlFTE,
                                (float)projectAssignmentsFTEIncLeadership,
                                (float)wlmTotal,
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
