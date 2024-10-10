using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Services;
using Radzen;

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

        private async void OnClientChange(UploadChangeEventArgs args)
        {
            Debug.WriteLine("** Client-side upload changed");

            foreach (var file in args.Files)
            {
                if (file.ContentType != "text/plain")
                {
                    throw new Exception("File is not of type text/plain as expected!");
                }

                Debug.WriteLine($"** File: {file.Name} / {file.Size} bytes");

                try
                {
                    // Open stream
                    using (var stream = file.OpenReadStream())
                    {
                        // Bail or read from stream
                        if (stream == null) throw new IOException("Could not open file!");

                        // Read into memory
                        var bytes = new byte[stream.Length];
                        await stream.ReadAsync(bytes);

                        // Convert text
                        var fileText = System.Text.Encoding.Default.GetString(bytes);
                        var lines = fileText.Split("\n");

                        // Read one line at a time
                        var dates = new List<DateTime>();
                        foreach (var line in lines)
                        {
                            // Split line
                            var values = line.Split("\t");

                            // Continue if no data on the line
                            if (values.Length == 0) continue;

                            // If the line starts with "Resource"
                            if (values[0] == "Resource")
                            {
                                // Check it has the required neighbouring columns
                                if (values[1] != "Activity" || values[2] != "Task" || !DateTime.TryParse(values[3], out var temp))
                                {
                                    throw new Exception("File needs to have columns named Resource, Activity, Task before the weekly data!");
                                }

                                // Parse the dates
                                for (var week = 4; week < values.Length; week++)
                                {
                                    dates.Add(DateTime.Parse(values[week]));
                                }

                                // Move to next line
                                continue;
                            }

                            // Generate an object from the line



                            Debug.WriteLine($"** Read: {line}");
                        }
                    }

                    // TODO: Now build the chart data arrays


                }
                catch (Exception ex)
                {
                    // TODO: Present an error notification to the user

                    Debug.WriteLine($"** Client-side file read error: {ex.Message}");
                }
            }
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
