using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class Capacity : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        // This is the profile of the team after processing
        private IEnumerable<CapacityProfile> teamCapacityProfiles;

        // This is the flattened version of the above required by the charting library
        private IEnumerable<CapacityItem> chartSource;
        private ApexChartOptions<CapacityItem> options;

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;

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

            await Task.Run(() =>
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
                        var assignedSubTasks = ProjectService.GetAll(context).SelectMany(x =>
                        {
                            return x.SubTasks.Where(y => y.AssignedResources.Any(z => z.Person == p));
                        });

                        // Generate capacity profile for this person from their assignments
                        var capProf = new CapacityProfile(p, assignedSubTasks);

                        // Add to the team profile list
                        temp.Add(capProf);
                    }

                    teamCapacityProfiles = temp;

                    // Flatten the team capacity to format required by chart source for the default view
                    chartSource = teamCapacityProfiles.SelectMany(x => x.GetWeekByWeekLoad());

                    Debug.WriteLine($"** ChartSource has {chartSource.Count()} entries!");
                }
            }).ContinueWith(t =>
            {
                IsLoading = false;
                InvokeAsync(StateHasChanged);
            });

        }
    }
}
