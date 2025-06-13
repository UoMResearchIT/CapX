using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;
using static PPMTool.Enums.Extensions;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Contractor")]
    public partial class Timesheets : BasePage
    {
        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private ISessionStorageService SessionStorage { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        private bool hideStaffResults = true;
        private bool showAllMyTimesheets;
        private DateTime dateNextTimesheet;
        private DateTime dateMondayThisWeek;
        private DateTime synopsisStartDate;
        private DateTime synopsisEndDate;
        private List<DateTime> synopsisDates;
        private List<Timesheet> myTimesheets;
        private List<Timesheet> myStaffTimesheets;
        private Dictionary<Person, List<Timesheet>> myStaffTimesheetsInPeriod;
        private bool initialLoadComplete;
        private bool exportRunning = false;

        public bool ShowAllMyTimesheets
        {
            get => showAllMyTimesheets;
            private set
            {
                if (value != showAllMyTimesheets)
                {
                    showAllMyTimesheets = value;
                    SessionStorage.SetItemAsync<bool?>("timesheets-showall-mine", showAllMyTimesheets);
                    if (initialLoadComplete)
                    {
                        Loading = true;
                        EnqueueLoadData(GenerateTask);
                    }
                }
            }
        }

        private bool showAllMyStaffTimesheets;
        public bool ShowAllMyStaffTimesheets
        {
            get => showAllMyStaffTimesheets;
            private set
            {
                if (value != showAllMyStaffTimesheets)
                {
                    showAllMyStaffTimesheets = value;
                    SessionStorage.SetItemAsync<bool?>("timesheets-showall-reports", showAllMyStaffTimesheets);
                    if (initialLoadComplete)
                    {
                        Loading = true;
                        EnqueueLoadData(GenerateTask);
                    }
                }
            }
        }

        private bool showSynopsis = true;
        public bool ShowSynopsis
        {
            get => showSynopsis;
            private set
            {
                if (value != showSynopsis)
                {
                    showSynopsis = value;
                    SessionStorage.SetItemAsync<bool?>("timesheets-showsynopsis", showSynopsis);
                }
            }
        }

        private bool superuserShowSynopsisForAllStaff = false;

        public bool SuperuserShowSynopsisForAllStaff
        {
            get => superuserShowSynopsisForAllStaff;
            private set
            {
                if (value != superuserShowSynopsisForAllStaff)
                {
                    superuserShowSynopsisForAllStaff = value;
                    SessionStorage.SetItemAsync<bool?>("timesheets-superuser-showall", superuserShowSynopsisForAllStaff);
                    if (initialLoadComplete)
                    {
                        Loading = true;
                        EnqueueLoadData(GenerateTask);
                    }
                }
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = true;
            LogInformation("Viewing Timesheets");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            // Load state from session storage and once finished, load the data
            var temp = await SessionStorage.GetItemAsync<bool?>("timesheets-showall-mine");
            if (temp != null) ShowAllMyTimesheets = temp ?? false;
            temp = await SessionStorage.GetItemAsync<bool?>("timesheets-showall-reports");
            if (temp != null) ShowAllMyStaffTimesheets = temp ?? false;
            temp = await SessionStorage.GetItemAsync<bool?>("timesheets-showsynopsis");
            if (temp != null) showSynopsis = temp ?? true;
            temp = await SessionStorage.GetItemAsync<bool?>("timesheets-superuser-showall");
            if (temp != null) SuperuserShowSynopsisForAllStaff = temp ?? true;

            Loading = true;
            StateHasChanged();
            EnqueueLoadData(GenerateTask);
        }

        /// <summary>
        /// Generates a task to load data
        /// </summary>
        /// <returns></returns>
        private Task GenerateTask()
        {
            return Task.Run(() =>
            {
                LoadData();
            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    if (!initialLoadComplete)
                    {
                        initialLoadComplete = true;
                    }
                    Loading = false;
                    StateHasChanged();
                });
            });
        }

        private void LoadData()
        {
            // Get ALL timesheets for the user, then filter stuff out based the state of the ShowAll switch. 
            myTimesheets = new List<Timesheet>(); // Initialise the list
            myTimesheets = TimesheetService.GetMyTimesheets(Context, ActiveUser).OrderByDescending(t => t.StartDate).ToList();

            // Set the start date for the next one
            dateNextTimesheet = TimesheetService.GetNextTimesheetStartDateForUser(Context, ActiveUser);

            if (!ShowAllMyTimesheets)
            {
                // Remove items with Submitted or Approved status
                myTimesheets = myTimesheets.Where(t => t.Status != TimesheetStatus.Submitted && t.Status != TimesheetStatus.Approved).ToList();
            }

            // Show second grid if user manages staff - need to see the timesheets they have submitted.
            var managedPeople = PersonService.GetManagedStaff(Context, ActiveUser);

            // If Superuser then _potentially_ they may not manage staff but can see staff synopsis for all staff
            if ((ActiveUserRoleType == RoleType.Superuser) && SuperuserShowSynopsisForAllStaff)
            {
                // Get all staff if switch is selected
                managedPeople = PersonService.GetAllShallow(Context);
            }
            if (managedPeople.Count() > 0)  // Is a manager
            {
                hideStaffResults = false;  // Show/Hide the second grid based on this
                myStaffTimesheets = new List<Timesheet>();
                myStaffTimesheetsInPeriod = new Dictionary<Person, List<Timesheet>>();
                dateMondayThisWeek = GetDateForMondayThisWeek();
                synopsisStartDate = GetDateForAMonday(7); // Weeks in the past
                synopsisEndDate = GetDateForAMonday(2, false); // Weeks in the future
                synopsisDates = GetSynopsisDates(synopsisStartDate, synopsisEndDate);

                foreach (Person p in managedPeople
                    .Where(p => p.PersonId != ActiveUser?.PersonId)
                    .OrderBy(p => p.ShortName)) // For AH who is self-managed
                {
                    // Get timesheets of the person
                    myStaffTimesheets.AddRange(TimesheetService.GetMyTimesheets(Context, p).ToList());

                    // Only add the person to the synopsis if they are currently here in the window
                    if (p.EndDate == null || p.EndDate >= synopsisStartDate)
                    {
                        // Get timesheets in the range for that person
                        myStaffTimesheetsInPeriod[p] = TimesheetService.GetAllTimesheetsForPersonInDateRange(Context, p, synopsisStartDate, synopsisEndDate).OrderBy(t => t.StartDate).ToList();

                        // Pad the timesheet list with nulls
                        if (myStaffTimesheetsInPeriod[p].Count < synopsisDates.Count)
                        {
                            myStaffTimesheetsInPeriod[p] = GetPaddedTimesheetList(synopsisDates, myStaffTimesheetsInPeriod[p]);
                        }
                    }
                }

                if (!ShowAllMyStaffTimesheets)
                {
                    // Filter the list to only show items with Submitted status
                    myStaffTimesheets = myStaffTimesheets.Where(t => t.Status == TimesheetStatus.Submitted).ToList();
                }

                // Order the list, whatever it holds (but remove any New items as these haven't been submitted by the staff member yet!)
                myStaffTimesheets = myStaffTimesheets.Where(t => t.Status != TimesheetStatus.New).OrderByDescending(t => t.StartDate).ToList();
            }
        }

        /// <summary>
        /// Add a new timesheet
        /// </summary>
        void AddTimesheet()
        {
            Navigation.NavigateTo("timesheets/addtimesheet/-1");
        }

        /// <summary>
        /// Navigate to the specific timesheet to view/edit it
        /// <param name="timesheet"></param>
        /// </summary>
        private void EditTimesheet(Timesheet timesheet)
        {
            Navigation.NavigateTo($"timesheets/addtimesheet/{timesheet.TimesheetId}");
        }

        /// <summary>
        /// Get a datetime for a Monday in the past to find timesheets since
        /// </summary>
        private DateTime GetDateForMondayThisWeek()
        {
            DateTime today = DateTime.Today;
            int daysSinceMonday = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysSinceMonday < 0) daysSinceMonday += 7; // Adjust for Sunday
            return today.AddDays(-daysSinceMonday);
        }

        /// <summary>
        /// Get a datetime for a Monday in the past to find timesheets since
        /// <param name="numberOfWeeks"></param>
        /// <param name="inPast"></param>
        /// </summary>
        private DateTime GetDateForAMonday(int numberOfWeeks, bool inPast = true)
        {
            DateTime thisWeekMonday = GetDateForMondayThisWeek();
            return (inPast ? thisWeekMonday.AddDays(-numberOfWeeks * 7) : thisWeekMonday.AddDays(numberOfWeeks * 7));
        }

        /// <summary>
        /// Build the list of dates we want to show the synopsis for
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// </summary>
        private List<DateTime> GetSynopsisDates(DateTime start, DateTime end)
        {
            DateTime nextDate;
            List<DateTime> allDates = new List<DateTime>();
            for (nextDate = start; nextDate <= end; nextDate = nextDate.AddDays(7))
            {
                allDates.Add(nextDate);
            }
            return allDates;
        }

        /// <summary>
        /// Pads the list of timesheets with nulls so that the order/structure matches the list of dates
        /// <param name="dates"></param>
        /// <param name="unpaddedList"></param>
        /// </summary>
        private List<Timesheet> GetPaddedTimesheetList(List<DateTime> dates, List<Timesheet> unpaddedList)
        {
            List<Timesheet> paddedList = new List<Timesheet>();
            List<DateTime> datesFromTimesheet = unpaddedList.Select(t => t.StartDate).ToList();
            foreach (DateTime date in dates)
            {
                // This will return null if no match
                Timesheet sheet = unpaddedList.FirstOrDefault(t => t.StartDate == date);
                paddedList.Add(sheet);
            }
            return paddedList;
        }


        /// <summary>
        /// A task to download the data for all contractors.
        /// </summary>
        private void DownloadData()
        {
            exportRunning = true;
            Task.Run(async () =>
            {
                // Create a context to be accessed on this thread
                var threadContext = ContextFactory.CreateDbContext();
                var timesheets = TimesheetService.GetAll(threadContext);
                var excludedTaskCodes = new HashSet<int> { 1, 2, 3 };  // Excluded codes

                List<TimesheetDataDownloadDto> dailyEntries = new List<TimesheetDataDownloadDto>();
                foreach (Timesheet t in timesheets)
                {
                    var dailySummaries = t.GetDailySummaries(excludedTaskCodes);
                    if (dailySummaries != null)
                    {
                        dailyEntries.AddRange(dailySummaries);
                    }
                }

                // Run the file export on the render context
                await InvokeAsync(async () =>
                {
                    try
                    {
                        // Create file path
                        var filename = $"TimesheetData__{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.xlsx";
                        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimesheetDataExport");
                        Directory.CreateDirectory(folder);
                        var path = Path.Combine(folder, filename);

                        // Create workbook
                        using (var workbook = new XLWorkbook())
                        {
                            // Create a tab
                            var worksheet = workbook.Worksheets.Add("Timesheet Data");

                            // Extract headers dynamically
                            var properties = typeof(TimesheetDataDownloadDto).GetProperties();

                            // Get human-friendly column headers if they exist
                            var headers = properties.Select(p =>
                            {
                                var attr = p.GetCustomAttribute<ExcelHeaderAttribute>();
                                return attr?.HeaderName ?? p.Name; // Use attribute value if available, otherwise default to property name
                            }).ToList();


                            // Apply headers and formatting
                            for (int i = 0; i < headers.Count; i++)
                            {
                                worksheet.Cell(1, i + 1).Value = headers[i];
                                worksheet.Cell(1, i + 1).Style.Font.Bold = true;  // Add bold headers

                                // Centralise the "hours" column
                                if (headers[i].Contains("Hours", StringComparison.OrdinalIgnoreCase))
                                {
                                    worksheet.Column(i + 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                                }
                            }
                            int row = 2;

                            // Write the data rows
                            foreach (var entry in dailyEntries)
                            {
                                for (int col = 0; col < headers.Count; col++)
                                {
                                    bool isDateTime = properties[col].PropertyType == typeof(DateTime);
                                    var value = properties[col].GetValue(entry);

                                    var cell = worksheet.Cell(row, col + 1);
                                    if (value is DateTime dateValue)
                                    {
                                        cell.Value = dateValue.ToString("dd/MM/yy");
                                        cell.Style.DateFormat.Format = "dd/MM/yy"; // Apply formatting in the spreadsheet
                                    }
                                    else
                                    {
                                        cell.Value = value.ToString() ?? "";
                                    }
                                }
                                row++;
                            }

                            worksheet.SheetView.FreezeRows(1); // Freezes the first rown
                            worksheet.Columns().AdjustToContents(); // Autofit columns

                            // Save the workbook
                            workbook.SaveAs(path);
                        }

                        Debug.WriteLine($"** Exported to {path}");

                        // Get file stream
                        using var streamRef = new DotNetStreamReference(stream: File.Open(path, FileMode.Open));

                        // Invoke JS on the client to download the file
                        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Could not download file: {ex}");
                    }
                });
            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    exportRunning = false;
                    StateHasChanged();
                });
            });
            StateHasChanged();
        }

    }
}
