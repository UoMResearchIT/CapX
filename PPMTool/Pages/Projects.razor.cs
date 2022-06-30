using System;
using System.Collections.Generic;
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
    public partial class Projects : ComponentBase
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        private IEnumerable<Project> projects;
        private List<ChartItem> chartSource = new List<ChartItem>();
        private ApexChartOptions<ChartItem> options;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get people from the database
            using var context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).ToArray();
            if (proj.Count() > 0)
            {
                projects = proj;

                // Organise the project data so it is plottable
                foreach (var p in proj)
                {
                    chartSource.AddRange(ChartHelper.AggregateByWeek(p.SubTasks, x =>
                    {
                        // Value summed is the average contruibution of the task for that week
                        var durationWeeks = x.DurationHours / (7 * 7);
                        return x.PlannedWorkHours / durationWeeks;
                    }, p.Name));
                }
            }

            options = new ApexChartOptions<ChartItem>
            {
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = true,
                        RangeBarGroupRows = false
                    }
                }
            };
        }

        private void ProjectClicked(int id)
        {
            NavigationManager.NavigateTo($"/projectdetails/{id}");
        }
    }
}
