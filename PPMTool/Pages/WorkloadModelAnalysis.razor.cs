using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class WorkloadModelAnalysis : BasePage
    {
        private List<DutyChartItem> dutyChartItems = new List<DutyChartItem>();
        private ApexChartOptions<DutyChartItem> dutyChartOptions;

        [Inject]
        private PersonService PersonService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Chart options
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

            // Start the spinner
            Loading = true;
        }

        private void GenerateCharts()
        {
            Loading = true;
            Task.Run(() =>
            {
                Debug.WriteLine("** Starting generation...");

                // TODO: Generate the chart items

                // Assign X Labels for duty chart
                dutyChartOptions.Xaxis = new XAxis
                {
                    //Categories = dutyXLabels.ToArray()
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
    }
}
