// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;
using System.Diagnostics;
using ApexCharts;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Data.Helpers;
using PPMTool.Helpers;
using PPMTool.Models;
using PPMTool.Services;
using Radzen;
using static PPMTool.Helpers.ExportHelper;
using Fill = ApexCharts.Fill;

namespace PPMTool.Pages
{
    public partial class DataDashboard : BasePage
    {
        private DateTime startDate = DateTime.Today;
        private int monthsAhead;
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
                var endDate = startDate.Date.AddMonths(monthsAhead);
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


                    // Cancelled //

                    // Get tasks for cancelled projects running at the start of the week
                    var tasksOnCancelledProjectsThisWeek = projectsInDatabaseThisWeek
                        .Where(x => x.ProjectStatus.IsCancelled())
                            .SelectMany(x => x.SubTasks
                                .Where(x => !x.IsLeadershipTask && x.IsWithin(currentWeekStart)
                            )
                        );
                    var cancelledDemand = (float)tasksOnCancelledProjectsThisWeek.RoundedSum(x => x.Demand);


                    // All Projects (not cancelled) //

                    // Get projects not cancelled
                    var projectsThisWeekNotCancelled = projectsInDatabaseThisWeek.Where(x => !x.ProjectStatus.IsCancelled());

                    // Get number of confirmed and unconfirmed in this subset (including finished)
                    var numberUnconfirmed = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus.IsUnconfirmed()).Count();
                    var numberConfirmed = projectsThisWeekNotCancelled.Count() - numberUnconfirmed;

                    // Get all (technical only) tasks that run at the start of the week
                    var tasksOnActiveProjectsThisWeek = projectsThisWeekNotCancelled
                        .SelectMany(x => x.SubTasks
                            .Where(x => !x.IsLeadershipTask && x.IsWithin(currentWeekStart))
                        );

                    // Get demand totals from tasks
                    var unmetDemand = (float)tasksOnActiveProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemand = (float)tasksOnActiveProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemand = (float)Math.Round(totalDemand - unmetDemand);

                    // Get all (leadership only) tasks that run at the start of the week
                    var leadershipTasksOnActiveProjectsThisWeek = projectsThisWeekNotCancelled
                        .SelectMany(x => x.SubTasks
                            .Where(x => x.IsLeadershipTask && x.IsWithin(currentWeekStart))
                        );

                    // Get demand for leadership
                    var leadershipDemand = (float)leadershipTasksOnActiveProjectsThisWeek.RoundedSum(x => x.Demand);

                    // Finished //

