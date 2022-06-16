using System;
using System.Collections.Generic;
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

        private IEnumerable<CapacityProfile> data;
        private ApexChartOptions<CapacityProfile> options;

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;

            options = new ApexChartOptions<CapacityProfile>
            {
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = true
                    }
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
                        // Pull all projects on which they are assigned into a flattened dictionary
                        var proj = ProjectService.GetAll(context).SelectMany(x =>
                        {
                            return x.SubTasks.Select(y =>
                            {
                                return new KeyValuePair<string, SubTask>
                                (
                                    x.Name,
                                    y
                                );
                            });
                        });

                        // Sort by start date
                        proj.ToList().Sort((x, y) => x.Value.StartDate.CompareTo(y.Value.StartDate));

                        // Add to the data source
                        //temp.Add(new CapacityProfile(p, proj));
                    }

                    data = temp;
                }
            }).ContinueWith(t =>
            {
                IsLoading = false;
                InvokeAsync(StateHasChanged);
            });

        }
    }
}
