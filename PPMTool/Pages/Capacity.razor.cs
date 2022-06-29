using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using static PPMTool.Data.CapacityProfile;

namespace PPMTool.Pages
{
    public partial class Capacity : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        private IEnumerable<CapacityProfile> teamCapacityProfiles;
        private ApexChart<CapacityItem> chart;
        private IEnumerable<CapacityItem> chartSource;
        private ApexChartOptions<CapacityItem> options;
        private List<string> nameOptions;
        private string chartTitle;
        private string tooltipText;

        private string chosenPerson;
        private string ChosenPerson
        {
            get => chosenPerson;
            set
            {
                if (chosenPerson != value)
                {
                    chosenPerson = value;

                    // Update the chart source
                    Task.Run(async () => await ConfigureSourceAsync());
                }
            }
        }

        private bool includeUnFunded = true;
        public bool IncludeUnFunded
        {
            get => includeUnFunded;
            set
            {
                if (includeUnFunded != value)
                {
                    includeUnFunded = value;
                    
                    // Update the chart source
                    Task.Run(async () =>
                    {
                        UpdateCapacityProfiles();
                        await ConfigureSourceAsync();
                        await chart?.UpdateSeriesAsync();
                    });
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            options = new ApexChartOptions<CapacityItem>
            {
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = true,
                        RangeBarOverlap = true
                    }
                },
                Legend = new Legend
                {
                    Show = false
                }
            };

            // Get dropdown options
            using var context = new PPMToolContext();
            nameOptions = PersonService.GetAll(context).Select(p => p.Name).ToList();
            nameOptions.Sort();

            // Get data for chart
            UpdateCapacityProfiles();
            await ConfigureSourceAsync();
            StateHasChanged();
        }

        private void OnDataPointHover(HoverData<CapacityItem> e)
        {
            // HACK: This shouldn't be necessary but since the chart I see and the data behind it seem to be out of sync then I have no choice here.
            try
            {
                var item = e.Series.ApexSeries.Items.ElementAt(e.DataPointIndex);
                tooltipText = $"FTE: {item.FTE}% | {item.StartDate.ToShortDateString()} - {item.EndDate.ToShortDateString()}";
            }
            catch { }
        }

        private void OnDataPointHoverLeave(HoverData<CapacityItem> e)
        {
            tooltipText = null;
        }

        private void UpdateCapacityProfiles()
        {
            // Get people from the database
            using var context = new PPMToolContext();
            var peo = PersonService.GetAll(context);
            if (peo.Count() > 0)
            {
                var temp = new List<CapacityProfile>();
                foreach (var p in peo)
                {
                    // Pull all projects which contain subtasks to which that person is assigned
                    var projects = ProjectService.GetAll(context);
                    if (!IncludeUnFunded) projects = projects.Where(p => p.FundingStatus != FundingStatus.AwaitingSubmission && p.FundingStatus != FundingStatus.AwaitingOutcome);
                    Debug.WriteLine($"** {p.Name} has {projects?.Count()} projects to consider!");

                    // Create a list of assignments
                    var assignments = new List<Assignment>();
                    foreach (var project in projects)
                    {
                        foreach (var subTask in project.SubTasks)
                        {
                            if (subTask.AssignedResources.Any(z => z.Person == p))
                            {
                                assignments.Add(new Assignment(project.Name, subTask));
                            }
                        }
                    }

                    // Generate capacity profile for this person from their assignments
                    var capProf = new CapacityProfile(p, assignments);

                    // Add to the team profile list
                    temp.Add(capProf);
                }

                teamCapacityProfiles = temp;
            }
        }

        /// <summary>
        /// Repackages the capacity profile information into appropriate chart source
        /// </summary>
        private async Task ConfigureSourceAsync()
        {
            // Flatten the team capacity to format required by chart source
            if (ChosenPerson == "All" || ChosenPerson == null)
            {
                chartSource = teamCapacityProfiles.SelectMany(x => x.GetWeekByWeekLoad());
            }
            else
            {
                chartSource = teamCapacityProfiles.FirstOrDefault(x => x.Person.Name == ChosenPerson)?.GetProjectByProjectLoad();
            }
            chartTitle = $"Load for {ChosenPerson ?? "All"}";
            Debug.WriteLine($"** Finished configuring {chartTitle}. Include unfunded = {includeUnFunded}!");

            // First time this is called, there is no reference to the chart
            if (chart != null)
            {
                Debug.WriteLine($"** Re-renderering chart!");
                await chart?.RenderAsync();
            }
            Debug.WriteLine($"** ChartSource has {chartSource?.Count()} entries!");
        }
    }
}
