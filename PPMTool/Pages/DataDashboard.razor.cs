using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class DataDashboard : BasePage
    {
        private float personalDevFTE = 0.1f;
        private float architectureFTE = 0.05f;
        private float projectManFTE = 0.05f;
        private float staffManFTE = 0.05f;
        private float coachFTE = 0.1f;

        private float grade41Costs = 33333.55f;
        private float grade55Costs = 43172.16f;
        private float grade65Costs = 50935.80f;
        private float grade71Costs = 57458.16f;
        private float grade75Costs = 64797.29f;

        private float currentBudget = 1096765;
        private int numberOfStaffManagedByHead = 6;
        private DateTime startDate = DateTime.Today;
        private int yearsBehind = 1;
        private int yearsAhead = 2;
        private int generationProgressPercent = 0;

        private List<DemandChartItem> demandChartItems = new List<DemandChartItem>();
        private ApexChartOptions<DemandChartItem> demandChartOptions;

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        public DataDashboard()
        {
            // Default to the first day of the current financial year
            var today = DateTime.Today;
            startDate = new DateTime(today.Month < 8 ? today.Year - 1 : today.Year, 8, 1);

            // Set chart options
            demandChartOptions = new ApexChartOptions<DemandChartItem>
            {
                Chart = new Chart
                {
                    Stacked = true,
                    Type = ChartType.Area
                },
                Xaxis = new XAxis
                {
                    Type = XAxisType.Datetime
                },
                Yaxis = new List<YAxis>
                {
                    new YAxis
                    {
                        Labels = new YAxisLabels
                        {
                            Formatter = @"function (val, index) { return val.toFixed(2); }"
                        }
                    }
                }
            };
        }

        private void GenerateCharts()
        {
            Loading = true;
            generationProgressPercent = 0;
            Task.Run(() =>
            {
                Debug.WriteLine("** Starting generation...");

                // Clear the existing demand item list
                demandChartItems.Clear();

                // Max for spinner
                var maxGenWeeks = (yearsBehind + yearsAhead) * 52;

                // Get starting lists from the DB
                var people = PersonService.GetAll(context);
                var projects = ProjectService.GetAll(context);

                // For each week
                var currentWeekStart = startDate.AddYears(-yearsBehind);
                while (currentWeekStart < startDate.AddYears(yearsAhead))
                {
                    // Initialise
                    float wlmProject = 0f;
                    float wlmBAU = 0f;
                    float wlmPD = 0f;
                    float wlmPSM = 0f;
                    float wlmStaff = 0f;
                    float wlmRSA = 0f;
                    float assignmentUnder = 0f;
                    float assignmentOver = 0f;
                    int numStaff = 0;
                    int weekCount = 0;

                    // Update the spinner
                    generationProgressPercent = (int)Math.Round(weekCount / (float)maxGenWeeks);

                    // Get the projects that are running during the week (exclude those projects with no tasks that have default start date)
                    var projectsInDatabaseThisWeek = projects.Where(x => x.StartDate != default && x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6)));


                    // Cancelled //
                    var tasksOnCancelledProjectsThisWeek = projectsInDatabaseThisWeek.Where(x => x.ProjectStatus.IsCancelled()).SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));
                    var cancelledDemand = (float)tasksOnCancelledProjectsThisWeek.RoundedSum(x => x.Demand);


                    // All Projects (not cancelled) //

                    // Get projects not cancelled
                    var projectsThisWeekNotCancelled = projectsInDatabaseThisWeek.Where(x => !x.ProjectStatus.IsCancelled());

                    // Get number of confirmed and unconfirmed in this subset
                    var numberUnconfirmed = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus.IsUnconfirmed()).Count();
                    var numberConfirmed = projectsThisWeekNotCancelled.Count() - numberUnconfirmed;

                    // Get all tasks that run during the week
                    var tasksOnActiveProjectsThisWeek = projectsThisWeekNotCancelled.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));

                    // Get demand totals from tasks
                    var unmetDemand = (float)tasksOnActiveProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemand = (float)tasksOnActiveProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemand = totalDemand - unmetDemand;


                    // Finished //

                    // Get just total FTE of finished projects
                    var tasksOnFinishedProjectsThisWeek = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus == ProjectStatus.Finished).SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));
                    var metDemandFinished = (float)tasksOnFinishedProjectsThisWeek.RoundedSum(x => x.Demand);
                    var unmetDemandFinished = (float)tasksOnFinishedProjectsThisWeek.RoundedSum(x => x.UnmetDemand);


                    // Confirmed //

                    // Get just confirmed project tasks (not including finished)
                    var tasksOnConfirmedActiveProjectsThisWeek = projectsThisWeekNotCancelled.Where(x => !x.ProjectStatus.IsUnconfirmed()).SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));

                    // Get met and unmet demand for this subset
                    var unmetDemandConfirmed = (float)tasksOnConfirmedActiveProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemandConfirmed = (float)tasksOnConfirmedActiveProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemandConfirmed = totalDemandConfirmed - unmetDemandConfirmed;


                    // Unconfirmed //

                    // Calculate the unconfirmed totals
                    var unmetDemandUnconfirmed = unmetDemand - unmetDemandConfirmed;
                    var totalDemandUnconfirmed = totalDemand = totalDemandConfirmed;
                    var metDemandUnconfirmed = totalDemandUnconfirmed - unmetDemandUnconfirmed;

                    // Compute value of confirmed and unconfirmed projects using G7.1 salary costs
                    var confirmedValue = (float)Math.Round(totalDemandConfirmed * grade71Costs, 2);
                    var unConfirmedValue = (float)Math.Round(totalDemandUnconfirmed * grade71Costs, 2);


                    // People //

                    // Get the people who are employed for at least one day during the week
                    var peopleEmployedThisWeek = people.Where(x => x.StartDate <= currentWeekStart && (x.EndDate == null || x.EndDate >= currentWeekStart));

                    // Compute people based totals
                    foreach (var person in peopleEmployedThisWeek)
                    {
                        // Get the workload model that is active at the beginning of the week
                        var activeModel = person.WorkloadModelChanges.Where(x => x.ChangeDate <= currentWeekStart).OrderBy(x => x.ChangeDate).LastOrDefault();

                        // If no workload model active then default to the standard 100% project work model
                        if (activeModel == null)
                        {
                            activeModel = new WorkloadModelChange()
                            {
                                ChangeDate = currentWeekStart,
                                Person = person,
                                ProjectWorkFTE = person.FTE,
                                Notes = "Default model"
                            };
                        }

                        // Update totals
                        wlmProject += (float)activeModel.ProjectWorkFTE;
                        wlmBAU += (float)activeModel.BusinessAsUsualFTE;
                        wlmPD += (float)activeModel.PersonalDevelopmentFTE;
                        wlmPSM += (float)activeModel.ProjectAndServiceManagementFTE;
                        wlmStaff += (float)activeModel.StaffManagementFTE;
                        wlmRSA += (float)activeModel.ArchitectureFTE;
                        numStaff++;

                        // Get assignments for this person and sum for the week
                        var assignmentsThisWeek = tasksOnActiveProjectsThisWeek.SelectMany(x => x.AssignedResources.Where(x => x.Person.PersonId == person.PersonId));
                        var totalAssignmentFTE = assignmentsThisWeek.RoundedSum(x => x.AssignmentFTE);

                        // Increment the totals of under and overallocation
                        if (totalAssignmentFTE > activeModel.ProjectWorkFTE)
                        {
                            assignmentOver += (float)(totalAssignmentFTE - activeModel.ProjectWorkFTE);
                        }
                        else if (totalAssignmentFTE < activeModel.ProjectWorkFTE)
                        {
                            assignmentUnder += (float)(activeModel.ProjectWorkFTE - totalAssignmentFTE);
                        }
                    }

                    // Create a demand item and add it to the list
                    demandChartItems.Add(new DemandChartItem()
                    {
                        WeekStart = currentWeekStart,
                        ProjectFTE = wlmProject,
                        BAUFTE = wlmBAU,
                        PersonalDevFTE = wlmPD,
                        PSMFTE = wlmPSM,
                        StaffManFTE = wlmStaff,
                        RSAFTE = wlmRSA,
                        NumberOfStaff = numStaff,
                        NumberStaffRequiringLineManagement = numStaff - numberOfStaffManagedByHead,
                        NumberOfConfirmedProjects = numberConfirmed,
                        NumberOfUnconfirmedProjects = numberUnconfirmed,
                        UnmetDemandFTE = unmetDemand,
                        MetDemandFTE = metDemand,
                        TotalDemandFTE = totalDemand,
                        UnassignedFTE = wlmProject - metDemand,
                        ConfirmedMetDemandFTE = metDemandConfirmed,
                        ConfirmedUnmetDemandFTE = unmetDemandConfirmed,
                        UnconfirmedMetDemandFTE = metDemandUnconfirmed,
                        UnconfirmedUnmetDemandFTE = unmetDemandUnconfirmed,
                        ConfirmedDemandFTE = totalDemandConfirmed,
                        UnconfirmedDemandFTE = totalDemandUnconfirmed,
                        UnderallocationFTE = assignmentUnder,
                        OverallocationFTE = assignmentOver,
                        BenchProjectFTE = wlmProject - metDemand - unmetDemand,
                        ConfirmedValue = confirmedValue,
                        UnconfirmedValue = unConfirmedValue,
                        CancelledDemand = cancelledDemand,
                        FinishedMetDemand = metDemandFinished,
                        FinishedUnmetDemand = unmetDemandFinished,
                    });

                    // Move to next week
                    currentWeekStart = currentWeekStart.AddDays(7);
                    weekCount++;
                }

            }).ContinueWith(t =>
            {
                Debug.WriteLine($"** ...generation finished {t.Status}");
                Loading = false;
                InvokeAsync(StateHasChanged);
            });
        }
    }
}
