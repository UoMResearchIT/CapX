using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Developer")]
    public partial class Timesheets : BasePage
    {
        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private ISessionStorageService SessionStorage { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

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
            if (temp != null) ShowSynopsis = temp ?? true;
            temp = await SessionStorage.GetItemAsync<bool?>("timesheets-superuser-showall");
            if (temp != null) SuperuserShowSynopsisForAllStaff = temp ?? true;
            EnqueueLoadData(GenerateTask);
        }

        /// <summary>
        /// Generates a task to load data
        /// </summary>
        /// <returns></returns>
        private Task GenerateTask()
        {
            // TODO: Do away with this and just use the async/await on the main thread
            return Task.Run(async () =>
            {
                await LoadDataAsync();
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

        /// <summary>
        /// Load in the timesheet data from the service
        /// </summary>
        /// <param name="showAll"></param>
        private async Task LoadDataAsync()
        {
            // Get ALL timesheets for the user, then filter stuff out based the state of the ShowAll switch. 
            myTimesheets = new List<Timesheet>();
            if (ActiveUser?.Person != null)
            {
                myTimesheets = TimesheetService.GetMyTimesheets(Context, ActiveUser?.Person).OrderByDescending(t => t.StartDate).ToList();

                // Set the start date for the next one
                dateNextTimesheet = TimesheetService.GetNextTimesheetStartDateForUser(Context, ActiveUser?.Person);

                if (!ShowAllMyTimesheets)
                {
                    // Remove items with Submitted or Approved status
                    myTimesheets = myTimesheets.Where(t => t.Status != TimesheetStatus.Submitted && t.Status != TimesheetStatus.Approved).ToList();
                }
            }

            // Show second grid if user manages staff - need to see the timesheets they have submitted.
            var managedPeople = PersonService.GetManagedStaff(Context, ActiveUser?.Person);

            // If Superuser then _potentially_ they may not manage staff but can see staff synopsis for all staff
            if ((ActiveUserRoleType == RoleType.Superuser) && SuperuserShowSynopsisForAllStaff)
            {
                // Get all staff if switch is selected
                managedPeople = await PersonService.GetAllShallowAsync(Context);
            }

            // Is a manager with staff
            myStaffTimesheets = new List<Timesheet>();
            myStaffTimesheetsInPeriod = new Dictionary<Person, List<Timesheet>>();
            if (managedPeople.Count() > 0)
            {
                dateMondayThisWeek = GetDateForMondayThisWeek();
                synopsisStartDate = GetDateForAMonday(7); // Weeks in the past
                synopsisEndDate = GetDateForAMonday(2, false); // Weeks in the future
                synopsisDates = GetSynopsisDates(synopsisStartDate, synopsisEndDate);

                foreach (Person p in managedPeople
                    .Where(p => p.PersonId != ActiveUser?.Person?.PersonId)
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
    }
}
