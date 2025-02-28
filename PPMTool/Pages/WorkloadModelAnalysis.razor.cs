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
            if (UserService.GetRoleTypeForUsername(Context, ActiveUserName) != RoleType.Superuser)
            {
                availablePeople = availablePeople
                    .Where(x => x.PersonId == ActiveUser?.Person?.PersonId || (x.LineManager?.PersonId == ActiveUser?.Person?.PersonId && x.IsCurrentStaff()))
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
                endDate = endDate.Value.StartOfWeek().AddDays(6);
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

                        // Build the data object for the chart
                        data.Add(WorkloadModelChartHelper.GetWorkloadModelChartData(person, weekStart, allTimesheets));

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
