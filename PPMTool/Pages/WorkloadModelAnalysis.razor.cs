using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private class TimesheetReportLine
        {
            public string Resource { get; set; }
            public string Activity { get; set; }
            public string Task { get; set; }
            public IList<float> WeeklyValues { get; set; } = new List<float>();
        }

        private List<DutyChartItem> dutyChartItems = new List<DutyChartItem>();
        private ApexChartOptions<DutyChartItem> dutyChartOptions;
        private byte[] file;
        private string fileName;
        private long? fileSize;

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
        }

        void OnError(UploadErrorEventArgs args, string name)
        {
            LogError($"File Upload Failed: {args.Message}");
        }

        void OnChange(byte[] value, string name)
        {
            // Start the spinner
            Loading = true;

            try
            {
                // Data
                var dates = new List<DateTime>();
                var reportData = new List<TimesheetReportLine>();

                // Bail or read from stream
                if (value == null)
                {
                    Loading = false;
                    return;
                }

                // Convert text -- arrives as a base64 image bizarrely!
                var fileText = System.Text.Encoding.Default.GetString(value);
                string[] dbInfo = fileText.Split("base64,");
                var base64Contents = dbInfo[1].ToString();
                byte[] contentsAsBytes = Convert.FromBase64String(base64Contents);
                fileText = System.Text.Encoding.Default.GetString(contentsAsBytes);

                // Split into lines
                var lines = fileText.Split("\n");

                // Read one line at a time
                bool headersParsed = false;
                int columnCount = 0;
                foreach (var line in lines)
                {
                    // Split line
                    var values = line.Split("\t");

                    // Continue if no data on the line
                    if (values.Length < 3) continue;

                    // Continue if the final line
                    if (values[0] == "Page total") continue;

                    // Error if it is somewhere in the middle of the file and there is something up with the formatting
                    if (headersParsed && values.Length != columnCount)
                    {
                        throw new Exception($"Line {string.Join("|", new string[] { values[0], values[1], values[2] })} does not have the same number of columns {values.Length} as expected {columnCount}!");
                    }

                    // If the line starts with "Resource"
                    if (!headersParsed && values[0] == "Resource")
                    {
                        // Check it has the required neighbouring columns
                        if (values[1] != "Activity" || values[2] != "Task" || !DateTime.TryParse(values[3], out var temp))
                        {
                            throw new Exception("File needs to have columns named Resource, Activity, Task before the weekly data!");
                        }

                        // Parse the dates
                        for (var week = 3; week < values.Length; week++)
                        {
                            dates.Add(DateTime.Parse(values[week]));
                        }

                        // Move to next line
                        columnCount = values.Length;
                        Debug.WriteLine($"** Expecting {values.Length - 3} weeks in the file");
                        headersParsed = true;
                        continue;
                    }

                    // Skip if not yet reached the header row
                    if (!headersParsed) continue;

                    // Generate an object from the line and stip out double quotes
                    var obj = new TimesheetReportLine
                    {
                        Resource = values[0].Replace("\"", "").Replace("\r", ""),
                        Activity = values[1].Replace("\"", "").Replace("\r", ""),
                        Task = values[2].Replace("\"", "").Replace("\r", "")
                    };

                    // Get weekly data and strip the first three columns
                    var valuesAsList = values.ToList();
                    valuesAsList.RemoveRange(0, 3);

                    // Parse to floats and add to object
                    obj.WeeklyValues = valuesAsList.Select(x =>
                    {
                        return float.TryParse(x, out var value) ? value : 0f;
                    }).ToList();
                    reportData.Add(obj);
                }

                // TODO: Now build the chart data arrays
                Debug.WriteLine("The end!");

                Loading = false;

            }
            catch (Exception ex)
            {
                // Present an error notification to the user
                ShowNotification(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Upload Issue",
                    Detail = $"{ex.Message}",
                    Duration = 4000
                });
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
