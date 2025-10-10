using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Data.Helpers
{
    public abstract class ExportHelper
    {

        /// <summary>
        /// Given a person, prepare data from database with a weekly granularity
        /// </summary>
        /// <param name="person">Person who is being exported</param>
        /// <param name="projects">All projects as retrieved from the project service</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="finrefs">All the financial references</param>
        /// <returns>List of data items</returns>
        public static IEnumerable<AssignmentChunk> GetExportDataForPerson(Person person, IEnumerable<Project> projects, DateTime startDate, DateTime endDate, IEnumerable<FinancialReference> finrefs)
        {
            Debug.WriteLine($"** Building data for {person.Name}...");

            // Filter list of projects to those running during the window
            var projectsInWindow = projects
                .Where(x => !x.ProjectStatus.IsCancelled())
                .Where(x => x.IsWithin(startDate, endDate));

            Debug.WriteLine($"** {projectsInWindow.Count()} projects within window for {person.Name}");

            return FinanceHelper.GetAssignmentChunksInWindowFromProjects(person, startDate, endDate, projectsInWindow, finrefs);
        }

        /// <summary>
        /// Represents a collection of data related to the recovery report
        /// </summary>
        internal class RecoveryDataForDay
        {
            /// <summary>
            /// Day of the data
            /// </summary>
            public DateTime Date { get; }

            /// <summary>
            /// Person-Value data based on what the time recovered should be for that day based on WLMs
            /// </summary>
            public IDictionary<string, float> TargetRecovery { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data based on what the sum of the person's assignments say they have on that day
            /// </summary>
            public IDictionary<string, float> RecoveredTime { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which subtracts the target and recovered values and permits values over 100%
            /// </summary>
            public IDictionary<string, float> Net { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which subtracts the target and recovered values but caps off values over 100%
            /// </summary>
            public IDictionary<string, float> NetCapped { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data based on what the sum of the person's assignments say they have on that day including leadership
            /// </summary>
            public IDictionary<string, float> RecoveredTimeIncLeadership { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which subtracts the target and recovered values including leadership and permits values over 100%
            /// </summary>
            public IDictionary<string, float> NetIncLeadership { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which subtracts the target and recovered values including leadership but caps off values over 100%
            /// </summary>
            public IDictionary<string, float> NetCappedIncLeadership { get; } = new Dictionary<string, float>();

            /// <summary>
            /// Person-Value data which holds the costs for a person on that day
            /// </summary>
            public IDictionary<string, float> PersonCosts { get; } = new Dictionary<string, float>();

            public RecoveryDataForDay(DateTime date)
            {
                Date = date;
            }
        }

        /// <summary>
        /// Represents the summary over the window of the target and recovery for a person
        /// </summary>
        internal class RecoveryDataOverWindow
        {
            public string Name { get; }

            public float Target { get; private set; }

            public float TargetCosts { get; private set; }

            public float Recovered { get; private set; }

            public float RecoveredCosts { get; private set; }

            public float RecoveredIncLeadership { get; private set; }

            public float RecoveredIncLeadershipCosts { get; private set; }

            public float NetCapped { get; private set; }

            public float NetCappedCosts { get; private set; }

            public float NetCappedIncLead { get; private set; }

            public float NetCappedIncLeadCosts { get; private set; }

            public float PersonCosts { get; private set; }

            public RecoveryDataOverWindow(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Update the values based on the FTE for the day
            /// </summary>
            /// <param name="targetFTE"></param>
            /// <param name="assignedFTE"></param>
            /// <param name="assignedIncLeadFTE"></param>
            /// <param name="maxCap"></param>
            /// <param name="actualCosts">Costs of the person on the day (zero if left or not start)</param>
            /// <param name="costs">Annual cost / 365 (non-zero even if left or not started)</param>
            public void Update(
                float targetFTE,
                float assignedFTE,
                float assignedIncLeadFTE,
                float maxCap,
                float actualCosts,
                float costs)
            {
                Target += targetFTE;
                TargetCosts += targetFTE * costs;
                Recovered += assignedFTE;
                RecoveredCosts += assignedFTE * costs;
                RecoveredIncLeadership += assignedIncLeadFTE;
                RecoveredIncLeadershipCosts += assignedIncLeadFTE * costs;

                var net = assignedFTE - targetFTE;
                var netCapped = net > maxCap ? maxCap : net;
                NetCapped += netCapped;
                NetCappedCosts += netCapped * costs;

                net = assignedIncLeadFTE - targetFTE;
                netCapped = net > maxCap ? maxCap : net;
                NetCappedIncLead += netCapped;
                NetCappedIncLeadCosts += netCapped * costs;

                PersonCosts += actualCosts;
            }

            /// <summary>
            /// Returns the FTE of the target for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageTarget(int daysInWindow)
            {
                return Target / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the target FTE for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageTargetCosts()
            {
                return TargetCosts;
            }

            /// <summary>
            /// Returns the FTE of the recovered for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageRecovered(int daysInWindow)
            {
                return Recovered / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the recovered FTE for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageRecoveredCosts()
            {
                return RecoveredCosts;
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
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageRecoveredIncLeadershipCosts()
            {
                return RecoveredIncLeadershipCosts;
            }

            /// <summary>
            /// Returns the FTE of the capped net for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageNetCapped(int daysInWindow)
            {
                return NetCapped / daysInWindow;
            }

            /// <summary>
            /// Returns the average costs of the capped net FTE for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageNetCappedCosts()
            {
                return NetCappedCosts;
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
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetAverageNetCappedIncLeadershipCosts()
            {
                return NetCappedIncLeadCosts;
            }

            /// <summary>
            /// Gets the costs over the window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetEstimatedCosts()
            {
                return PersonCosts;
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

            // Create a context to be accesed on this thread
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

                    // Create a new item
                    var currentDayData = new RecoveryDataForDay(currentDate);

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
                        var projectAssignmentsFTE = chunks
                            .Where(x => !x.IsLeadershipAssignment)
                            .Sum(x => x.FTE);
                        var projectAssignmentsFTEIncLeadership = chunks
                            .Sum(x => x.FTE);

                        // Net value
                        var netValue = projectAssignmentsFTE - projectWorkTargetFTE;
                        var netValueIncLeadership = projectAssignmentsFTEIncLeadership - projectWorkTargetFTE;

                        // Net value capped
                        var maxOverAllocation = wlmTotal - projectWorkTargetFTE;
                        if (maxOverAllocation < 0) maxOverAllocation = 0;
                        var netValueCapped = netValue > maxOverAllocation ? maxOverAllocation : netValue;
                        var netValueCappedIncLeadership = netValueIncLeadership > maxOverAllocation ? maxOverAllocation : netValueIncLeadership;

                        // Add to the data dictionary (this is mainly for troubleshooting)
                        currentDayData.TargetRecovery.Add(person.Name, (float)projectWorkTargetFTE);
                        currentDayData.RecoveredTime.Add(person.Name, (float)projectAssignmentsFTE);
                        currentDayData.NetCapped.Add(person.Name, (float)netValueCapped);
                        currentDayData.RecoveredTimeIncLeadership.Add(person.Name, (float)projectAssignmentsFTEIncLeadership);
                        currentDayData.NetCappedIncLeadership.Add(person.Name, (float)netValueCappedIncLeadership);
                        currentDayData.PersonCosts.Add(person.Name, (float)actualCostsOnDay);

                        // Update the totals based on this day
                        windowRecoveryData
                            .First(x => x.Name == person.Name)
                            .Update(
                                (float)projectWorkTargetFTE,
                                (float)projectAssignmentsFTE,
                                (float)projectAssignmentsFTEIncLeadership,
                                (float)maxOverAllocation,
                                (float)actualCostsOnDay,
                                (float)referenceCostsForADay
                            );
                    }

                    // Advance the day
                    currentDate = currentDate.AddDays(1);
                }
            }

            return windowRecoveryData;
        }
    }
}
