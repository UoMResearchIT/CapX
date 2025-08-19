using System.ComponentModel;
using System.Diagnostics;
using ApexCharts;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Helpers;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;
using Fill = ApexCharts.Fill;

namespace PPMTool.Pages
{
    public partial class DataDashboard : BasePage
    {
        private float personalDevFTE = 0.1f;
        private float architectureFTE = 0.05f;
        private float projectManFTE = GlobalDefaults.ProjectManagementDefaultFTE;
        private float staffManFTE = 0.05f;
        private float coachFTE = 0.1f;

        private int numberOfStaffManagedByHead = 6;
        private DateTime startDate = DateTime.Today;
        private int yearsAhead;
        private bool showFinishedAsSeparate = false;

        private IEnumerable<Person> people;
        private IEnumerable<Project> projects;

        private List<DemandChartItem> demandChartItems = new List<DemandChartItem>();
        private List<DutyChartItem> dutyChartItems = new List<DutyChartItem>();
        private ApexChartOptions<DemandChartItem> demandChartOptions;
        private ApexChartOptions<DemandChartItem> fteChartOptions;
        private ApexChartOptions<DemandChartItem> ytdChartOptions;
        private ApexChartOptions<DutyChartItem> dutyChartOptions;

        private bool exportRunning;
        private ViewOption viewOption;

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private FinancialReferenceService FinancialReferenceService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private InvoiceService InvoiceService { get; set; }

        [Inject]
        private PaymentService PaymentService { get; set; }

        /// <summary>
        /// Different view options on the segmented control
        /// </summary>
        private enum ViewOption
        {
            [Description("Last FY")]
            LastFY,
            [Description("Current FY")]
            CurrentFY,
            [Description("Next FY")]
            NextFY,
            Custom
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                // Get starting lists from the DB
                people = PersonService.GetAll(Context);
                projects = ProjectService.GetAll(Context);

                // Set chart options
                ytdChartOptions = new ApexChartOptions<DemandChartItem>
                {
                    Chart = new Chart
                    {
                        Type = ChartType.Line,
                        Animations = new Animations { Enabled = false },
                        Zoom = new Zoom
                        {
                            AllowMouseWheelZoom = false
                        }
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
                    },
                    Colors = GetColours(ChartColourSet.MoneyChart)
                };

                fteChartOptions = new ApexChartOptions<DemandChartItem>
                {
                    Chart = new Chart
                    {
                        Type = ChartType.Line,
                        Animations = new Animations { Enabled = false },
                        Zoom = new Zoom
                        {
                            AllowMouseWheelZoom = false
                        }
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
                        Animations = new Animations { Enabled = false },
                        Zoom = new Zoom
                        {
                            AllowMouseWheelZoom = false
                        }
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
                        Animations = new Animations { Enabled = false },
                        Zoom = new Zoom
                        {
                            AllowMouseWheelZoom = false
                        }
                    },
                    PlotOptions = new PlotOptions
                    {
                        Bar = new PlotOptionsBar
                        {
                            Horizontal = false
                        }
                    }
                };

                await JSRuntime.InvokeVoidAsync("setFinishedFlag", showFinishedAsSeparate);

                // Set the initial settings and generate the charts
                viewOption = ViewOption.CurrentFY;
                ViewOptionChanged();
            }
        }

        /// <summary>
        /// Callback on "show finished as separate" switch
        /// </summary>
        /// <param name="state"></param>
        private void FinishedChanged(bool state)
        {
            JSRuntime.InvokeVoidAsync("setFinishedFlag", showFinishedAsSeparate);
            Task.Delay(500);
            GenerateCharts();
        }

