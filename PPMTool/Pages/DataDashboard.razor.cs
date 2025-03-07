// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Editing only permitted by superusers
            EditAuthorised = ActiveUserRoleType == RoleType.Superuser;

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

            Loading = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("setFinishedFlag", showFinishedAsSeparate);

                // Set the initial settings and generate the charts
                viewOption = ViewOption.CurrentFY;
                ViewOptionChanged();
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

                // Create a thread-local context
                var context = ContextFactory.CreateDbContext();

                // Variable chart options
                demandChartOptions.Fill.Colors = GetColours(ChartColourSet.DutyChart);
                demandChartOptions.Colors = GetColours(ChartColourSet.DutyChart);

                // Clear the existing demand item lists
                demandChartItems.Clear();
                dutyChartItems.Clear();

                // Tracked values
                var currentWeekStart = startDate;
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
                    var projectsThisWeekConfirmed = projectsThisWeekNotCancelled.Where(x => !x.ProjectStatus.IsUnconfirmed());

                    // Remove finished projects if being shown separately
                    if (showFinishedAsSeparate)
                    {
                        projectsThisWeekConfirmed = projectsThisWeekConfirmed.Where(x => x.ProjectStatus != ProjectStatus.Finished);
                    }

                    // Get tasks
                    var tasksOnConfirmedProjectsThisWeek = projectsThisWeekConfirmed.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));

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
                    var tasksOnUnconfirmedProjectsThisWeek = projectsThisWeekUnconfirmed.SelectMany(x => x.SubTasks.Where(x => x.IsWithin(currentWeekStart, currentWeekStart.AddDays(6))));

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
                        return InvoiceService.GetFundsReceived(context, x.ProjectId) / (x.EndDate.Subtract(x.StartDate).TotalDays / 7f);
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

                    // Get the people who are employed for at least one day during the week
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

                // Create blank list of data
                var allData = new List<ExportHelper.AssignmentChunk>();

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
                        ProjectService.GetAll(threadContext),
                        startDate,
                        endDate,
                        FinancialReferenceService.GetAll(threadContext)
                    );
                    allData.AddRange(data);
                }
                allData.Sort((x, y) => x.EmployeeName.CompareTo(y.EmployeeName));

                // Run the file export on the render context
                await InvokeAsync(async () =>
                {
                    try
                    {
                        // Write to CSV file
                        var filename = $"Capacity_{DateTime.Now.Ticks}.csv";
                        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapX");
                        Directory.CreateDirectory(folder);
                        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapX", filename);
                        using (var writer = new StreamWriter(path))
                        {

                            // Write header row
                            var props = typeof(ExportHelper.AssignmentChunk).GetProperties();
                            var propNames = props.Select(x => x.Name);
                            var headers = propNames.ToList();
                            writer.WriteLine(string.Join(",", headers));

                            // Write rows one at a time
                            foreach (var record in allData)
                            {
                                // Write properties
                                var valuesAsStrings = new List<string>();
                                foreach (var name in propNames)
                                {
                                    string value = record.GetType().GetProperty(name).GetValue(record)?.ToString() ?? string.Empty;

                                    // Project and task names with a comma will mess up the CSV format so replace with a semi-colon
                                    valuesAsStrings.Add(value.Replace(",", ";"));
                                }

                                // Write the row
                                writer.WriteLine(string.Join(",", valuesAsStrings));
                            }
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
