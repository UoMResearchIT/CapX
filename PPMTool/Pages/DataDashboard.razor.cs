using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class DataDashboard : BasePage
    {
        private float personalDevFTE = 0.1f;
        private float architectureFTE = 0.05f;
        private float projectManFTE = 0.05f;
        private float staffManFTE = 0.05f;
        private float coachFTE = 0.1f;
        private float appSupportPSMFTE = 0.1f;
        private float trainingPSMFTE = 0.1f;
        private float otherPSMFTE = 0.5f;

        /// <summary>
        ///  The amount of money that we are expected to recover:
        ///  i.e. negative, blue values in Column E of the tracker which represent the salary costs removed from the budget
        /// </summary>
        private float recoveryTarget = 1118849;

        private int numberOfStaffManagedByHead = 6;
        private DateTime startDate = DateTime.Today;
        private int yearsAhead = 3;
        private bool showFinishedAsSeparate = false;

        private IEnumerable<Person> people;
        private IEnumerable<Project> projects;

        private List<DemandChartItem> demandChartItems = new List<DemandChartItem>();
        private List<DutyChartItem> dutyChartItems = new List<DutyChartItem>();
        private ApexChartOptions<DemandChartItem> demandChartOptions;
        private ApexChartOptions<DemandChartItem> fteChartOptions;
        private ApexChartOptions<DemandChartItem> ytdChartOptions;
        private ApexChartOptions<DutyChartItem> dutyChartOptions;


        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private FinancialReferenceService FinancialReferenceService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Editing only permitted by superusers
            EditAuthorised = AuthenticationState?.User.IsInRole("Superuser") ?? false;

            // Get starting lists from the DB
            people = PersonService.GetAll(context);
            projects = ProjectService.GetAll(context);

            // Default to the first day of the previous financial year
            var today = DateTime.Today;
            startDate = new DateTime(today.Month < 8 ? today.Year - 2 : today.Year - 1, 8, 1);

            // Set chart options
            ytdChartOptions = new ApexChartOptions<DemandChartItem>
            {
                Chart = new Chart
                {
                    Type = ChartType.Line,
                    Animations = new Animations { Enabled = false }
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
                            Formatter = @"function (val, index) { return '£' + val.toFixed(0).replace(/\B(?<!\.\d*)(?=(\d{3})+(?!\d))/g, "","") }"
                        }
                    }
                },
                Annotations = new Annotations
                {
                    Xaxis = new List<AnnotationsXAxis>
                    {
                        new AnnotationsXAxis()
                        {
                            X = DateTime.Today.ToUnixTimeMilliseconds(),
                            BorderWidth = 2,
                            StrokeDashArray = 5,
                            BorderColor = "#888",
                            Label = new Label
                            {
                                Text = "Today",
                                Position = LabelPosition.Left
                            }
                        }
                    }
                }
            };

            fteChartOptions = new ApexChartOptions<DemandChartItem>
            {
                Chart = new Chart
                {
                    Type = ChartType.Line,
                    Animations = new Animations { Enabled = false }
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
                },
                Annotations = new Annotations
                {
                    Xaxis = new List<AnnotationsXAxis>
                    {
                        new AnnotationsXAxis()
                        {
                            X = DateTime.Today.ToUnixTimeMilliseconds(),
                            BorderWidth = 2,
                            StrokeDashArray = 5,
                            BorderColor = "#888",
                            Label = new Label
                            {
                                Text = "Today",
                                Position = LabelPosition.Left
                            }
                        }
                    }
                }
            };

            demandChartOptions = new ApexChartOptions<DemandChartItem>
            {
                Chart = new Chart
                {
                    Type = ChartType.Area,
                    Animations = new Animations { Enabled = false }
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
                },
                Fill = new Fill
                {
                    Type = new FillTypeSelections(Enumerable.Repeat(FillType.Solid, 8).ToArray()),
                    Opacity = new Opacity(Enumerable.Repeat(0.7, 8).ToArray())
                },
                Annotations = new Annotations
                {
                    Xaxis = new List<AnnotationsXAxis>
                    {
                        new AnnotationsXAxis()
                        {
                            X = DateTime.Today.ToUnixTimeMilliseconds(),
                            BorderWidth = 2,
                            StrokeDashArray = 5,
                            BorderColor = "#888",
                            Label = new Label
                            {
                                Text = "Today",
                                Position = LabelPosition.Left
                            }
                        }
                    }
                },
                Markers = new Markers
                {
                    Size = 0
                },
                Tooltip = new ApexCharts.Tooltip
                {
                    Marker = new TooltipMarker
                    {
                        Show = false
                    },
                    Custom = @"function({series, seriesIndex, dataPointIndex, w}) { return formatTooltip({series, seriesIndex, dataPointIndex, w}); }"

                }
            };

            dutyChartOptions = new ApexChartOptions<DutyChartItem>
            {
                Chart = new Chart
                {
                    Type = ChartType.Bar,
                    Animations = new Animations { Enabled = false }
                },
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = false
                    }
                }
            };

            // Start the spinners
            Loading = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("setFinishedFlag", showFinishedAsSeparate);

                // Generate the charts
                GenerateCharts();
            }
        }

        private void FinishedChanged(bool state)
        {
            JSRuntime.InvokeVoidAsync("setFinishedFlag", showFinishedAsSeparate);
            Task.Delay(500);
            GenerateCharts();
        }

        private void GenerateCharts()
        {
            Loading = true;
            Task.Run(() =>
            {
                Debug.WriteLine("** Starting generation...");

                // Variable chart options
                demandChartOptions.Fill.Colors = GetColours();
                demandChartOptions.Colors = GetColours();

                // Clear the existing demand item lists
                demandChartItems.Clear();
                dutyChartItems.Clear();

                // TODO: Pre-compute the weekly values for each project

                // Tracked values
                var currentWeekStart = startDate;
                var currentFY = 0;
                var endDate = startDate.AddYears(yearsAhead);
                var startFY = FinancialReference.GetFinancialYear(startDate);
                var endFY = FinancialReference.GetFinancialYear(endDate);
                int numberOfWeeks = 0;
                List<string> dutyXLabels = new List<string>();
                FinancialReference currentFinRef;
                float recoveryTargetPerWeek = 0f;

                // For each week
                while (currentWeekStart < endDate)
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

                    // If quarter has changed then create a new object for the quarter
                    if (dutyChartItems.Count == 0 || dutyChartItems.Last().Year != currentWeekStart.Year || dutyChartItems.Last().Period != (int)Math.Ceiling(currentWeekStart.Month / 3f))
                    {
                        dutyChartItems.LastOrDefault()?.UpdateMinMax();
                        dutyChartItems.Add(new DutyChartItem
                        {
                            WeekStart = currentWeekStart
                        });
                        numberOfWeeks = 0;
                        dutyXLabels.Add($"Q{dutyChartItems.Last().Period} {dutyChartItems.Last().Year}");
                    }

                    // If financial year has changed then get the next financial reference and update recovery target
                    if (currentFY != FinancialReference.GetFinancialYear(currentWeekStart))
                    {
                        currentFinRef = FinancialReferenceService.GetFinancialReferenceForDate(context, currentWeekStart);
                        currentFY = FinancialReference.GetFinancialYear(currentWeekStart);

                        // Compute how many weeks of this FY run within the window of the graph
                        var proportionOfFY = FinancialReference.GetProportionOfFinancialYearInRange(currentFY, startDate, endDate);
                        recoveryTargetPerWeek = currentFinRef.RecoveryTarget * proportionOfFY / 52;
                    }

                    // Get the projects that are running during the week (exclude those projects with no tasks as they will have "default" start date)
                    var projectsInDatabaseThisWeek = projects.Where(x => x.StartDate != default && x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6)));


                    /// Cancelled ///
                    var tasksOnCancelledProjectsThisWeek = projectsInDatabaseThisWeek.Where(x => x.ProjectStatus.IsCancelled()).SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));
                    var cancelledDemand = (float)tasksOnCancelledProjectsThisWeek.RoundedSum(x => x.Demand);


                    /// All Projects (not cancelled) ///

                    // Get projects not cancelled
                    var projectsThisWeekNotCancelled = projectsInDatabaseThisWeek.Where(x => !x.ProjectStatus.IsCancelled());

                    // Get number of confirmed and unconfirmed in this subset (including finished)
                    var numberUnconfirmed = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus.IsUnconfirmed()).Count();
                    var numberConfirmed = projectsThisWeekNotCancelled.Count() - numberUnconfirmed;

                    // Get all tasks that run during the week
                    var tasksOnActiveProjectsThisWeek = projectsThisWeekNotCancelled.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));

                    // Get demand totals from tasks
                    var unmetDemand = (float)tasksOnActiveProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemand = (float)tasksOnActiveProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemand = (float)Math.Round(totalDemand - unmetDemand);


                    /// Finished ///

                    // Get just total FTE of finished projects
                    var projectsThisWeekThatAreFinished = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus == ProjectStatus.Finished);
                    var tasksOnFinishedProjectsThisWeek = projectsThisWeekThatAreFinished.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));
                    var totalDemandFinished = (float)tasksOnFinishedProjectsThisWeek.RoundedSum(x => x.Demand);
                    var unmetDemandFinished = (float)tasksOnFinishedProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var metDemandFinished = (float)Math.Round(totalDemandFinished - unmetDemandFinished);


                    /// Confirmed ///

                    // Get just confirmed projects
                    var projectsThisWeekConfirmedActive = projectsThisWeekNotCancelled.Where(x => !x.ProjectStatus.IsUnconfirmed());

                    // Remove finished projects if being shown separately
                    if (showFinishedAsSeparate)
                    {
                        projectsThisWeekConfirmedActive = projectsThisWeekConfirmedActive.Where(x => x.ProjectStatus != ProjectStatus.Finished);
                    }

                    // Get tasks
                    var tasksOnConfirmedActiveProjectsThisWeek = projectsThisWeekConfirmedActive.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));

                    // Get met and unmet demand for this subset
                    var unmetDemandConfirmed = (float)tasksOnConfirmedActiveProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemandConfirmed = (float)tasksOnConfirmedActiveProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemandConfirmed = (float)Math.Round(totalDemandConfirmed - unmetDemandConfirmed);


                    /// Unconfirmed ///

                    // Get just unconfirmed projects
                    var projectsThisWeekUnconfirmedButActive = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus.IsUnconfirmed());

                    // Remove finished projects if being shown separately
                    if (showFinishedAsSeparate)
                    {
                        projectsThisWeekUnconfirmedButActive = projectsThisWeekUnconfirmedButActive.Where(x => x.ProjectStatus != ProjectStatus.Finished);
                    }

                    // Get tasks
                    var tasksOnUnconfirmedActiveProjectsThisWeek = projectsThisWeekUnconfirmedButActive.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));

                    // Calculate the unconfirmed totals
                    var unmetDemandUnconfirmed = (float)tasksOnUnconfirmedActiveProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemandUnconfirmed = (float)tasksOnUnconfirmedActiveProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemandUnconfirmed = (float)Math.Round(totalDemandUnconfirmed - unmetDemandUnconfirmed);


                    /// Costs ///

                    // TODO: Compute the cumulative YTD values


                    /// People ///

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
                                Notes = "Default Model"
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

                    // Get previous item to initialise the next item for the YTD values
                    var previousDemandChartItem = demandChartItems.LastOrDefault();
                    var recoveryYTD = 0f;
                    if (previousDemandChartItem != null)
                    {
                        recoveryYTD = previousDemandChartItem.RecoveryTargetYTD;
                    }
                    recoveryYTD += recoveryTargetPerWeek;


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
                        CancelledDemand = cancelledDemand,
                        FinishedMetDemand = metDemandFinished,
                        FinishedUnmetDemand = unmetDemandFinished,
                        RecoveryTargetYTD = recoveryYTD
                    });

                    // Update averages for quarter for duty chart
                    numberOfWeeks++;
                    var item = dutyChartItems.Last();
                    item.ProjectShortfall = UpdateAverage(item.ProjectShortfall, wlmProject - totalDemand, numberOfWeeks);
                    item.StaffManagementShortfall = UpdateAverage(item.StaffManagementShortfall, wlmStaff - (numStaff - numberOfStaffManagedByHead) * staffManFTE, numberOfWeeks);
                    item.PSManagementShortfall = UpdateAverage(item.PSManagementShortfall, wlmPSM - (projectManFTE * (numberConfirmed + numberUnconfirmed) + appSupportPSMFTE + trainingPSMFTE + otherPSMFTE), numberOfWeeks);
                    item.RSAShortfall = UpdateAverage(item.RSAShortfall, wlmRSA - (numberConfirmed + numberUnconfirmed) * architectureFTE, numberOfWeeks);

                    // Move to next week
                    currentWeekStart = currentWeekStart.AddDays(7);
                }

                // Assign X Labels for duty chart
                dutyChartOptions.Xaxis = new XAxis
                {
                    Categories = dutyXLabels.ToArray()
                };

                // Determine min and max for y axis of duty chart
                dutyChartItems.Last().UpdateMinMax();
                dutyChartOptions.Yaxis = new List<YAxis>
                {
                    new YAxis
                    {
                        Min = dutyChartItems.Min(x => x.Min),
                        Max = dutyChartItems.Max(x => x.Max),
                        Labels = new YAxisLabels
                        {
                            Formatter = @"function (val, index) { return val.toFixed(2); }"
                        },
                        ForceNiceScale = true
                    }
                };

            }).ContinueWith(t =>
            {
                Debug.WriteLine($"** ...generation finished {t.Status}");
                Loading = false;
                InvokeAsync(StateHasChanged);
            });
        }

        /// <summary>
        /// Helper method to compute an average update
        /// </summary>
        /// <param name="oldValue">Reference to the old value you are updating</param>
        /// <param name="newValue">New value to be added to the average</param>
        /// <param name="numberOfWeeks">New number of values in the average collection</param>
        /// <returns></returns>
        internal float UpdateAverage(float oldValue, float newValue, int numberOfWeeks)
        {
            oldValue *= numberOfWeeks - 1;
            oldValue += newValue;
            oldValue /= numberOfWeeks;
            return (float)Math.Round(oldValue, 2);
        }

        /// <summary>
        /// Get list of colours for the charts
        /// </summary>
        /// <returns></returns>
        public List<string> GetColours()
        {
            return !showFinishedAsSeparate ? new List<string>
                {
                    "#9B5DE5",
                    "#7AFF60",
                    "#FEE440",
                    "#FB8F23",
                    "#F44A4A",
                    "#000",
                } : new List<string>
                {
                    "#F15BB5",
                    "#9B5DE5",
                    "#7AFF60",
                    "#FEE440",
                    "#FB8F23",
                    "#F44A4A",
                    "#00F5D4",
                    "#000",
                };
        }
    }
}