        /// <summary>
        /// Method to generate the data for all the charts on the page
        /// </summary>
        private void GenerateCharts()
        {
            Loading = true;
            Task.Run(() =>
            {
                Debug.WriteLine("** Starting generation...");

                // Create a thread-local context
                var context = ContextFactory.CreateDbContext();

                // Variable chart options
                demandChartOptions.Fill.Colors = GetColours(ChartColourSet.DutyChart);
                demandChartOptions.Colors = GetColours(ChartColourSet.DutyChart);

                // Clear the existing demand item lists
                demandChartItems.Clear();
                dutyChartItems.Clear();

                // Tracked values
                var currentWeekStart = startDate.Date;
                var currentFY = 0;
                var endDate = startDate.AddYears(yearsAhead);
                var startFY = FinancialReference.GetFinancialYear(startDate);
                var endFY = FinancialReference.GetFinancialYear(endDate);
                int numberOfWeeks = 0;
                List<string> dutyXLabels = new List<string>();
                FinancialReference currentFinRef = FinancialReferenceService.GetFinancialReferenceForDate(Context, startDate);
                float recoveryTargetPerWeek = 0f;
                float proportionOfFY = 0f;

                // For each week
                while (currentWeekStart < endDate)
                {
                    // Initialise
                    float wlmProject = 0f;
                    float wlmBAU = 0f;
                    float wlmPD = 0f;
                    float wlmPM = 0f;
                    float wlmStaff = 0f;
                    float wlmRSA = 0f;
                    float assignmentUnder = 0f;
                    float assignmentOver = 0f;
                    int numStaff = 0;
                    float recoverableStaffCosts = 0f;
                    float recoveryYTD = 0f;

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
                        currentFinRef = FinancialReferenceService.GetFinancialReferenceForDate(Context, currentWeekStart);
                        currentFY = FinancialReference.GetFinancialYear(currentWeekStart);

                        // Compute how many weeks of this FY run within the window of the graph
                        proportionOfFY = FinancialReference.GetProportionOfFinancialYearInRange(currentFY, startDate, endDate);
                        recoveryTargetPerWeek = currentFinRef.RecoveryTarget * proportionOfFY / 52;
                    }

                    // Get the projects that are running at the start of the week (exclude those projects with no tasks as they will have "default" start date)
                    var projectsInDatabaseThisWeek = projects.Where(x => x.StartDate != default && x.IsWithin(currentWeekStart));


                    /// Cancelled ///

                    // Get tasks for cancelled projects running at the start of the week
                    var tasksOnCancelledProjectsThisWeek = projectsInDatabaseThisWeek.Where(x => x.ProjectStatus.IsCancelled()).SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart)));
                    var cancelledDemand = (float)tasksOnCancelledProjectsThisWeek.RoundedSum(x => x.Demand);


                    /// All Projects (not cancelled) ///

                    // Get projects not cancelled
                    var projectsThisWeekNotCancelled = projectsInDatabaseThisWeek.Where(x => !x.ProjectStatus.IsCancelled());