                    // Get just total FTE of finished projects
                    var projectsThisWeekThatAreFinished = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus == ProjectStatus.Finished);
                    var tasksOnFinishedProjectsThisWeek = projectsThisWeekThatAreFinished.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart)));
                    var totalDemandFinished = (float)tasksOnFinishedProjectsThisWeek.RoundedSum(x => x.Demand);
                    var unmetDemandFinished = (float)tasksOnFinishedProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var metDemandFinished = (float)Math.Round(totalDemandFinished - unmetDemandFinished);


                    // Confirmed //

                    // Get just confirmed projects
                    var projectsThisWeekConfirmed = projectsThisWeekNotCancelled.Where(x => !x.ProjectStatus.IsUnconfirmed());

                    // Remove finished projects if being shown separately
                    if (showFinishedAsSeparate)
                    {
                        projectsThisWeekConfirmed = projectsThisWeekConfirmed.Where(x => x.ProjectStatus != ProjectStatus.Finished);
                    }

                    // Get tasks (excluding leadership tasks)
                    var tasksOnConfirmedProjectsThisWeek = projectsThisWeekConfirmed
                        .SelectMany(x => x.SubTasks
                            .Where(x => !x.IsLeadershipTask && x.IsWithin(currentWeekStart))
                        );

                    // Get met and unmet demand for this subset
                    var unmetDemandConfirmed = (float)tasksOnConfirmedProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemandConfirmed = (float)tasksOnConfirmedProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemandConfirmed = (float)Math.Round(totalDemandConfirmed - unmetDemandConfirmed);


                    // Unconfirmed //

                    // Get just unconfirmed projects
                    var projectsThisWeekUnconfirmed = projectsThisWeekNotCancelled.Where(x => x.ProjectStatus.IsUnconfirmed());

                    // Remove finished projects if being shown separately
                    if (showFinishedAsSeparate)
                    {
                        projectsThisWeekUnconfirmed = projectsThisWeekUnconfirmed.Where(x => x.ProjectStatus != ProjectStatus.Finished);
                    }

                    // Get tasks (excluding leadership tasks)
                    var tasksOnUnconfirmedProjectsThisWeek = projectsThisWeekUnconfirmed
                        .SelectMany(x => x.SubTasks
                            .Where(x => !x.IsLeadershipTask && x.IsWithin(currentWeekStart))
                        );

                    // Calculate the unconfirmed totals
                    var unmetDemandUnconfirmed = (float)tasksOnUnconfirmedProjectsThisWeek.RoundedSum(x => x.UnmetDemand);
                    var totalDemandUnconfirmed = (float)tasksOnUnconfirmedProjectsThisWeek.RoundedSum(x => x.Demand);
                    var metDemandUnconfirmed = (float)Math.Round(totalDemandUnconfirmed - unmetDemandUnconfirmed);


                    // Costs //

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

                    // People //

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

                        // Get (technical only) assignments for this person and sum for the week
                        var assignmentsThisWeek = tasksOnActiveProjectsThisWeek
                            .SelectMany(x => x.AssignedResources
                                .Where(x => x.Person.PersonId == person.PersonId)
                            );
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
                        NumberStaffRequiringLineManagement = numStaff - GetSetting(SettingType.NumberOfStaffManagedByHeadDefault, 0),
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
                        LeadershipDemand = leadershipDemand,
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
                    item.StaffManagementShortfall = UpdateAverage(
                        item.StaffManagementShortfall,
                        wlmStaff - (numStaff - GetSetting(SettingType.NumberOfStaffManagedByHeadDefault, 0)) * GetSetting(SettingType.StaffManagementDefaultFTE, 0f),
                        numberOfWeeks
                    );
                    item.PSManagementShortfall = UpdateAverage(item.PSManagementShortfall, wlmPM - leadershipDemand, numberOfWeeks);
                    item.RSAShortfall = UpdateAverage(item.RSAShortfall, wlmRSA - (numberConfirmed + numberUnconfirmed) * GetSetting(SettingType.TechnicalLeadershipDefaultFTE, 0f), numberOfWeeks);

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
                    monthsAhead = 12;
                    break;

                case ViewOption.CurrentFY:
                    startDate = new DateTime(FinancialReference.GetFinancialYear(DateTime.Today), 8, 1);
                    monthsAhead = 12;
                    break;

                case ViewOption.NextFY:
                    startDate = new DateTime(FinancialReference.GetFinancialYear(DateTime.Today) + 1, 8, 1);
                    monthsAhead = 12;
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
        /// Callback when the months ahead is changed through the UI
        /// </summary>
        private void MonthsAheadChanged()
        {
            Debug.WriteLine("** Months Ahead Changed -- changing to Custom view option");
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
                using (var context = ContextFactory.CreateDbContext())
                {
                    try
                    {
                        var allProjects = ProjectService.GetAll(context);
                        var realProjects = allProjects
                            .Where(x => !x.ProjectStatus.IsCancelled());
                        var allFinRefs = FinancialReferenceService.GetAll(context);

                        // Create blank list of data
                        var assignmentChunks = new List<AssignmentChunk>();

                        // Set the report length
                        var startDate = this.startDate.Date;
                        var endDate = this.startDate.Date.AddMonths(monthsAhead).AddDays(-1);

                        // Filter list of projects to those running during the window
                        var projectsInWindow = realProjects
                            .Where(x => x.IsWithin(startDate, endDate));
                        Debug.WriteLine($"** {projectsInWindow.Count()} projects running during the window.");

                        // Get the breakdown of budget details for the tasks/resources in the projects we care about
                        var projectBudgetDetails = FinanceHelper.GetProjectBudgetDetail(projects);
                        Debug.WriteLine($"** Built {projectBudgetDetails.Count()} budget details.");

                        // Get data for each person active in the window
                        var peopleActive = await PersonService.GetEmployedPeopleShallowAsync(Context, startDate, endDate);
                        foreach (var person in peopleActive)
                        {
                            // Filter list of tasks for those projects that just run during the window and are assigned to this person.
                            // Filter out leadership tasks for projects that don't allow them to be recharged.
                            var tasksInWindow = projectsInWindow
                                .SelectMany(x => x.SubTasks)
                                .Where(x => x.AssignedResources
                                    .Any(x => x.Person.PersonId == person.PersonId) &&
                                    x.IsWithin(startDate, endDate) &&
                                    (!x.IsLeadershipTask ||
                                        (x.OwningProject.CostModel.HasLeadership() && x.IsLeadershipTask)
                                    )
                                );
                            Debug.WriteLine($"** {tasksInWindow.Count()} tasks within window for {person.Name}");

                            // Represent the assignments (including leadership assignments if cost model allows recharge) in the window as chunks.
                            // Do not recompute the costs here as it is a waste of effort.
                            var data = AssignmentHelper.GetAssignmentChunks(
                                person,
                                projectsInWindow,
                                allFinRefs,
                                startDate,
                                endDate,
                                tasksInWindow,
                                budgetDetails: projectBudgetDetails);

                            Debug.WriteLine($"** Built {data.Count()} rows for {person.Name}");
                            assignmentChunks.AddRange(data);
                        }
                        assignmentChunks.Sort((x, y) => x.EmployeeName.CompareTo(y.EmployeeName));
                        Debug.WriteLine($"** {assignmentChunks.Count()} assignment entries generated!");

                        // Get recovery data for assignment chunks
                        int totalDaysInReportingWindow = (int)(endDate.Subtract(startDate).TotalDays + 1);
                        var totalData = ExportHelper.GetRecoveryData(
                            peopleActive,
                            assignmentChunks,
                            ContextFactory,
                            PersonService,
                            ProjectService,
                            FinancialReferenceService
                        );
                        Debug.WriteLine($"** {totalData.Count()} recovery summary entries generated!");

                        // Generate data for project summary
                        var projectData = new List<ProjectBudgetSummary>();
                        foreach (var assignment in assignmentChunks)
                        {
                            // Find existing entry or add new
                            var summary = projectData.FirstOrDefault(x => x.ProjectId == assignment.ProjectId.ToString());
                            if (summary == null)
                            {
                                summary = new ProjectBudgetSummary
                                {
                                    ProjectId = assignment.ProjectId.ToString(),
                                    ProjectName = assignment.ProjectName
                                };
                                projectData.Add(summary);
                            }

                            // Add the values
                            summary.PlannedCosts += assignment.PlannedCost;
                            summary.RecoveredCosts += assignment.AmountCovered;
                        }

                        // Create file path
                        var filename = $"Recovery_{DateTime.Now.Ticks}.xlsx";
                        var path = FileHelper.GetLocalApplicationFilePath(filename);

                        // Create workbook and worksheet
                        using (var workbook = new XLWorkbook())
                        {
                            // **** Blank Posts Worksheet **** //
                            var worksheet = workbook.Worksheets.Add("Posts");
                            worksheet.SheetView.FreezeRows(1);

                            // **** Assignments Worksheet **** //

                            // Assignments worksheet first
                            worksheet = workbook.Worksheets.Add("Assignments", 0);
                            worksheet.SheetView.FreezeRows(1);

                            // Get properties and reorder the end date so it comes after the start date
                            var props = typeof(AssignmentChunk).GetProperties().ToList();
                            var startDateProp = props.FirstOrDefault(p => p.Name == nameof(AssignmentChunk.StartDate));
                            var endDateProp = props.FirstOrDefault(p => p.Name == nameof(AssignmentChunk.EndDate));
                            props.Remove(endDateProp);
                            if (startDateProp != null && endDateProp != null)
                            {
                                var startIndex = props.IndexOf(startDateProp);
                                props.Insert(startIndex + 1, endDateProp);
                            }

                            // Write header row
                            IXLCell cell = default;
                            IXLComment comment = default;

                            // The SPOT ID column is unique
                            cell = worksheet.Cell(1, 1);
                            cell.Value = "SPOT ID";
                            cell.Style.Font.Bold = true;

                            // Other headers
                            for (int col = 0; col < props.Count(); col++)
                            {
                                var prop = props[col];
                                cell = worksheet.Cell(1, col + 2);
                                cell.Value = prop.Name;
                                cell.Style.Font.Bold = true;

                                var attributes = prop.GetCustomAttributes(false);
                                var descriptionAttr = attributes.FirstOrDefault(x => x.GetType() == typeof(DescriptionAttribute));
                                if (descriptionAttr != null)
                                {
                                    var description = (descriptionAttr as DescriptionAttribute).Description;
                                    comment = cell.CreateComment();
                                    comment.AddText(description);
                                }
                            }

                            // Write data rows
                            for (int row = 0; row < assignmentChunks.Count; row++)
                            {
                                var record = assignmentChunks[row];

                                // The SPOT ID column is unique
                                cell = worksheet.Cell(row + 2, 1);
                                cell.FormulaR1C1 = "=VLOOKUP(RC[1],Posts!R2C1:R60C5,4,FALSE)";
                                cell.Style.NumberFormat.SetFormat("@");

                                // Rest of the data
                                for (int col = 0; col < props.Count(); col++)
                                {
                                    var propName = props[col].Name;
                                    var property = record.GetType().GetProperty(propName);
                                    var rawValue = property?.GetValue(record);
                                    cell = worksheet.Cell(row + 2, col + 2);

                                    // Format and assign
                                    if (propName == nameof(AssignmentChunk.StartDate) || propName == nameof(AssignmentChunk.EndDate))
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
                                    else if (propName == nameof(AssignmentChunk.AmountCovered) ||
                                        propName == nameof(AssignmentChunk.SalaryCostEstimate) ||
                                        propName == nameof(AssignmentChunk.PlannedCost))
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
                                            cell.Value = Math.Round((double)rawValue, 3);
                                        }
                                        else
                                        {
                                            cell.Value = rawValue?.ToString() ?? string.Empty;
                                        }
                                    }
                                }
                            }

                            // Adjust the column widths
                            worksheet.Columns().AdjustToContents();

                            // **** Costs Worksheet **** //

                            // Get a list of people active by name
                            var totalPeople = peopleActive.Count;
                            var columnTitles = new List<string>
                            {
                                "Estimated Costs (Mid-Grade)",
                                "Actual Costs (Tracker)",
                                "Estimate Error",
                                "Project Work FTE",
                                "Staff Mgmt FTE",
                                "Project Mgmt FTE",
                                "Service Mgmt FTE",
                                "BAU FTE",
                                "Personal Development FTE",
                                "Tech Leadership FTE",
                                "Project Work Costs",
                                "Staff Mgmt Costs",
                                "Project Mgmt Costs",
                                "Service Mgmt Costs",
                                "BAU Costs",
                                "Personal Development Costs",
                                "Tech Leadership Costs",
                                "Estimated Baseline",
                                "Assigned FTE",
                                "Assigned Costs",
                                "In Budget Costs",
                                "Actual Baseline",
                                "Baseline Surplus"
                            };

                            var columnComments = new List<string>
                            {
                                "These are the costs of the person over the reporting period based on mid-grade estimates.",
                                "These are the actual costs of the person over the reporting period based on finance tracker data (including PCM costs).",
                                "This is the difference between estimated and actual costs.",
                                "This is the average technical target recovery FTE for the person over the reporting period based on their workload model.",
                                "This is the average staff management FTE for the person over the reporting period based on their workload model.",
                                "This is the average project management FTE for the person over the reporting period based on their workload model.",
                                "This is the average service management FTE for the person over the reporting period based on their workload model.",
                                "This is the average business-as-usual FTE for the person over the reporting period based on their workload model.",
                                "This is the average personal development FTE for the person over the reporting period based on their workload model.",
                                "This is the average technical leadership FTE for the person over the reporting period based on their workload model.",
                                "These are the technical target recovery costs of the person over the reporting period based on mid-grade estimates.",
                                "These are the staff management costs of the person over the reporting period based on mid-grade estimates.",
                                "These are the project management costs of the person over the reporting period based on mid-grade estimates.",
                                "These are the service management costs of the person over the reporting period based on mid-grade estimates.",
                                "These are the business-as-usual costs of the person over the reporting period based on mid-grade estimates.",
                                "These are the personal development costs of the person over the reporting period based on mid-grade estimates.",
                                "These are the technical leadership costs of the person over the reporting period based on mid-grade estimates.",
                                "This is the required baseline budget for the person over the reporting period (estimated costs - project costs).",
                                "This is the average recovered FTE including leadership assignments (that we can recharge) for the person over the reporting period based on their assignments",
                                "These are the recovered costs of the person including leadership assignments over the reporting period based on mid-grade estimates.",
                                "These are the costs that can be covered by known research funding sources for all assignments (technical and leadership).",
                                "This the actual baseline budget required for the person based on their technical and leadership assignments and what we believe is available in research funding to cover them.",
                                "The difference between the baseline budget required by their workload model and what is actually required based on predicted recharge."
                            };

                            // Add tab
                            var worksheetTotals = workbook.AddWorksheet("Costs", 0);
                            worksheetTotals.SheetView.FreezeRows(1);

                            // Header row
                            cell = worksheetTotals.Cell(1, 1);
                            cell.Value = "SPOT ID";
                            cell.Style.Font.Bold = true;
                            cell = worksheetTotals.Cell(1, 2);
                            cell.Value = "Name";
                            cell.Style.Font.Bold = true;
                            for (int i = 0; i < columnTitles.Count; ++i)
                            {
                                cell = worksheetTotals.Cell(1, 3 + i);
                                cell.Value = columnTitles[i];
                                cell.Style.Font.Bold = true;
                                comment = cell.CreateComment();
                                comment.Author = "CapX Exporter";
                                comment.AddText(columnComments[i]);
                            }

                            string moneyFormat = "_-£* #,##0.00_-;[Red]-£* #,##0.00_-;_-£* \"-\"??_-;_-@_-";
                            string numberFormat = "0.000_ ;[Red]-0.000";

                            // Each row
                            for (int i = 0; i < peopleActive.Count; ++i)
                            {
                                // Adjust the start and end dates of the average period if necessary
                                var person = peopleActive[i];
                                var totalItem = totalData.First(x => x.Name == person.Name);
                                var adjustedStart = startDate;
                                var adjustedEnd = endDate;

                                if (person.EndDate != null && person.EndDate < endDate)
                                {
                                    adjustedEnd = person.EndDate.Value;
                                }
                                if (person.StartDate > startDate)
                                {
                                    adjustedStart = person.StartDate;
                                }
                                int averagePeriod = (int)(adjustedEnd.Subtract(adjustedStart).TotalDays + 1);

                                // SPOT ID
                                cell = worksheetTotals.Cell(2 + i, 1);
                                cell.FormulaR1C1 = "=VLOOKUP(RC[1],Posts!R2C1:R60C5,4,FALSE)";
                                cell.Style.Font.Bold = true;
                                cell.Style.NumberFormat.SetFormat("@");

                                // Name
                                cell = worksheetTotals.Cell(2 + i, 2);
                                cell.Value = person.Name;
                                cell.Style.Font.Bold = true;

                                // Costs of person over window
                                var windowCosts = totalItem.GetEstimatedCosts();
                                cell = worksheetTotals.Cell(2 + i, 3);
                                cell.Value = windowCosts;
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Leave next column blank
                                // Actual costs filled in manually from finance tracker
                                cell = worksheetTotals.Cell(2 + i, 4);
                                cell.FormulaR1C1 = "=VLOOKUP(RC[-2],Posts!R2C1:R60C5,5,FALSE)";
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Variance is formula
                                cell = worksheetTotals.Cell(2 + i, 5);
                                cell.FormulaR1C1 = "=RC[-2]-RC[-1]";
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Target FTE
                                cell = worksheetTotals.Cell(2 + i, 6);
                                cell.Value = totalItem.GetAverageProjectWorkTarget(averagePeriod);
                                cell.Style.NumberFormat.Format = numberFormat;

                                // Staff management FTE
                                cell = worksheetTotals.Cell(2 + i, 7);
                                cell.Value = totalItem.GetAverageStaffFTE(averagePeriod);
                                cell.Style.NumberFormat.Format = numberFormat;

                                // Project management FTE
                                cell = worksheetTotals.Cell(2 + i, 8);
                                cell.Value = totalItem.GetAverageProjectManagementFTE(averagePeriod);
                                cell.Style.NumberFormat.Format = numberFormat;

                                // Service management FTE
                                cell = worksheetTotals.Cell(2 + i, 9);
                                cell.Value = totalItem.GetAverageServiceManagementFTE(averagePeriod);
                                cell.Style.NumberFormat.Format = numberFormat;

                                // BAU FTE
                                cell = worksheetTotals.Cell(2 + i, 10);
                                cell.Value = totalItem.GetAverageBAUFTE(averagePeriod);
                                cell.Style.NumberFormat.Format = numberFormat;

                                // PD FTE
                                cell = worksheetTotals.Cell(2 + i, 11);
                                cell.Value = totalItem.GetAveragePersonalDevelopmentFTE(averagePeriod);
                                cell.Style.NumberFormat.Format = numberFormat;

                                // Tech Leadership FTE
                                cell = worksheetTotals.Cell(2 + i, 12);
                                cell.Value = totalItem.GetAverageTechLeadershipFTE(averagePeriod);
                                cell.Style.NumberFormat.Format = numberFormat;

                                // Project Costs
                                cell = worksheetTotals.Cell(2 + i, 13);
                                var targetCosts = totalItem.GetAverageProjectWorkTargetCosts();
                                cell.Value = targetCosts;
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Staff management costs
                                cell = worksheetTotals.Cell(2 + i, 14);
                                cell.Value = totalItem.GetAverageStaffMgmtCosts();
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Project management costs
                                cell = worksheetTotals.Cell(2 + i, 15);
                                cell.Value = totalItem.GetAverageProjectManagementCosts();
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Service management costs
                                cell = worksheetTotals.Cell(2 + i, 16);
                                cell.Value = totalItem.GetAverageServiceManagementCosts();
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // BAU costs
                                cell = worksheetTotals.Cell(2 + i, 17);
                                cell.Value = totalItem.GetAverageBAUFTECosts();
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // PD costs
                                cell = worksheetTotals.Cell(2 + i, 18);
                                cell.Value = totalItem.GetAveragePersonalDevelopmentFTECosts();
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Tech Leadership costs
                                cell = worksheetTotals.Cell(2 + i, 19);
                                cell.Value = totalItem.GetAverageTechLeadershipFTECosts();
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Baseline Budget
                                cell = worksheetTotals.Cell(2 + i, 20);
                                cell.FormulaR1C1 = "=RC[-17]-RC[-7]";
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Recovered Inc Lead
                                cell = worksheetTotals.Cell(2 + i, 21);
                                cell.Value = totalItem.GetAverageRecoveredIncLeadership(averagePeriod);
                                cell.Style.NumberFormat.Format = numberFormat;

                                // Recovered Inc Lead Costs
                                cell = worksheetTotals.Cell(2 + i, 22);
                                cell.Value = totalItem.GetAverageRecoveredIncLeadershipCosts();
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Amount in budget
                                cell = worksheetTotals.Cell(2 + i, 23);
                                cell.Value = totalItem.GetInBudgetTotals();
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Baseline
                                cell = worksheetTotals.Cell(2 + i, 24);
                                // = C - Q, both on the same row (relative R1C1: no anchors)
                                // Clamp to positive and don't want a negative baseline for a post
                                cell.FormulaR1C1 = "=MAX(RC[-21]-RC[-1], 0)";
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Difference to baseline
                                cell = worksheetTotals.Cell(2 + i, 25);
                                // = H - baseline, same row (relative R1C1: no anchors)
                                cell.FormulaR1C1 = "=RC[-5]-RC[-1]";
                                cell.Style.NumberFormat.Format = moneyFormat;
                            }

                            // Add total row (leave blank row)
                            var totalRow = peopleActive.Count + 3;
                            for (var col = 0; col < columnTitles.Count; ++col)
                            {
                                var totalCell = worksheetTotals.Cell(totalRow, col + 3);

                                // SUM from row 2 in this column to the row above the gap before the total row
                                totalCell.FormulaR1C1 = "=SUM(R2C:R[-2]C)";

                                // Copy formatting
                                var cellAbove = worksheetTotals.Cell(totalRow - 2, col + 3);
                                totalCell.Style.NumberFormat.Format = cellAbove.Style.NumberFormat.Format;
                                totalCell.Style.Font.Bold = true;
                            }

                            // Adjust the column widths
                            worksheetTotals.Columns().AdjustToContents();

                            // Group the WLM details
                            worksheetTotals.Columns(6, 12).Group();
                            worksheetTotals.CollapseColumns();

                            // **** Projects Sheet **** //

                            // Add another sheet here for the project summary
                            var worksheetProjects = workbook.Worksheets.Add("Projects", 1);
                            worksheetProjects.SheetView.FreezeRows(1);

                            // Header row
                            props = typeof(ProjectBudgetSummary).GetProperties().ToList();
                            for (int i = 0; i < props.Count; ++i)
                            {
                                cell = worksheetProjects.Cell(1, 1 + i);
                                cell.Value = props[i].Name;
                                cell.Style.Font.Bold = true;
                            }
                            cell = worksheetProjects.Cell(1, 1 + props.Count);
                            cell.Value = "Deficit";
                            cell.Style.Font.Bold = true;

                            // Data
                            for (int i = 0; i < projectData.Count; ++i)
                            {
                                var proj = projectData[i];

                                // Column A: Project ID
                                cell = worksheetProjects.Cell(2 + i, 1);
                                cell.Value = proj.ProjectId;

                                // Column B: Project name
                                cell = worksheetProjects.Cell(2 + i, 2);
                                cell.Value = proj.ProjectName;

                                // Column C: PlannedCosts
                                cell = worksheetProjects.Cell(2 + i, 3);
                                cell.Value = proj.PlannedCosts;
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Column D: RecoveredCosts
                                cell = worksheetProjects.Cell(2 + i, 4);
                                cell.Value = proj.RecoveredCosts;
                                cell.Style.NumberFormat.Format = moneyFormat;

                                // Column E: Formula = C - B (relative R1C1, no anchors)
                                cell = worksheetProjects.Cell(2 + i, 5);
                                cell.FormulaR1C1 = "=RC[-1]-RC[-2]";
                                cell.Style.NumberFormat.Format = moneyFormat;
                            }

                            // Adjust the column widths
                            worksheetProjects.Columns().AdjustToContents();

                            // **** Summary Worksheet **** //

                            // Add another sheet here for the overall summary
                            var ws = workbook.Worksheets.Add("Summary", 0);
                            ws.SheetView.FreezeRows(1);

                            // Title
                            cell = ws.Cell("A1");
                            cell.Value = "Cost Summary";
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontSize = 16;
                            ws.Range("A1:B1").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                            // Period length
                            cell = ws.Cell("A2");
                            cell.Value = "Period (months)";
                            ws.Cell("B2").Value = monthsAhead;

                            // Annual budget
                            cell = ws.Cell("A3");
                            cell.Value = "Annual Budget";
                            cell = ws.Cell("B3");
                            cell.Value = 936370;
                            cell.Style.NumberFormat.Format = moneyFormat;
                            comment = cell.CreateComment();
                            comment.Author = "CapX Exporter";
                            comment.AddText("Number lifted from the tracker");

                            // Style header
                            var range = ws.Range("A1:B3");
                            range.Style.Fill.BackgroundColor = XLColor.LightGray;
                            range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            range.Style.Border.BottomBorderColor = XLColor.Black;

                            // Write the rows
                            AddSummaryRow(ws, 4, "How much I think we cost (mid-grade estimates)", moneyFormat, $"=Costs!C{totalRow}");
                            AddSummaryRow(ws, 5, "How much we aim to recover through WLM project work allocations (salary estimates)", moneyFormat, $"=Costs!M{totalRow}");
                            AddSummaryRow(ws, 6, "How much we could recover (if all work we do as assignments is paid for)", moneyFormat, $"=Costs!V{totalRow}");
                            AddSummaryRow(ws, 7, "How much we can't recover as money ran out (i.e. work we did for free)", moneyFormat, $"=Costs!W{totalRow} - Costs!V{totalRow}");
                            AddSummaryRow(ws, 8, "How much we actually can recover (based on money in the project budgets)", moneyFormat, null, "=R[-2]C + R[-1]C");
                            AddSummaryRow(ws, 9, "Actual surplus against cost recovery target due to combination of working for free and under assignment", moneyFormat, null, "=R[-1]C - R[-4]C");
                            AddSummaryRow(ws, 10, "How much ITS give us (baseline budget)", moneyFormat, null, "=R[-8]C * R[-7]C / 12");
                            AddSummaryRow(ws, 11, "Surplus against the budget provided by ITS to cover current operation (salary estimate)", moneyFormat, null, "=R[-1]C - (R[-7]C - R[-3]C)");
                            AddSummaryRow(ws, 12, "How much we actually cost (from tracker)", moneyFormat, $"Costs!D{totalRow}");
                            AddSummaryRow(ws, 13, "Surplus against the budget provided by ITS to cover operation based on actual costs from tracker", moneyFormat, null, "=R[-3]C - (R[-1]C - R[-5]C)");

                            // Style final row
                            range = ws.Range("A13:B13");
                            range.Style.Fill.BackgroundColor = XLColor.LightGray;
                            range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                            range.Style.Border.TopBorderColor = XLColor.Black;

                            // Adjust the column widths
                            ws.Columns().AdjustToContents();

                            // Save the workbook
                            workbook.SaveAs(path);
                        }

                        Debug.WriteLine($"** Exported report to {path}");

                        // Run the file export on the render context
                        await InvokeAsync(async () =>
                        {
                            // Get file stream
                            using var streamRef = new DotNetStreamReference(stream: File.Open(path, FileMode.Open));

                            // Invoke JS on the client to download the file
                            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
                        });
                    }
                    catch (Exception ex)
                    {
                        LogError($"Could not export report: {ex}");
                    }
                }

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

        /// <summary>
        /// Helper method to add a summary row
        /// </summary>
        /// <param name="ws"></param>
        /// <param name="row"></param>
        /// <param name="text"></param>
        /// <param name="numberFormat"></param>
        /// <param name="formulaA1"></param>
        /// <param name="formulaR1C1"></param>
        private void AddSummaryRow(IXLWorksheet ws, int row, string text, string numberFormat, string formulaA1 = null, string formulaR1C1 = null)
        {
            var cell = ws.Cell(row, 1);
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell = ws.Cell(row, 2);
            if (!string.IsNullOrWhiteSpace(formulaA1))
            {
                cell.FormulaA1 = formulaA1;
            }
            else if (!string.IsNullOrWhiteSpace(formulaR1C1))
            {
                cell.FormulaR1C1 = formulaR1C1;
            }
            else
            {
                throw new Exception("Formula not provided!");
            }
            cell.Style.NumberFormat.Format = numberFormat;
        }
    }
}
