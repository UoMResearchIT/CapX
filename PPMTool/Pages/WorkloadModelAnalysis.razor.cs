using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using DotNetExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class WorkloadModelAnalysis : BasePage
    {
        private class TimesheetReportLine
        {
            public string Resource { get; set; }
            public string Activity { get; set; }
            public string Task { get; set; }
            public Duty Duty { get; set; }
            public IList<float> WeeklyValues { get; set; } = new List<float>();
        }

        private Dictionary<string, List<WLMWeeklyDataChartItem>> wlmChartItems = new Dictionary<string, List<WLMWeeklyDataChartItem>>();
        private List<ApexChartOptions<WLMWeeklyDataChartItem>> wlmChartOptions = new List<ApexChartOptions<WLMWeeklyDataChartItem>>();
        private byte[] file;
        private string fileName;
        private long? fileSize;
        private bool compareToWLM = true;
        private bool normalisedByTotalHours = false;

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            LogInformation($"Viewing WLM analysis page");
        }

        private void OnError(UploadErrorEventArgs args, string name)
        {
            LogError($"File Upload Failed: {args.Message}");
        }

        private void DisplayStyleChanged()
        {
            StateHasChanged();
        }

        private void OnFileChanged(byte[] value, string name)
        {
            // Start the spinner
            Loading = true;
            wlmChartItems.Clear();
            wlmChartOptions.Clear();

            if (value != null) LogInformation($"File Uploaded - generating WLM graphs...");

            Task.Run(() =>
            {
                try
                {
                    // Data
                    var dates = new List<DateTime>();
                    var reportData = new List<TimesheetReportLine>();
                    var matchingFails = new List<string>();

                    // Bail or read from stream
                    if (value == null)
                    {
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
                    var hasTotalColumn = false;
                    int columnCount = 0;
                    foreach (var line in lines)
                    {
                        // Split line
                        var values = line.Split("\t");

                        // Continue if no data on the line
                        if (values.Length < 3) continue;

                        // Continue if the final line
                        if (values[0] == "Page total" || values[0] == "Total") continue;

                        // Error if it is somewhere in the middle of the file and there is something up with the formatting
                        if (headersParsed && values.Length != columnCount)
                        {
                            throw new Exception($"Line {string.Join("|", new string[] { values[0], values[1], values[2] })} does not have the same number of columns {values.Length} as expected {columnCount}!");
                        }

                        // If the line starts with "Resource"
                        if (!headersParsed && values[0] == "Resource")
                        {
                            // Check it has the required neighbouring columns
                            if ((values[1] != "Activity" && values[1] != "Project Activity Number & Name") || values[2] != "Task" || !DateTime.TryParse(values[3], out var temp))
                            {
                                throw new Exception("File needs to have columns named \"Resource\", \"Activity\" (or \"Project Activity Number & Name\"), \"Task\" before the weekly data!");
                            }

                            // Parse the dates
                            for (var week = 3; week < values.Length; week++)
                            {
                                if (values[week].Replace("\r", "") == "Total")
                                {
                                    hasTotalColumn = true;
                                    continue;
                                }
                                dates.Add(DateTime.Parse(values[week]));
                            }

                            // Move to next line
                            columnCount = values.Length;
                            Debug.WriteLine($"** Expecting {columnCount - 3 - (hasTotalColumn ? 1 : 0)} weeks in the file");
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

                        // Look up the duty
                        Debug.WriteLine($"** Looking up \"{obj.Activity}\" | \"{obj.Task}\"");
                        int duty = InnateCodeService.FindDutyForTask(context, obj.Activity, obj.Task);
                        if (duty == -1)
                        {
                            // Cannot find this combo in the database
                            var tag = $"{obj.Activity}|{obj.Task}";
                            if (!matchingFails.Contains(tag)) matchingFails.Add($"{obj.Activity}|{obj.Task}");
                        }
                        obj.Duty = (Duty)duty;

                        // Get weekly data and strip the first three columns and possibly the last column
                        var valuesAsList = values.ToList();
                        valuesAsList.RemoveRange(0, 3);
                        if (hasTotalColumn) valuesAsList.RemoveAt(valuesAsList.Count - 1);

                        // Parse to floats and add to object
                        obj.WeeklyValues = valuesAsList.Select(x =>
                        {
                            return float.TryParse(x, out var value) ? value : 0f;
                        }).ToList();
                        reportData.Add(obj);
                    }

                    Debug.WriteLine($"** Finished reading lines.");
                    if (matchingFails.Count > 0)
                    {
                        throw new Exception($"Cannot find the following \"activity\" | \"task\" combinations in the CapX timesheet DB! =>\n{string.Join(";\r\n", matchingFails)} => They will need to be added by a CapX admin to the database before the generation will succeed.");
                    }

                    // Group the data by person
                    var groupedData = reportData.GroupBy(x => x.Resource);

                    // For each group (person)
                    foreach (var resourceData in groupedData)
                    {
                        LogInformation($"Generating chart items for {resourceData.Key}");

                        // Initialise a list of data
                        var data = new List<WLMWeeklyDataChartItem>();

                        // Find the WLM active at the beginning of the week for that person
                        var person = PersonService.GetByName(context, resourceData.Key.Trim());

                        if (person == null)
                        {
                            throw new Exception($"Could not find a person in the CapX DB with the name {resourceData.Key}. Is their name in Innate and CapX different? If so, change the name in the Innate report to match that in CapX and try again.");
                        }

                        // For each week of data
                        for (var i = 0; i < columnCount - 3 - (hasTotalColumn ? 1 : 0); i++)
                        {
                            // Get the week
                            var weekStart = dates[i];

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

                            // Loop over each task in the group
                            foreach (var row in resourceData)
                            {
                                // Add the hours for the task to the relevant item in the dictionary
                                item.WeeklyValuesByDuty[row.Duty] += row.WeeklyValues[i];
                            }

                            // Find total hours
                            float totalHours = 0f;
                            foreach (var duty in item.WeeklyValuesByDuty.Keys.Where(x => x != Duty.Other))
                            {
                                totalHours += item.WeeklyValuesByDuty[duty];
                            }
                            item.TotalHoursForWeek = totalHours;

                            // Convert raw hours to FTE using chosen normalisation approach
                            item.NormaliseHours(normalisedByTotalHours);

                            // If some time has been spent away, then scale WLM targets for the week
                            if (totalHours > 0f && item.WeeklyValuesByDuty[Duty.Other] > 0f)
                            {
                                var totalWLMtargetTime = item.WLMWeeklyTargetsByDuty.Sum(x => x.Value);
                                var fractionWorking = (totalWLMtargetTime - item.WeeklyValuesByDuty[Duty.Other]) / totalWLMtargetTime;
                                foreach (var duty in item.WeeklyValuesByDuty.Keys)
                                {
                                    item.WLMWeeklyTargetsByDuty[duty] *= fractionWorking;
                                }
                            }

                            // Compute the net
                            item.UpdateWLMNetValues();

                            // Add to list
                            data.Add(item);
                        }

                        // Add items to the chart data dictionary
                        wlmChartItems.Add(resourceData.Key, data);

                        // Create a chart options object
                        CreateChartOptions();
                    }
                }
                catch (Exception ex)
                {
                    // Present an error notification to the user
                    InvokeAsync(() => ShowNotification(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Upload Issue",
                        Detail = $"{ex.Message}",
                        Duration = 10000,
                        Style = "position: fixed; top: 100%; left: 50%; transform: translate(-50%, -100%); width: 100%"
                    }));
                    LogError($"{ex.Message}");

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
                    Animations = new Animations { Enabled = false }
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