                    // Get number of confirmed and unconfirmed in this subset (including finished)
                    var numberUnconfirmed = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus.IsUnconfirmed()).Count();
                    var numberConfirmed = projectsThisWeekNotCancelled.Count() - numberUnconfirmed;

                    // Get all tasks that run at the start of the week
                    var tasksOnActiveProjectsThisWeek = projectsThisWeekNotCancelled.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart)));

                    // Get demand totals from tasks
                    var unmetDemand = (float)tasksOnActiveProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemand = (float)tasksOnActiveProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemand = (float)Math.Round(totalDemand - unmetDemand);


                    /// Finished ///

                    // Get just total FTE of finished projects
                    var projectsThisWeekThatAreFinished = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus == ProjectStatus.Finished);
                    var tasksOnFinishedProjectsThisWeek = projectsThisWeekThatAreFinished.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart)));
                    var totalDemandFinished = (float)tasksOnFinishedProjectsThisWeek.RoundedSum(x => x.Demand);
                    var unmetDemandFinished = (float)tasksOnFinishedProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var metDemandFinished = (float)Math.Round(totalDemandFinished - unmetDemandFinished);


                    /// Confirmed ///

                    // Get just confirmed projects
                    var projectsThisWeekConfirmed = projectsThisWeekNotCancelled.Where(x => !x.ProjectStatus.IsUnconfirmed());

                    // Remove finished projects if being shown separately
                    if (showFinishedAsSeparate)
                    {
                        projectsThisWeekConfirmed = projectsThisWeekConfirmed.Where(x => x.ProjectStatus != ProjectStatus.Finished);
                    }

                    // Get tasks
                    var tasksOnConfirmedProjectsThisWeek = projectsThisWeekConfirmed.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart)));

                    // Get met and unmet demand for this subset
                    var unmetDemandConfirmed = (float)tasksOnConfirmedProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemandConfirmed = (float)tasksOnConfirmedProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemandConfirmed = (float)Math.Round(totalDemandConfirmed - unmetDemandConfirmed);


                    /// Unconfirmed ///

                    // Get just unconfirmed projects
                    var projectsThisWeekUnconfirmed = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus.IsUnconfirmed());

                    // Remove finished projects if being shown separately
                    if (showFinishedAsSeparate)
                    {
                        projectsThisWeekUnconfirmed = projectsThisWeekUnconfirmed.Where(x => x.ProjectStatus != ProjectStatus.Finished);
                    }

                    // Get tasks
                    var tasksOnUnconfirmedProjectsThisWeek = projectsThisWeekUnconfirmed.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart)));

                    // Calculate the unconfirmed totals
                    var unmetDemandUnconfirmed = (float)tasksOnUnconfirmedProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemandUnconfirmed = (float)tasksOnUnconfirmedProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemandUnconfirmed = (float)Math.Round(totalDemandUnconfirmed - unmetDemandUnconfirmed);


                    /// Costs ///

                    // Get the budget for all confirmed projects this week
                    var budgetYTD = (float)projectsThisWeekConfirmed.Sum(x =>
                    {
                        return x.Budget / (x.EndDate.Subtract(x.StartDate).TotalDays / 7f);
                    });

                    // Get the funds received for all confirmed projects this week
                    var receivedYTD = (float)projectsThisWeekConfirmed.Sum(x =>
                    {
                        return PaymentService.GetFundsReceived(context, x.ProjectId) / (x.EndDate.Subtract(x.StartDate).TotalDays / 7f);
                    });

                    // Get the planned costs for all confirmed projects this week
                    var plannedYTD = (float)projectsThisWeekConfirmed.Sum(x =>
                    {
                        return x.PlannedCost / (x.EndDate.Subtract(x.StartDate).TotalDays / 7f);
                    });

                    // Get the actual costs for all confirmed projects this week
                    var actualYTD = (float)projectsThisWeekConfirmed.Sum(x =>
                    {
                        return x.ActualCost / (x.EndDate.Subtract(x.StartDate).TotalDays / 7f);
                    });

                    // Get the request income for all confirmed projects this week
                    var requestedYTD = (float)projectsThisWeekConfirmed.Sum(x =>
                    {
                        return InvoiceService.GetFundsRequested(context, x.ProjectId) / (x.EndDate.Subtract(x.StartDate).TotalDays / 7f);
                    });

                    /// People ///

                    // Get the people who are employed at the beginning of the week
                    var peopleEmployedThisWeek = people.Where(x => x.StartDate <= currentWeekStart && (x.EndDate == null || x.EndDate >= currentWeekStart));

                    // Compute people based totals
                    foreach (var person in peopleEmployedThisWeek)
                    {
                        // Get active WLM or default G6 model
                        var activeModel = person.GetWorkloadModelOnDateOrDefault(currentWeekStart);

                        // Update totals
                        wlmProject += (float)activeModel.ProjectWorkFTE;
                        wlmBAU += (float)activeModel.BusinessAsUsualFTE;
                        wlmPD += (float)activeModel.PersonalDevelopmentFTE;
                        wlmPM += (float)activeModel.ProjectManagementFTE;
                        wlmStaff += (float)activeModel.StaffManagementFTE;
                        wlmRSA += (float)activeModel.ArchitectureFTE;
                        try
                        {
                            recoverableStaffCosts += (float)currentFinRef.GetMidGradeCosts(activeModel.Grade) * (float)activeModel.ProjectWorkFTE * proportionOfFY / 52;
                        }
                        catch (ArgumentException)
                        {
                            // Skip if the grade is invalid
                            Debug.WriteLine($"** Grade {activeModel.Grade} is invalid!");
                        }
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
                    if (previousDemandChartItem != null)
                    {
                        recoveryYTD = previousDemandChartItem.RecoveryTargetYTD;
                        recoverableStaffCosts += previousDemandChartItem.RecoverableStaffCostsYTD;
                        budgetYTD += previousDemandChartItem.BudgetYTD;
                        receivedYTD += previousDemandChartItem.ReceivedFundsYTD;
                        plannedYTD += previousDemandChartItem.PlannedCostYTD;
                        actualYTD += previousDemandChartItem.ActualCostsYTD;
                        requestedYTD += previousDemandChartItem.RequestedFundsYTD;
                    }
                    recoveryYTD += recoveryTargetPerWeek;

                    // Create a demand item and add it to the list
                    demandChartItems.Add(new DemandChartItem()
                    {
                        WeekStart = currentWeekStart,
                        ProjectFTE = wlmProject,
                        BAUFTE = wlmBAU,
                        PersonalDevFTE = wlmPD,
                        PSMFTE = wlmPM,
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
                        RecoveryTargetYTD = recoveryYTD,
                        BudgetYTD = budgetYTD,
                        ReceivedFundsYTD = receivedYTD,
                        PlannedCostYTD = plannedYTD,
                        ActualCostsYTD = actualYTD,
                        RequestedFundsYTD = requestedYTD,
                        RecoverableStaffCostsYTD = recoverableStaffCosts
                    });

                    // Update averages for quarter for duty chart
                    numberOfWeeks++;
                    var item = dutyChartItems.Last();
                    item.ProjectShortfall = UpdateAverage(item.ProjectShortfall, wlmProject - totalDemand, numberOfWeeks);
                    item.StaffManagementShortfall = UpdateAverage(item.StaffManagementShortfall, wlmStaff - (numStaff - numberOfStaffManagedByHead) * staffManFTE, numberOfWeeks);
                    item.PSManagementShortfall = UpdateAverage(item.PSManagementShortfall, wlmPM - projectManFTE * (numberConfirmed + numberUnconfirmed), numberOfWeeks);
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
        /// Represents the available color sets for different types of charts.
        /// </summary>
        private enum ChartColourSet
        {
            DutyChart,
            MoneyChart
        }

        /// <summary>
        /// Get list of colours for the charts
        /// </summary>
        /// <returns></returns>
        private List<string> GetColours(ChartColourSet chartSet)
        {
            var defaultSet = new List<string>
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

            switch (chartSet)
            {
                case ChartColourSet.DutyChart:
                    return
                        !showFinishedAsSeparate ?
                        new List<string>
                        {
                            "#9B5DE5",
                            "#7AFF60",
                            "#FEE440",
                            "#FB8F23",
                            "#F44A4A",
                            "#000",
                        } : defaultSet;



                case ChartColourSet.MoneyChart:
                    return new List<string>
                    {
                        "#F15BB5",
                        "#9B5DE5",
                        "#7AFF60",
                        "#FEE440",
                        "#FB8F23",
                        "#F44A4A",
                        "#000",
                    };
            }

            return defaultSet;
        }

        /// <summary>
        /// Callback when the view option is changed in the select bar
        /// </summary>
        private void ViewOptionChanged()
        {
            switch (viewOption)
            {
                case ViewOption.LastFY:
                    startDate = new DateTime(FinancialReference.GetFinancialYear(DateTime.Today) - 1, 8, 1);
                    yearsAhead = 1;
                    break;

                case ViewOption.CurrentFY:
                    startDate = new DateTime(FinancialReference.GetFinancialYear(DateTime.Today), 8, 1);
                    yearsAhead = 1;
                    break;

                case ViewOption.NextFY:
                    startDate = new DateTime(FinancialReference.GetFinancialYear(DateTime.Today) + 1, 8, 1);
                    yearsAhead = 1;
                    break;

                case ViewOption.Custom:
                    return;
            }
            GenerateCharts();
        }

        /// <summary>
        /// Callback when the start date is changed through the UI
        /// </summary>
        private void StartDateChanged()
        {
            Debug.WriteLine("** Start Date Changed -- changing to Custom view option");
            viewOption = ViewOption.Custom;
            GenerateCharts();
        }

        /// <summary>
        /// Callback when the years ahead is changed through the UI
        /// </summary>
        private void YearsAheadChanged()
        {
            Debug.WriteLine("** Years Ahead Changed -- changing to Custom view option");
            viewOption = ViewOption.Custom;
            GenerateCharts();
        }

        /// <summary>
        /// Method to export a financial report for Research Finance
        /// </summary>
        private void ExportFinancialReport()
        {
            LogInformation($"Exporting financial report...");

            exportRunning = true;

            Task.Run(async () =>
            {
                // Create a context to be accesed on this thread
                var threadContext = ContextFactory.CreateDbContext();
                var allProjects = ProjectService.GetAll(threadContext);
                var allFinRefs = FinancialReferenceService.GetAll(threadContext);

                // Create blank list of data
                var allData = new List<AssignmentChunk>();

                // Set the report length
                var startDate = new DateTime(FinancialReference.GetFinancialYear(this.startDate), 8, 1);
                var endDate = new DateTime(FinancialReference.GetFinancialYear(this.startDate) + yearsAhead, 7, 31);

                // Get data for each person active in the window
                var peopleActive = people.Where(x => x.StartDate <= endDate && (x.EndDate == null || x.EndDate >= startDate));
                foreach (var person in peopleActive)
                {
                    // Get the row data
                    var data = ExportHelper.GetExportDataForPerson(
                        person,
                        allProjects,
                        startDate,
                        endDate,
                        allFinRefs
                    );
                    allData.AddRange(data);
                }
                allData.Sort((x, y) => x.EmployeeName.CompareTo(y.EmployeeName));

                // Run the file export on the render context
                await InvokeAsync(async () =>
                {
                    try
                    {
                        // Create file path
                        var filename = $"Capacity_{DateTime.Now.Ticks}.xlsx";
                        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapX");
                        Directory.CreateDirectory(folder);
                        var path = Path.Combine(folder, filename);

                        // Create workbook and worksheet
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Capacity");

                            // Write header row
                            var props = typeof(AssignmentChunk).GetProperties();
                            var propNames = props.Select(x => x.Name).ToList();
                            for (int i = 0; i < propNames.Count; i++)
                            {
                                var cell = worksheet.Cell(1, i + 1);
                                cell.Value = propNames[i];
                                cell.Style.Font.Bold = true;
                            }

                            // Write data rows
                            for (int row = 0; row < allData.Count; row++)
                            {
                                var record = allData[row];
                                for (int col = 0; col < propNames.Count; col++)
                                {
                                    var property = record.GetType().GetProperty(propNames[col]);
                                    var rawValue = property?.GetValue(record);
                                    var cell = worksheet.Cell(row + 2, col + 1);

                                    // Format and assign
                                    if (propNames[col] == "StartDate" || propNames[col] == "EndDate")
                                    {
                                        if (rawValue is DateTime dt)
                                        {
                                            cell.Value = dt;
                                            cell.Style.DateFormat.Format = "dd/MM/yyyy";
                                        }
                                        else
                                        {
                                            cell.Value = rawValue?.ToString() ?? string.Empty;
                                        }
                                    }
                                    else if (propNames[col] == "FundingSourceAmount" || propNames[col] == "SalaryCostEstimate" || propNames[col] == "PlannedCost")
                                    {
                                        if (decimal.TryParse(rawValue?.ToString(), out var currencyValue))
                                        {
                                            cell.Value = currencyValue;
                                            cell.Style.NumberFormat.Format = "£#,##0.00";
                                        }
                                        else
                                        {
                                            cell.Value = rawValue?.ToString() ?? string.Empty;
                                        }
                                    }
                                    else
                                    {
                                        if (rawValue is int)
                                        {
                                            cell.Value = (int)rawValue;
                                        }
                                        else if (rawValue is double)
                                        {
                                            cell.Value = (double)rawValue;
                                        }
                                        else
                                        {
                                            cell.Value = rawValue?.ToString() ?? string.Empty;
                                        }
                                    }
                                }
                            }

                            // Save the workbook
                            workbook.SaveAs(path);
                        }

                        Debug.WriteLine($"** Exported {allData.Count} rows to {path}");

                        // Get file stream
                        using var streamRef = new DotNetStreamReference(stream: File.Open(path, FileMode.Open));

                        // Invoke JS on the client to download the file
                        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Could not download file: {ex}");
                    }
                });

            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    LogInformation($"Export task finished {t.Status}");
                    exportRunning = false;
                    StateHasChanged();
                });
            });
        }

        private class RecoveryData
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

            public RecoveryData(DateTime date)
            {
                Date = date;
            }
        }

        /// <summary>
        /// Represents the summary over the window of the target and recovery
        /// </summary>
        private class TotalRecovered
        {
            public string Name { get; }

            public float Target { get; private set; }

            public float Recovered { get; private set; }

            public float RecoveredIncLeadership { get; private set; }

            public float NetCapped { get; private set; }

            public float Net { get; private set; }

            public float NetCappedIncLead { get; private set; }

            public float NetIncLead { get; private set; }

            public float PersonCosts { get; private set; }

            public TotalRecovered(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Update the values based on the FTE for the day
            /// </summary>
            public void Update(float targetFTE, float assignedFTE, float assignedIncLeadFTE, float maxCap, float costs)
            {
                Target += targetFTE;
                Recovered += assignedFTE;
                RecoveredIncLeadership += assignedIncLeadFTE;
                var net = assignedFTE - targetFTE;
                Net += net;
                var netCapped = net > maxCap ? maxCap : net;
                NetCapped += netCapped;
                net = assignedIncLeadFTE - targetFTE;
                NetIncLead += net;
                netCapped = net > maxCap ? maxCap : net;
                NetCappedIncLead += netCapped;
                PersonCosts += costs;
            }

            /// <summary>
            /// Returns the FTE of the target for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetTarget(int daysInWindow)
            {
                return Target / daysInWindow;
            }

            /// <summary>
            /// Returns the FTE of the recovered for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetRecovered(int daysInWindow)
            {
                return Recovered / daysInWindow;
            }

            /// <summary>
            /// Returns the FTE of the recovered including leadership for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetRecoveredIncLeadership(int daysInWindow)
            {
                return RecoveredIncLeadership / daysInWindow;
            }

            /// <summary>
            /// Returns the FTE of the net for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetNet(int daysInWindow)
            {
                return Net / daysInWindow;
            }

            /// <summary>
            /// Returns the FTE of the capped net for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetNetCapped(int daysInWindow)
            {
                return NetCapped / daysInWindow;
            }

            /// <summary>
            /// Returns the FTE of the net including leadership for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetNetIncLeadership(int daysInWindow)
            {
                return NetIncLead / daysInWindow;
            }

            /// <summary>
            /// Returns the FTE of the capped net including leadership for the whole window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetNetCappedIncLeadership(int daysInWindow)
            {
                return NetCappedIncLead / daysInWindow;
            }

            /// <summary>
            /// Gets the costs over the window
            /// </summary>
            /// <param name="daysInWindow"></param>
            /// <returns></returns>
            public float GetCosts(int daysInWindow)
            {
                return PersonCosts / daysInWindow;
            }
        }

        /// <summary>
        /// Exports an Excel spreadsheet of target and assigned recovery of staff
        /// </summary>
        private void ExportRecoveryReport()
        {
            LogInformation($"Exporting recovery report...");

            exportRunning = true;

            Task.Run(async () =>
            {
                // Set the report length
                var startDate = new DateTime(FinancialReference.GetFinancialYear(this.startDate), 8, 1).Date;
                var endDate = new DateTime(FinancialReference.GetFinancialYear(this.startDate) + yearsAhead, 7, 31).Date;
                int totalDays = (int)(endDate.Subtract(startDate).TotalDays + 1);
                var currentDate = startDate;
                var peopleActive = new List<Person>();
                var allData = new List<RecoveryData>();
                var totalData = new List<TotalRecovered>();

                // Create a context to be accesed on this thread
                using (var threadContext = ContextFactory.CreateDbContext())
                {
                    // Get data for each person active in the window
                    var people = await PersonService.GetAllShallowAsync(threadContext);
                    peopleActive = people
                        .Where(x => x.StartDate <= endDate && (x.EndDate == null || x.EndDate >= startDate))
                        .ToList();

                    // Get projects active in the window with their subtasks and resources
                    var projectsInWindow = ProjectService.GetAll(threadContext)
                        .Where(x => !x.ProjectStatus.IsCancelled())
                        .Where(x => x.IsWithin(startDate, endDate));

                    // Normalisation factors for resource FTE based on grade
                    var currentFY = FinancialReference.GetFinancialYear(startDate);
                    var finref = FinancialReferenceService.GetFinancialReferenceForDate(threadContext, startDate);
                    // Initialise the totals
                    foreach (var person in peopleActive)
                    {
                        totalData.Add(new TotalRecovered(person.Name));
                    }

                    // Loop over the days
                    while (currentDate <= endDate)
                    {
                        // If the FY has changed then update the finref
                        if (FinancialReference.GetFinancialYear(currentDate) != currentFY)
                        {
                            finref = FinancialReferenceService.GetFinancialReferenceForDate(threadContext, currentDate);
                        }

                        // Create a new item
                        var currentDayData = new RecoveryData(currentDate);

                        // Get the subtasks that are active on this day
                        var tasksActiveOnDay = projectsInWindow
                            .SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentDate)));

                        // Get the projects that are active on this day
                        var projectsActiveOnDay = projectsInWindow
                            .Where(x => x.SubTasks.Any(x => x.IsWithin(currentDate)));

                        // Loop over each person employed in the window
                        foreach (var person in peopleActive)
                        {
                            // Get the project work amount on the day
                            var projectWorkTargetFTE = person.GetProjectWorkAvailabilityOnDate(currentDate);
                            var wlmTotal = person.GetWorkloadModelTotalOnDate(currentDate);
                            var gradeOnDay = person.GetGradeOnDate(currentDate);

                            // Get day costs for person based on mid-grade
                            var personCosts = gradeOnDay == null ? 0 : finref.GetMidGradeCosts(gradeOnDay ?? 6);

                            // Get resource assignments that are active on the day for this person
                            var resourcesOnDay = tasksActiveOnDay
                                .SelectMany(x => x.AssignedResources)
                                .Where(x => x.Person.PersonId == person.PersonId)
                                .ToList();

                            // Get the projects they manage which have a leadership recovery model
                            var projectsManagedByPerson = projectsActiveOnDay
                                .Where(x =>
                                    x.ProjectManager.PersonId == person.PersonId &&
                                    x.CostModel == CostModel.TechAndLeadership
                                );
                            var leadershipAssignmentFTE = projectsManagedByPerson.Sum(x => x.LeadershipFTE);

                            // Get the sum of their assignments on the day including leadership
                            var projectAssignmentsFTE = resourcesOnDay.Sum(x => x.AssignmentFTE);
                            var projectAssignmentsFTEIncLeadership = projectAssignmentsFTE + leadershipAssignmentFTE;

                            // Net value
                            var netValue = projectAssignmentsFTE - projectWorkTargetFTE;
                            var netValueIncLeadership = projectAssignmentsFTEIncLeadership - projectWorkTargetFTE;

                            // Net value capped
                            var maxOverAllocation = wlmTotal - projectWorkTargetFTE;
                            if (maxOverAllocation < 0) maxOverAllocation = 0;
                            var netValueCapped = netValue > maxOverAllocation ? maxOverAllocation : netValue;
                            var netValueCappedIncLeadership = netValueIncLeadership > maxOverAllocation ? maxOverAllocation : netValueIncLeadership;

                            // Add to the data dictionary
                            currentDayData.TargetRecovery.Add(person.Name, (float)projectWorkTargetFTE);
                            currentDayData.RecoveredTime.Add(person.Name, (float)projectAssignmentsFTE);
                            currentDayData.Net.Add(person.Name, (float)netValue);
                            currentDayData.NetCapped.Add(person.Name, (float)netValueCapped);
                            currentDayData.RecoveredTimeIncLeadership.Add(person.Name, (float)projectAssignmentsFTEIncLeadership);
                            currentDayData.NetIncLeadership.Add(person.Name, (float)netValueIncLeadership);
                            currentDayData.NetCappedIncLeadership.Add(person.Name, (float)netValueCappedIncLeadership);
                            currentDayData.PersonCosts.Add(person.Name, (float)personCosts);

                            // Update the totals
                            totalData
                                .First(x => x.Name == person.Name)
                                .Update(
                                    (float)projectWorkTargetFTE,
                                    (float)projectAssignmentsFTE,
                                    (float)projectAssignmentsFTEIncLeadership,
                                    (float)maxOverAllocation,
                                    (float)personCosts
                                );
                        }

                        // Add the current day data to the list
                        allData.Add(currentDayData);

                        // Advance the day
                        currentDate = currentDate.AddDays(1);
                    }
                }

                // Run the file export on the render context
                await InvokeAsync(async () =>
                {
                    try
                    {
                        // Create file path
                        var filename = $"Recovery_{DateTime.Now.Ticks}.xlsx";
                        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapX");
                        Directory.CreateDirectory(folder);
                        var path = Path.Combine(folder, filename);

                        // Create workbook and worksheet
                        using (var workbook = new XLWorkbook())
                        {
                            // Get a list of people active by name
                            var peopleActiveNames = peopleActive.Select(x => x.Name).ToList();
                            var totalPeople = peopleActiveNames.Count();

                            // Tab titles
                            var tabTitles = new List<string>
                            {
                                "Target",
                                "Recovered",
                                "Net (Uncapped)",
                                "Net (Capped)",
                                "Recovered (Inc Lead)",
                                "Net (Uncapped, Inc Lead)",
                                "Net (Capped, Inc Lead)",
                                "Costs"
                            };

                            for (int j = 0; j < tabTitles.Count; ++j)
                            {
                                var worksheet = workbook.Worksheets.Add(tabTitles[j]);

                                // Write header row
                                var cell = worksheet.Cell(1, 1);
                                cell.Value = "Date";
                                cell.Style.Font.Bold = true;

                                // Write the names of the people in the header row
                                for (int i = 0; i < totalPeople; i++)
                                {
                                    cell = worksheet.Cell(1, i + 2);
                                    cell.Value = peopleActiveNames[i];
                                    cell.Style.Font.Bold = true;
                                    cell.Style.Alignment.TextRotation = 90;
                                }

                                // Write data rows
                                for (int row = 0; row < allData.Count; row++)
                                {
                                    // Date
                                    cell = worksheet.Cell(row + 2, 1);
                                    cell.Value = allData[row].Date.ToString("dd/MM/yyyy");
                                    cell.Style.DateFormat.Format = "dd/MM/yyyy";

                                    // Each person
                                    for (int i = 0; i < totalPeople; i++)
                                    {
                                        // Get the cell
                                        cell = worksheet.Cell(row + 2, i + 2);
                                        float cellValue = 0f;

                                        // Get the dictionary entry
                                        switch (j)
                                        {
                                            case 0: // Target
                                                allData[row].TargetRecovery.TryGetValue(peopleActiveNames[i], out cellValue);
                                                break;
                                            case 1: // Recovered
                                                allData[row].RecoveredTime.TryGetValue(peopleActiveNames[i], out cellValue);
                                                break;
                                            case 2: // Net (Uncapped)
                                                allData[row].Net.TryGetValue(peopleActiveNames[i], out cellValue);
                                                break;
                                            case 3: // Net (Capped)
                                                allData[row].NetCapped.TryGetValue(peopleActiveNames[i], out cellValue);
                                                break;
                                            case 4: // Recovered Inc Lead
                                                allData[row].RecoveredTimeIncLeadership.TryGetValue(peopleActiveNames[i], out cellValue);
                                                break;
                                            case 5: // Net (Uncapped, Inc Lead)
                                                allData[row].NetIncLeadership.TryGetValue(peopleActiveNames[i], out cellValue);
                                                break;
                                            case 6: // Net (Capped, Inc Lead)
                                                allData[row].NetCappedIncLeadership.TryGetValue(peopleActiveNames[i], out cellValue);
                                                break;
                                            case 7: // Costs
                                                allData[row].PersonCosts.TryGetValue(peopleActiveNames[i], out cellValue);
                                                break;
                                        }

                                        // Assign the value
                                        cell.Value = cellValue;
                                        cell.Style.NumberFormat.Format = j == 7 ? "£0.00" : "#0.000";
                                    }
                                }
                            }

                            // Add totals tab
                            var worksheetTotals = workbook.AddWorksheet("Totals", 0);

                            // Header row
                            var cellTotals = worksheetTotals.Cell(1, 1);
                            cellTotals.Value = "Name";
                            cellTotals.Style.Font.Bold = true;
                            for (int i = 0; i < tabTitles.Count; ++i)
                            {
                                cellTotals = worksheetTotals.Cell(1, 2 + i);
                                cellTotals.Value = tabTitles[i];
                                cellTotals.Style.Font.Bold = true;
                            }

                            // Add additional baseline costs columns
                            cellTotals = worksheetTotals.Cell(1, 2 + tabTitles.Count);
                            cellTotals.Value = "Extra Baseline";
                            cellTotals.Style.Font.Bold = true;
                            cellTotals = worksheetTotals.Cell(1, 3 + tabTitles.Count);
                            cellTotals.Value = "Extra Baseline (Inc. Leadership)";
                            cellTotals.Style.Font.Bold = true;

                            // Each row
                            for (int i = 0; i < peopleActiveNames.Count; ++i)
                            {
                                var totalItem = totalData.First(x => x.Name == peopleActiveNames[i]);

                                cellTotals = worksheetTotals.Cell(2 + i, 1);
                                cellTotals.Value = peopleActiveNames[i];
                                cellTotals.Style.Font.Bold = true;

                                cellTotals = worksheetTotals.Cell(2 + i, 2);
                                cellTotals.Value = totalItem.GetTarget(totalDays);
                                cellTotals.Style.NumberFormat.Format = "#0.000";
                                cellTotals = worksheetTotals.Cell(2 + i, 3);
                                cellTotals.Value = totalItem.GetRecovered(totalDays);
                                cellTotals.Style.NumberFormat.Format = "#0.000";
                                cellTotals = worksheetTotals.Cell(2 + i, 4);
                                cellTotals.Value = totalItem.GetNet(totalDays);
                                cellTotals.Style.NumberFormat.Format = "#0.000";
                                cellTotals = worksheetTotals.Cell(2 + i, 5);
                                var netCapped = totalItem.GetNetCapped(totalDays);
                                cellTotals.Value = netCapped;
                                cellTotals.Style.NumberFormat.Format = "#0.000";
                                cellTotals = worksheetTotals.Cell(2 + i, 6);
                                cellTotals.Value = totalItem.GetRecoveredIncLeadership(totalDays);
                                cellTotals.Style.NumberFormat.Format = "#0.000";
                                cellTotals = worksheetTotals.Cell(2 + i, 7);
                                cellTotals.Value = totalItem.GetNetIncLeadership(totalDays);
                                cellTotals.Style.NumberFormat.Format = "#0.000";
                                cellTotals = worksheetTotals.Cell(2 + i, 8);
                                var netCappedLeadership = totalItem.GetNetCappedIncLeadership(totalDays);
                                cellTotals.Value = netCappedLeadership;
                                cellTotals.Style.NumberFormat.Format = "#0.000";
                                cellTotals = worksheetTotals.Cell(2 + i, 9);
                                cellTotals.Value = totalItem.GetCosts(totalDays);
                                cellTotals.Style.NumberFormat.Format = "£0.00";

                                // Extra costs
                                cellTotals = worksheetTotals.Cell(2 + i, 10);
                                cellTotals.Value = netCapped < 0 ? -netCapped * totalItem.GetCosts(totalDays) : 0;
                                cellTotals.Style.NumberFormat.Format = "£0.00";
                                cellTotals = worksheetTotals.Cell(2 + i, 11);
                                cellTotals.Value = netCappedLeadership < 0 ? -netCappedLeadership * totalItem.GetCosts(totalDays) : 0;
                                cellTotals.Style.NumberFormat.Format = "£0.00";
                            }

                            // Save the workbook
                            workbook.SaveAs(path);
                        }

                        Debug.WriteLine($"** Exported {allData.Count} rows to {path}");

                        // Get file stream
                        using var streamRef = new DotNetStreamReference(stream: File.Open(path, FileMode.Open));

                        // Invoke JS on the client to download the file
                        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Could not download file: {ex}");
                    }
                });

            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    LogInformation($"Export task finished {t.Status}");
                    exportRunning = false;
                    StateHasChanged();
                });
            });
        }
    }
}
