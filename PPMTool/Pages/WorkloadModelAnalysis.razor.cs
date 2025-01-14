using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using DotNetExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor.Rendering;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class WorkloadModelAnalysis : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private TimesheetService TimesheetService { get; set; }

        private Dictionary<string, List<WLMWeeklyDataChartItem>> wlmChartItems = new Dictionary<string, List<WLMWeeklyDataChartItem>>();
        private List<ApexChartOptions<WLMWeeklyDataChartItem>> wlmChartOptions = new List<ApexChartOptions<WLMWeeklyDataChartItem>>();
        private bool compareToWLM = true;
        private bool normalisedByTotalHours = false;
        private DateTime? startDate = DateTime.Today.StartOfMonth().StartOfWeek();
        private DateTime? endDate = DateTime.Today.StartOfWeek().AddDays(7);
        private IEnumerable<Person> availablePeople;
        private IEnumerable<Person> selectedPeople;
        private string loadingMessage;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Available people are all if superuser or self and people they manage and are current staff
            availablePeople = PersonService.GetAll(Context).OrderBy(x => x.Name);
            if (RolesService.GetRoleTypeForUsername(Context, ActiveUserName) != RoleType.Superuser)
            {
                availablePeople = availablePeople
                    .Where(x => x.PersonId == ActiveUser?.PersonId || (x.LineManager?.PersonId == ActiveUser?.PersonId && x.IsCurrentStaff()))
                    .OrderBy(x => x.Name);
            }

            LogInformation($"Viewing WLM analysis page");
        }

        /// <summary>
        /// Method to trigger state has changed after the display style settings of the charts has been updated
        /// </summary>
        private void DisplayStyleChanged()
        {
            StateHasChanged();
        }

        /// <summary>
        /// Method to increment the end date to so many months after the start date
        /// </summary>
        /// <param name="numberOfMonths"></param>
        private void SetEndDate(int numberOfMonths)
        {
            endDate = startDate.Value.AddMonths(numberOfMonths);
        }

        /// <summary>
        /// Method to generate chart objects
        /// </summary>
        private void GenerateCharts()
        {
            // Start the spinner
            Loading = true;
            loadingMessage = "Loading...";
            wlmChartItems.Clear();
            wlmChartOptions.Clear();

            Task.Run(() =>
            {
                LogInformation("Generating WLM graphs...");

                // Adjust the start and end dates to the nearest Monday before and Monday after
                startDate = startDate.Value.StartOfWeek();
                endDate = endDate.Value.StartOfWeek().AddDays(7);
                var totalTime = endDate.Value.Subtract(startDate.Value).TotalMilliseconds;

                // For each person selected
                foreach (var person in selectedPeople)
                {
                    LogInformation($"Generating chart items for {person.Name}");

                    // Initialise a list of data
                    var data = new List<WLMWeeklyDataChartItem>();

                    // Get all timesheets in the date range
                    var allTimesheets = TimesheetService.GetAllTimesheetsForPersonInDateRange(Context, person, startDate.Value, endDate.Value);

                    // Loop over the weeks
                    var weekStart = startDate.Value;
                    while (weekStart < endDate.Value)
                    {
                        var percent = (int)(weekStart.Subtract(startDate.Value).TotalMilliseconds * 100 / totalTime);
                        loadingMessage = $"Loading...{person.Name} ({percent}%)";
                        InvokeAsync(StateHasChanged);

                        // Get the workload model change in place on the date of the week start
                        var wlm = person.GetWorkloadModelOnDateOrDefault(weekStart);

                        // Create a chart item
                        var item = new WLMWeeklyDataChartItem()
                        {
                            WeekStart = weekStart,
                            WLMWeeklyTargetsByDuty = new Dictionary<Duty, float>
                            {
                                { Duty.Other, 0 },
                                { Duty.ProjectWork, (float)wlm.ProjectWorkFTE },
                                { Duty.BAU, (float)wlm.BusinessAsUsualFTE },
                                { Duty.PersonalDevelopment, (float)wlm.PersonalDevelopmentFTE },
                                { Duty.StaffMgmt, (float)wlm.StaffManagementFTE },
                                { Duty.ProjectAndServiceMgmt, (float)wlm.ProjectAndServiceManagementFTE},
                                { Duty.RSA, (float)wlm.ArchitectureFTE },
                            }
                        };

                        // Loop over each task in the current timesheet
                        var currentTimesheet = allTimesheets.FirstOrDefault(x => x.StartDate.Date == weekStart.Date);
                        if (currentTimesheet != null)
                        {
                            foreach (var entry in currentTimesheet.TimesheetEntries)
                            {
                                // Update values in the entry as not in DB
                                entry.UpdateTotalHours();

                                // Add the hours for the task to the relevant item in the dictionary
                                item.WeeklyValuesByDuty[entry.InnateCodeTask.Duty] += (float)entry.TotalHours;
                            }
                        }

                        // Find total hours worked (excluding leave)
                        float totalHours = 0f;
                        foreach (var duty in item.WeeklyValuesByDuty.Keys.Where(x => x != Duty.Other))
                        {
                            totalHours += item.WeeklyValuesByDuty[duty];
                        }
                        item.TotalHoursForWeek = totalHours;

                        // How many hours expected from WLM
                        var wlmTargetTotalHours = item.WLMWeeklyTargetsByDuty.Sum(x => x.Value) * 35f;

                        // Convert raw hours to FTE based on standard week
                        foreach (var duty in item.WeeklyValuesByDuty.Keys)
                        {
                            item.WeeklyValuesByDuty[duty] /= 35f;
                        }

                        // If underbooked due to time on leave or we are on a shorter working week then scale WLM targets for the week
                        if (totalHours < wlmTargetTotalHours)
                        {
                            var fractionWorking = totalHours / wlmTargetTotalHours;
                            foreach (var duty in item.WeeklyValuesByDuty.Keys)
                            {
                                item.WLMWeeklyTargetsByDuty[duty] *= fractionWorking;
                            }
                        }

                        // Compute the net
                        item.UpdateWLMNetValues();

                        // Add to list
                        data.Add(item);

                        // Increment
                        weekStart = weekStart.AddDays(7);
                    }

                    // Add items to the chart data dictionary
                    wlmChartItems.Add(person.Name, data);

                    // Create a chart options object
                    CreateChartOptions();
                }
            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });
            });
        }

        /// <summary>
        /// Creates a chart options object
        /// </summary>
        private void CreateChartOptions()
        {
            // Chart options
            wlmChartOptions.Add(new ApexChartOptions<WLMWeeklyDataChartItem>
            {
                Chart = new Chart
                {
                    Type = ChartType.Bar,
                    Stacked = true,
                    StackOnlyBar = true,
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
                },
                Yaxis = new List<YAxis>
                {
                    new YAxis
                    {
                        Labels = new YAxisLabels
                        {
                            Formatter = @"function (val, index) { return val.toFixed(2); }"
                        },
                        ForceNiceScale = true
                    }
                },
                Xaxis = new XAxis
                {
                    Categories = Enum.GetValues<Duty>().Select(x => x.GetDescription())
                }
            });
        }
    }
}
