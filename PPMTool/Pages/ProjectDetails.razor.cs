using System.Data;
using System.Diagnostics;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using ApexCharts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Helpers;
using PPMTool.Enums;
using PPMTool.Pages.Components;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer,Reader,Finance")]
    public partial class ProjectDetails : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private NoteService NoteService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private EmailService EmailService { get; set; }

        [Inject]
        private IConfiguration Configuration { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private InvoiceService InvoiceService { get; set; }

        [Inject]
        private PaymentService PaymentService { get; set; }

        [Inject]
        private FundingSourceService FundingSourceService { get; set; }

        [Inject]
        private SkillTagService SkillTagService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Parameter]
        public int? ProjectId { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "rtp")]
        public int? RTP { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "filteredNote")]
        public int? FilteredNote { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "filterDueNotes")]
        public bool FilterDueNotes { get; set; }

        private string mentionSearchString = string.Empty;
        public string MentionSearchString
        {
            get => mentionSearchString;
            private set
            {
                if (value != mentionSearchString)
                {
                    mentionSearchString = value;
                    FilterMentionables();
                }
            }
        }

        private List<GanttBlock> confirmedBlocks;
        private List<GanttBlock> provisionalBlocks;
        private List<SubTask> allTasks;
        private IList<SubTask> gridTasks;
        private List<Note> allNotes;
        private Project project;
        private FinanceSummaryItem financeSummaryItem;
        private List<ChartHelper.WeeklyTaskEffort> burnUpChartSource;
        private ApexChartOptions<GanttBlock> ganttChartOptions;
        private ApexChartOptions<ChartHelper.WeeklyTaskEffort> burnUpChartOptions;
        private int count;
        private readonly int gridPageSize = 10;
        private bool isEditExistingNote;
        private bool editorVisible;
        private Note noteModel;
        private IList<Person> mentions;
        private string noteSearchTerms;
        private List<Note> filteredNotes;
        private bool showOnlyFinanceNotes;
        private bool showOnlyDueItems;
        private bool sortByDueDate;
        private Popup popup;
        private IList<Person> mentionables;
        private IList<Person> cachedMentionables;
        private Person highlightedPerson;
        private RadzenHtmlEditor htmlEditor;
        private bool isCurrentUserFollowing;
        private bool isProjectManager;
        private bool groupLinkedTasks;
        private ApexChart<GanttBlock> scheduleChart;
        private IEnumerable<SkillTag> skillsRequiredForProject;
        private bool loadingBurnUpChart = false;
        private bool loadingGanttChart = false;
        IEnumerable<Person> resources = new List<Person>();
        IList<Person> selectedResources = new List<Person>();

        /// <summary>
        /// Fired when the paramters are changed
        /// </summary>
        protected override void OnParametersSet()
        {
            // Set the loading flag and redraw the view while the background task runs
            base.OnParametersSet();

            Debug.WriteLine("** OnParameters!!!!");

            // Fire the load task
            _ = LoadDataAsync();

            Debug.WriteLine($"** Initialised project details");
        }

        /// <summary>
        /// Method to get the background task that does all the intialisation work
        /// </summary>
        /// <returns></returns>
        private async Task LoadDataAsync()
        {
            Debug.WriteLine("** Loading Data...");
            try
            {
                Loading = true;
                StateHasChanged();
                await Task.Yield();

                // Setup initial state
                showOnlyFinanceNotes = ActiveUserRoleType == RoleType.Finance;
                noteModel = new Note
                {
                    IsFinanceInfo = ActiveUserRoleType == RoleType.Finance
                };

                // Reset the search box
                noteSearchTerms = string.Empty;

                // Filter the mentions reset
                cachedMentionables = UserService
                    .GetAll(Context)
                    .Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                    .DistinctBy(x => x.Person)
                    .Select(x => x.Person)
                    .ToList();
                mentionables = cachedMentionables;

                // Query string only consulted when Project ID is not specified in URL
                if (ProjectId == null && RTP != null)
                {
                    // Try get the project
                    ProjectId = ProjectService.GetAll(Context).FirstOrDefault(x => x.RTP == RTP)?.ProjectId;
                }

                // Load the project details if an ID was resolved
                if (ProjectId != null)
                {
                    project = ProjectService.GetAll(Context).FirstOrDefault(x => x.ProjectId == ProjectId);
                    var sources = FundingSourceService.GetAll(Context).Where(x => x.Project.ProjectId == ProjectId);

                    // Generate the list of skills
                    skillsRequiredForProject = SkillTagService.GetSkillsForProject(Context, project.ProjectId);

                    // Generate the funds requested and received
                    var transactions = FinanceHelper.ComputeTransactionBreakdown(
                        Context,
                        project.LeadershipFundingSource?.FundingSourceId ?? 0,
                        project.PlannedLeadershipCosts,
                        project.SubTasks.SelectMany(x => x.AssignedResources),
                        sources,
                        InvoiceService.GetFundsRequested(Context, project.ProjectId),
                        PaymentService.GetFundsReceived(Context, project.ProjectId)
                    );

                    // Generate the finance item for the project
                    financeSummaryItem = new FinanceSummaryItem(
                        project,
                        project.ProjectManager,
                        project.SubTasks?.RoundedSum(x => x.ActualWorkHours) ?? 0,
                        transactions
                    );

                    // Load the task grid
                    allTasks = project.SubTasks.OrderBy(x => x.StartDate).ToList();
                    LoadTaskData(new LoadDataArgs());

                    // Populate the resource dropdown
                    var resourceIds = project.SubTasks
                        .SelectMany(x => x.AssignedResources.Select(x => x.Person.PersonId))
                        .DistinctBy(x => x) ?? new List<int>();
                    var tempResourceNames = new List<Person>();
                    foreach (var id in resourceIds)
                    {
                        var person = PersonService.GetById(Context, id);
                        if (person != null)
                        {
                            tempResourceNames.Add(person);
                        }
                    }
                    resources = tempResourceNames;

                    // Load other parts of the page concurrently
                    await Task.WhenAll(
                        LoadGanttChartAsync(),
                        LoadBurnUpChartAsync(),
                        ConfigureNotesAsync()
                    );
                }

                LogInformation($"Viewing project details for RTP-{project?.RTP}");
            }
            finally
            {
                Debug.WriteLine("** ...Finished Loading Data!");
                Loading = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Fired once the page has been rendered
        /// </summary>
        /// <param name="firstRender"></param>
        /// <returns></returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            // If no project ID set by the time the page is renderered then navigate away
            if (ProjectId == null)
            {
                Navigation.NavigateTo("nothinghere");
                return;
            }

            // If the path is the legacy path then redirect
            if (!Navigation.Uri.Contains("projects/projectdetails"))
            {
                Navigation.NavigateTo(Navigation.Uri.Replace("/projectdetails", "/projects/projectdetails"));
                return;
            }

            if (firstRender)
            {
                Debug.WriteLine("** After Render - first render!");

                // Create a reference to self in JS
                await JSRuntime.InvokeVoidAsync("setDotNetReference", DotNetObjectReference.Create(this));

                // Go fetch the notes (has to be after render as need to scroll to)
                await ConfigureNotesAsync();
            }
        }

        /// <summary>
        /// Configures the note filters and then gets them from the DB applying scroll to as required
        /// </summary>
        /// <returns></returns>
        private async Task ConfigureNotesAsync()
        {
            // After the page has finished rendering then apply the search string from the parameter
            if (FilteredNote != null)
            {
                // Set the search term to filter
                noteSearchTerms = $"#id={FilteredNote}";
            }
            else if (FilterDueNotes)
            {
                showOnlyDueItems = true;
                sortByDueDate = true;
            }

            // Get the notes from the DB
            LoadNotesFromDB();

            // Filter and Highlight
            FilterAndHighlightNotes();

            // Refresh
            StateHasChanged();
            await Task.Yield();

            // Check whether the parameter is present to scroll to the due notes
            if (FilterDueNotes)
            {
                // Refresh then scroll last due note into view
                await Task.Delay(300);
                await JSRuntime.InvokeVoidAsync("scrollToElement", $"note_{filteredNotes.LastOrDefault()?.NoteId}");
            }
        }

        /// <summary>
        /// Method to load the data for the schedule chart
        /// </summary>
        private async Task LoadGanttChartAsync()
        {
            Debug.WriteLine("** Loading Gantt...");
            loadingGanttChart = true;
            await InvokeAsync(StateHasChanged);

            // Generate the blocks for the schedule chart
            var allBlocks = new List<GanttBlock>();
            foreach (var t in allTasks)
            {
                // Initialise as the task name
                var groupName = t.Name;

                if (t.Predecessor != null)
                {
                    // Find predecessor in the existing list
                    var match = allBlocks.FirstOrDefault(x => x.Task.SubTaskId == t.Predecessor.SubTaskId);
                    if (match != null)
                    {
                        groupName = match.PredecessorGroupName;
                    }
                    else
                    {
                        Debug.WriteLine("** Shouldn't be here but predecessor grouping will fail!");
                        LogError("Cannot find predecessor task in temporary list!");
                    }
                }

                // Add to the list of blocks
                allBlocks.Add(new GanttBlock(t, groupName));
            }

            // Add a gantt block representing the management task
            var managementTasks = project.GetLeadershipTaskRanges();
            foreach (var dateRange in managementTasks)
            {
                var leadershipName = "(Leadership)";
                allBlocks.Insert(0, new GanttBlock(new SubTask
                {
                    Name = leadershipName,
                    StartDate = dateRange.StartDate,
                    EndDate = dateRange.EndDate,
                    OwningProject = project,
                    AssignedResources = new List<Resource>
                    {
                        new Resource
                        {
                            Person = project.ProjectManager,
                            AssignmentFTE = Math.Round(project.LeadershipFTE, 3)
                        }
                    }

                }, leadershipName, isLeadershipTask: true));
            }

            // Fill in the data
            ChartHelper.CompleteChartSeries(
                allBlocks,
                c => new GanttBlock(new SubTask() { Name = c.Task.Name, StartDate = DateTime.Today, EndDate = DateTime.Today }, c.PredecessorGroupName, true),
                out confirmedBlocks,
                out provisionalBlocks
            );

            // Update the UI                    
            isCurrentUserFollowing = project.Followers.Any(x => x.Name == ActiveUser?.Name) ||
                project.ProjectManager?.Name == ActiveUser?.Name;
            isProjectManager = ActiveUserRoleType == RoleType.Superuser || (ActiveUserRoleType == RoleType.Manager && ActiveUser?.Person?.PersonId == project?.ProjectManager?.PersonId);

            ganttChartOptions = new ApexChartOptions<GanttBlock>
            {
                Chart = new Chart
                {
                    Zoom = new Zoom
                    {
                        AllowMouseWheelZoom = false
                    }
                },
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = true,
                        RangeBarGroupRows = true
                    }
                },
                Fill = new Fill
                {
                    Opacity = 1,
                    Type = new FillTypeSelections(new FillType[] { FillType.Solid, FillType.Pattern }),
                    Pattern = new FillPattern
                    {
                        Style = new FillPatternStyleSelections(new FillPatternStyle[] { FillPatternStyle.SlantedLines }),
                    }
                },
                Legend = new ApexCharts.Legend
                {
                    Show = false
                },
                Annotations = new Annotations
                {
                    Xaxis = new List<AnnotationsXAxis>
                    {
                        new AnnotationsXAxis()
                        {
                            X = DateTime.Today.ToUnixTimeMilliseconds(),
                            BorderWidth = 2,
                            StrokeDashArray = 5,
                            BorderColor = "red",
                            Label = new Label
                            {
                                Text = "Current Week",
                                Position = LabelPosition.Right
                            }
                        }
                    }
                }
            };

            // Update the Gantt chart axis limits
            UpdateScheduleChartAxisLimits();

            // Reset the flag
            loadingGanttChart = false;
            await InvokeAsync(StateHasChanged);
            Debug.WriteLine("** ...Finished Loading Gantt!");
        }

        /// <summary>
        /// Method to load the burn-up chart -- can be called from a background thread
        /// </summary>
        private async Task LoadBurnUpChartAsync()
        {
            Debug.WriteLine("** Loading Burn-Up...");
            loadingBurnUpChart = true;
            await InvokeAsync(StateHasChanged);

            // Create the burn-up chart items
            burnUpChartSource = new List<ChartHelper.WeeklyTaskEffort>();

            // Get details of weekly effort
            var temp = ChartHelper.GetWeeklyTaskEffortItems(project.SubTasks).ToList();
            Debug.WriteLine($"** Returned {temp.Count} weeks of items; {temp.FirstOrDefault()?.ResourceEffort.Count} unqiue resources.");

            // Generate burn-up series by aggregating the values
            for (var i = 0; i < temp.Count; ++i)
            {
                var lastWeek = i == 0 ? null : burnUpChartSource[i - 1];
                var thisWeek = temp[i];
                burnUpChartSource.Add(new ChartHelper.WeeklyTaskEffort(thisWeek, lastWeek));
            }

            // Early exit if chartSource has no data
            if (burnUpChartSource.Count < 1) return;

            // Create a new data point to indicate progress
            var seriesStart = burnUpChartSource.Min(x => x.WeekDate);
            var seriesEnd = burnUpChartSource.Max(x => x.WeekDate);
            var todayLine = DateTime.Today;

            // If the task has started yet or has already finished then x coordinate is the limits of the series
            if (DateTime.Today < seriesStart) todayLine = seriesStart;
            else if (DateTime.Today > seriesEnd) todayLine = seriesEnd;

            // Set options
            burnUpChartOptions = new ApexChartOptions<ChartHelper.WeeklyTaskEffort>
            {
                Chart = new Chart
                {
                    Zoom = new Zoom
                    {
                        AllowMouseWheelZoom = false,
                        Enabled = true,
                        Type = AxisType.Xy
                    }
                },
                Stroke = new Stroke
                {
                    Curve = new CurveSelections([Curve.Straight])
                },
                Annotations = new Annotations
                {
                    Xaxis = new List<AnnotationsXAxis>
                    {
                        new AnnotationsXAxis()
                        {
                            X = todayLine.ToUnixTimeMilliseconds(),
                            BorderWidth = 2,
                            StrokeDashArray = 3,
                            BorderColor = "black",
                            Label = new Label
                            {
                                Text = "Current Week",
                                Position = LabelPosition.Right
                            }
                        }
                    }
                },
                Xaxis = new XAxis
                {
                    Title = new AxisTitle { Text = "Week Beginning" }
                },
                Yaxis = new List<YAxis>
                {
                    new YAxis { Title = new AxisTitle { Text = "Work (Hours)" } }
                }
            };

            loadingBurnUpChart = false;
            await InvokeAsync(StateHasChanged);
            Debug.WriteLine("** ...Finished Loading Burn-Up!");
        }

        /// <summary>
        /// Callback for when the values of the dropdown are changed
        /// </summary>
        /// <param name="values"></param>
        private void ResourceSelectionChanged(object values)
        {
            var personIds = values as IEnumerable<int>;
            selectedResources = new List<Person>();
            if (personIds != null)
            {
                foreach (var id in personIds)
                {
                    // Find the person by ID
                    var person = resources.First(x => x.PersonId == id);
                    selectedResources.Add(person);
                }
            }

            // Reload the chart
            Task.Run(LoadBurnUpChartAsync);
        }

        /// <summary>
        /// Handler for switching the group linked tasks option
        /// </summary>
        /// <param name="value"></param>
        private void GroupTasksChanged(bool value)
        {
            UpdateScheduleChartAxisLimits();

            // Redraw the chart
            scheduleChart?.RenderAsync();
        }

        /// <summary>
        /// Updates the schedule chart axis limits as switching between grouped and ungrouped doesn't auto update properly
        /// </summary>
        private void UpdateScheduleChartAxisLimits()
        {
            var allBlocks = confirmedBlocks.Concat(provisionalBlocks).Where(x => !x.IsFake());

            // Set the axis limits
            ganttChartOptions.Yaxis = new List<YAxis>
            {
                new YAxis
                {
                    Min = allBlocks.Count() == 0 ? null : allBlocks.Min(x => x.Task.StartDate).ToUnixTimeMilliseconds(),
                    Max = allBlocks.Count() == 0 ? null : allBlocks.Max(x => x.Task.EndDate).ToUnixTimeMilliseconds()
                }
            };
            scheduleChart?.UpdateOptionsAsync(false, false, false);
        }

        /// <summary>
        /// Toggle the following status by adding or removing the current acitve user to the project's follower list
        /// </summary>
        private void ToggleFollowing()
        {
            if (ActiveUser == null) return;
            if (project.Followers.Contains(ActiveUser?.Person))
            {
                project.Followers.Remove(ActiveUser?.Person);
                ProjectService.Update(Context, project);
                isCurrentUserFollowing = false;
                LogInformation($"Stopped following project {project.GetFullName()}");
            }
            else
            {
                project.Followers.Add(ActiveUser?.Person);
                ProjectService.Update(Context, project);
                isCurrentUserFollowing = true;
                LogInformation($"Now following project {project.GetFullName()}");
            }
            StateHasChanged();
        }

        /// <summary>
        /// Stores a reference to the person associated with a current mention
        /// </summary>
        /// <param name="person"></param>
        private void HighlightMention(Person person)
        {
            highlightedPerson = person;
        }

        /// <summary>
        /// Resets the reference to the person associated with a current mention
        /// </summary>
        /// <param name="person"></param>
        private void UnHighlightMention(Person person)
        {
            highlightedPerson = null;
        }

        /// <summary>
        /// Filters the mentionables list based on the search string.
        /// </summary>
        private void FilterMentionables()
        {
            if (string.IsNullOrWhiteSpace(mentionSearchString))
            {
                mentionables = cachedMentionables;
            }
            else
            {
                mentionables = cachedMentionables
                    .Where(x => x.Name.ToLower().Contains(mentionSearchString.ToLower()) || x.ShortName.ToLower().StartsWith(mentionSearchString.ToLower()))
                    .ToList();
            }
            highlightedPerson = mentionables.FirstOrDefault();
            Debug.WriteLine($"** Filtered mentionables based on \"{mentionSearchString}\" giving {mentionables.Count} results.");
        }

        /// <summary>
        /// Handle changes in the HTML editor
        /// </summary>
        /// <param name="args"></param>
        private void ProcessEditorInput(KeyboardEventArgs args)
        {
            Debug.WriteLine($"** Key pressed in the editor \"{args.Key}\"");

            // If it is a mention trigger but not a mention insertion then open the popup
            if (args.Key == "@")
            {
                Debug.WriteLine($"** Opening popup...");

                // Save cursor position
                htmlEditor.SaveSelectionAsync().ContinueWith(async t =>
                {
                    // Open the popup if not already open
                    await InvokeAsync(async () =>
                    {
                        await popup.ToggleAsync(htmlEditor.Element);
                        StateHasChanged();
                    });
                });
            }
        }

        /// <summary>
        /// Handle key presses while the mention popup is visible
        /// </summary>
        /// <param name="args"></param>
        private void ProcessMentionSearchInput(KeyboardEventArgs args)
        {
            if (args.Key == "Escape")
            {
                MentionPerson(null);
            }
            else if (args.Key == "Enter" || args.Key == "Tab")
            {
                MentionPerson(highlightedPerson);
            }
            else if (args.Key == "ArrowDown")
            {
                var currentIndex = mentionables.IndexOf(highlightedPerson);
                if (currentIndex < mentionables.Count - 1)
                {
                    highlightedPerson = mentionables[currentIndex + 1];
                }
            }
            else if (args.Key == "ArrowUp")
            {
                var currentIndex = mentionables.IndexOf(highlightedPerson);
                if (currentIndex > 0)
                {
                    highlightedPerson = mentionables[currentIndex - 1];
                }
            }
        }

        /// <summary>
        /// Open the mention popup via JS
        /// </summary>
        /// <returns></returns>
        private async Task OnMentionPopupOpenAsync()
        {
            // Focus on the search box
            await JSRuntime.InvokeVoidAsync("eval", "setTimeout(function(){ document.getElementById('search').focus(); }, 200)");
        }

        /// <summary>
        /// Insert the initials of the selected person via JS
        /// </summary>
        /// <param name="person"></param>
        private void MentionPerson(Person person)
        {
            htmlEditor.RestoreSelectionAsync().ContinueWith(async t =>
            {
                // Close the popup
                MentionSearchString = string.Empty;
                await popup.CloseAsync();

                // Insert text and move cursor
                await JSRuntime.InvokeVoidAsync("insertTextAtCaret", $"{person?.ShortName ?? ""}");
            });
        }

        /// <summary>
        /// Invoked when the notes filter switch is toggled
        /// </summary>
        private void FilterSwitchToggled()
        {
            LoadNotesFromDB();
            FilterAndHighlightNotes();
        }

        /// <summary>
        /// Populates the notes to be show in the list
        /// </summary>
        private void LoadNotesFromDB()
        {
            Debug.WriteLine("** Populating notes...");
            allNotes = NoteService.GetAll(Context).Where(x => x.Project.ProjectId == ProjectId).ToList();
            if (showOnlyFinanceNotes) allNotes = allNotes.Where(x => x.IsFinanceInfo).ToList();
            if (showOnlyDueItems) allNotes = allNotes.Where(x => x.IsDue() || x.IsOverDue()).ToList();
            if (sortByDueDate) allNotes = allNotes.Where(x => x.DueDate != null).OrderBy(x => x.DueDate).Concat(allNotes.Where(x => x.DueDate == null)).ToList();
            filteredNotes = allNotes;
        }

        /// <summary>
        /// Filters the notes in the list via JS and also applies text highlighting if searching
        /// </summary>
        private void FilterAndHighlightNotes()
        {
            Debug.WriteLine("** Filtering / Highlighting notes...");

            // Clear existing highlighting
            InvokeAsync(async () =>
            {
                await JSRuntime.InvokeVoidAsync("clearHighlightInNotes");
            }).ContinueWith(async t =>
            {
                await InvokeAsync(async () =>
                {
                    // Wait for JS to finish
                    await Task.Delay(500);

                    // No search terms so show all
                    if (string.IsNullOrWhiteSpace(noteSearchTerms))
                    {
                        filteredNotes = allNotes;
                        Debug.WriteLine($"** Notes reset");
                        StateHasChanged();
                    }

                    // Search terms are present
                    else
                    {
                        // Search by DB ID (useful for resolving links)
                        if (noteSearchTerms.StartsWith("#id=") && noteSearchTerms.Length > 4 && int.TryParse(noteSearchTerms.Substring(4), out int noteId))
                        {
                            filteredNotes = allNotes.Where(x => x.NoteId == noteId).ToList();
                            Debug.WriteLine($"** Filtered based on ID {noteId} giving {filteredNotes.Count} notes.");

                            // Re-render then scroll to note
                            StateHasChanged();
                            await Task.Delay(300);
                            await JSRuntime.InvokeVoidAsync("scrollToElement", $"note_{noteId}");
                        }
                        else
                        {
                            // Filter based on the search terms (plain text content)
                            filteredNotes = allNotes.Where(x =>
                            {
                                var plainText = HtmlHelper.ConvertToPlainText(x.HtmlContent);
                                return plainText.ToLower().Contains(noteSearchTerms.Trim().ToLower());
                            }).ToList();

                            Debug.WriteLine($"** Filtered based on \"{noteSearchTerms}\" giving {filteredNotes.Count} notes.");

                            // Re-render the view
                            StateHasChanged();
                            await Task.Delay(500);

                            // Call highlighter JS function
                            await JSRuntime.InvokeVoidAsync("highlightInNotes", noteSearchTerms.Trim());
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Clears the search terms and resets the filter
        /// </summary>
        private void ClearSearch()
        {
            noteSearchTerms = string.Empty;
            FilterAndHighlightNotes();
        }

        /// <summary>
        /// Shows or hides the HTML editor div with scrolling to the editor via JS
        /// </summary>
        /// <param name="show"></param>
        private async void ShowOrHideEditor(bool show)
        {
            // Set visibility
            editorVisible = show;

            if (editorVisible)
            {
                // Scroll to the new editor window after a delay to allow the page to render
                await Task.Delay(300);
                await JSRuntime.InvokeVoidAsync("scrollToElement", "note-editor");

            }
            StateHasChanged();

            // Needs to be called after state has changed
            if (editorVisible)
            {
                await htmlEditor.FocusAsync();
            }
        }

        /// <summary>
        /// Navigate to the edit project page
        /// </summary>
        /// <param name="project"></param>
        private void EditProject(Project project)
        {
            Navigation.NavigateTo($"projects/addproject/{project.ProjectId}");
        }

        /// <summary>
        /// Navigate to the finance page for that project
        /// </summary>
        /// <param name="project"></param>
        private void EditFinance(Project project)
        {
            Navigation.NavigateTo($"managefinancialitems?rtp={project?.RTP}");
        }

        /// <summary>
        /// Handles the add note button click
        /// </summary>
        private void AddClicked()
        {
            noteModel = new Note
            {
                IsFinanceInfo = ActiveUserRoleType == RoleType.Finance
            };
            isEditExistingNote = false;
            ShowOrHideEditor(true);
        }

        /// <summary>
        /// Handles the note edit discard button
        /// </summary>
        private void DiscardClicked()
        {
            LogInformation($"Discarding changes to note {noteModel?.NoteId} on {project.GetFullName()}");
            if (isEditExistingNote)
            {
                NoteService.RestoreModel(Context, ref noteModel);
            }
            isEditExistingNote = false;
            LoadNotesFromDB();
            FilterAndHighlightNotes();
            ShowOrHideEditor(false);
        }

        /// <summary>
        /// Saves a new note to the database and hides the editor
        /// </summary>
        private async Task SaveNoteAsync()
        {
            Debug.WriteLine("** SAVING NOTE!!! ");

            if (project == null || project.ProjectId < 0)
            {
                ShowOrHideEditor(false);
                LogError("Attempt to add a note when no project model present!");
                return;
            }

            // Populate model and add to DB
            noteModel.Project = project;
            noteModel.Author = ActiveUser;
            noteModel.CreatedDate = DateTime.Now;
            ResolveMentionsInCurrentNoteModel();
            NoteService.Add(Context, noteModel);
            LogInformation($"Added note for {project.GetFullName()}");
            noteSearchTerms = string.Empty;
            LoadNotesFromDB();
            FilterAndHighlightNotes();
            ShowOrHideEditor(false);
            await EmailService.SendMentionAndOwnerEmailNotificationsAsync(noteModel, mentions);
        }

        /// <summary>
        /// Updates an existing note in the DB and hides the editor
        /// </summary>
        private async Task UpdateNoteAsync()
        {
            // Update model in DB
            noteModel.EditedDate = DateTime.Now;
            noteModel.Editor = ActiveUser;
            ResolveMentionsInCurrentNoteModel();
            NoteService.Update(Context, noteModel, false);
            var listOfNoteChanges = NoteService.GetDiffList<Note>(Context);
            NoteService.Update(Context, noteModel, true);
            LogInformation($"Updated note {noteModel.NoteId} for {project.GetFullName()}");
            LoadNotesFromDB();
            FilterAndHighlightNotes();
            ShowOrHideEditor(false);
            await EmailService.SendMentionAndOwnerEmailNotificationsAsync(noteModel, mentions, listOfNoteChanges);
        }

        /// <summary>
        /// Enters note edit mode
        /// </summary>
        /// <param name="noteToEdit"></param>
        private void EditNote(Note noteToEdit)
        {
            // Remove the note from the list so it doesn't confuse the user
            filteredNotes.Remove(noteToEdit);

            // Set state
            ShowOrHideEditor(true);
            LogInformation($"Editing note {noteModel.NoteId} for {project.GetFullName()}");
            noteModel = noteToEdit;
            isEditExistingNote = true;
        }

        /// <summary>
        /// Deletes a note
        /// </summary>
        /// <param name="noteToDelete"></param>
        private async void DeleteNote(Note noteToDelete)
        {
            bool confirmed = await DialogService.Confirm($"You are about to delete a note from {project.GetFullName()}!", "Delete Note") ?? false;
            if (confirmed)
            {
                LogInformation($"Deleting note {noteToDelete.NoteId} | {noteToDelete.HtmlContent} | {noteToDelete.GetNoteAuthorText()}");
                NoteService.Delete(Context, noteToDelete);
                LoadNotesFromDB();
                FilterAndHighlightNotes();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Generates the absolute URL which filters the notes to the one selected via JS
        /// </summary>
        /// <param name="noteTolink"></param>
        private string GetCopyLinkText(Note noteTolink)
        {
            return $"{Configuration["Authentication:HostUrl"]}/projects/projectdetails/{project.ProjectId}?filteredNote={noteTolink.NoteId}";
        }

        /// <summary>
        /// Marks a due date flag on a note as complete
        /// </summary>
        /// <param name="note"></param>
        private void MarkComplete(Note note)
        {
            LogInformation($"Completing note {note.NoteId} for {project.GetFullName()}");
            note.CompletedDate = DateTime.Now;
            NoteService.Update(Context, note);
            StateHasChanged();
        }

        /// <summary>
        /// Attempts to resolve the mentions and links in the note content for the current note model.
        /// </summary>
        private void ResolveMentionsInCurrentNoteModel()
        {
            Debug.WriteLine($"** Content Resolve: {noteModel.HtmlContent}");

            // Get list of all new mentions in the note content
            var newMentions = new List<string>();
            var matches = Regex.Matches(noteModel.HtmlContent, @"(>|^|\s)@\w+");
            newMentions.AddRange(matches.Select(x => x.Value.Trim()).Distinct());

            // Load in the list of managers
            var managers = UserService.GetAll(Context).Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser).Select(x => x.Person).ToList();

            // For each mention, attempt to resolve it and replace in the HTMl content
            foreach (Match m in matches)
            {
                var trimmedMatch = TrimMatch(m.Value.Trim(), '@');
                var person = managers.FirstOrDefault(x => x.ShortName.Equals(trimmedMatch.Substring(1), StringComparison.OrdinalIgnoreCase));
                if (person != null)
                {
                    Debug.WriteLine($"** Replacing {trimmedMatch} with {person.Name}");
                    noteModel.HtmlContent = noteModel.HtmlContent.Replace(trimmedMatch, $"&nbsp;<span class=\"badge badge-primary\">{person.Name}</span>&nbsp;");
                }
                else
                {
                    // Warning if the mention cannot be resolved
                    ShowNotification(new CapXNotificationMessage
                    {
                        Summary = "Mention Failure",
                        Detail = $"The mention {trimmedMatch} could not be resolved! Please edit your note to correct."
                    });
                }
            }

            // Update the mentions list (for notifications) by extracting the formatted tags
            var resolvedMentions = new List<string>();
            matches = Regex.Matches(noteModel.HtmlContent, @"<span class=""badge badge-primary"">(.*?)<\/span>");
            resolvedMentions.AddRange(matches.Select(x => x.Groups[1].Value.Trim()).Distinct());
            mentions = new List<Person>();
            foreach (var m in resolvedMentions)
            {
                // Extract the name from the match
                var match = managers.FirstOrDefault(x => x.Name.Equals(m, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    mentions.Add(match);
                }
            }

            // Get list of all new RTP-XXX references in the note content
            var newRtpRefs = new List<string>();
            matches = Regex.Matches(noteModel.HtmlContent, @"(>|^|\s)#RTP-\w+(\s|$)", RegexOptions.IgnoreCase);
            newRtpRefs.AddRange(matches.Select(x => x.Value.Trim()).Distinct());

            // For each reference, attempt to resolve it and replace in the HTMl content
            foreach (var r in newRtpRefs)
            {
                var trimmedMatch = TrimMatch(r, '#');
                var match = ProjectService.GetAllShallow(Context)
                    .FirstOrDefault(x => x.RTP.ToString().Equals(trimmedMatch.Substring(5), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    noteModel.HtmlContent = noteModel.HtmlContent.Replace(trimmedMatch, $"&nbsp;<a href=\"{Configuration["Authentication:HostUrl"]}/projects/projectdetails/{match.ProjectId}\" class=\"badge badge-success\">{match.GetFullName()}</a>&nbsp;");
                }
                else
                {
                    // Warning if the reference cannot be resolved
                    ShowNotification(new CapXNotificationMessage
                    {
                        Summary = "RTP Reference Failure",
                        Detail = $"The reference {trimmedMatch} could not be resolved! Please edit your note to correct."
                    });
                }
            }
        }

        /// <summary>
        /// Method to trim the matches to remove their preceding characters if necessary
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private string TrimMatch(string match, char delimiter)
        {
            if (match.StartsWith(">") || char.IsWhiteSpace(match[0]))
            {
                int atIndex = match.IndexOf(delimiter);
                if (atIndex != -1)
                {
                    return match.Substring(atIndex);
                }
            }
            return match;
        }

        /// <summary>
        /// Handler for when a task is selected in the schedule chart -- navigates to the edit task page
        /// </summary>
        /// <param name="dataPoint"></param>
        private void TaskSelected(SelectedData<GanttBlock> dataPoint)
        {
            if (!EditAuthorised || (dataPoint.DataPoint.Items.FirstOrDefault()?.IsLeadershipTask ?? true)) return;

            // Only so the navigation when in project view mode
            if (dataPoint.IsSelected)
            {
                var task = dataPoint.DataPoint.Items.FirstOrDefault()?.Task;
                if (task == null) return;
                Debug.WriteLine($"** Selected {task.Name}. Navigating to task edit page...");
                EditTask(task);
            }
        }

        /// <summary>
        /// Navigates to the edit task page
        /// </summary>
        /// <param name="task"></param>
        void EditTask(SubTask task)
        {
            Navigation.NavigateTo($"projects/addtask/{project.ProjectId}/{task.SubTaskId}");
        }

        /// <summary>
        /// Navigates to the add task page
        /// </summary>
        void AddTask()
        {
            Navigation.NavigateTo($"projects/addtask/{project.ProjectId}/-1");
        }

        /// <summary>
        /// Navigates to the add task page with the copy parameter set
        /// </summary>
        /// <param name="task"></param>
        void CopyTask(SubTask task)
        {
            // Navigate to the add task page passing the task ID to be copied and the query string parameter to indicate it is a copy
            Navigation.NavigateTo($"projects/addtask/{project.ProjectId}/{task.SubTaskId}?copy=true");
        }

        /// <summary>
        /// Navigates to the split task page
        /// </summary>
        /// <param name="task"></param>
        void SplitTask(SubTask task)
        {
            // Navigate to the split task page passing the task ID to be split
            Navigation.NavigateTo($"projects/splittask/{project.ProjectId}/{task.SubTaskId}");
        }

        /// <summary>
        /// Loads the task data grid content. Necessary to ensure that we can filter the resources on the fly.
        /// </summary>
        /// <param name="args"></param>
        private void LoadTaskData(LoadDataArgs args)
        {
            Debug.WriteLine("** Loading the tasks in the grid...");

            var query = project.SubTasks.ToList().AsQueryable();

            if (!string.IsNullOrEmpty(args.Filter))
            {
                // Filter via the Where method
                query = query.Where(args.Filter);
            }

            // Now apply the resources filter
            if (args.Filters != null && args.Filters.Count() > 0)
            {
                var filter = args.Filters.FirstOrDefault(x => x.Property == "Resources");
                var filterValue = filter?.FilterValue as string;
                if (filter != null && filterValue != null)
                {
                    query = query.Where(x => x.AssignedResources.Any(x => x.Person.ShortName.Contains(filterValue)));
                }
            }

            if (!string.IsNullOrEmpty(args.OrderBy))
            {
                // Sort via the OrderBy method
                query = query.OrderBy(args.OrderBy);
            }

            // Important!!! Make sure the Count property of RadzenDataGrid is set.
            count = query.Count();

            // Perform paging
            gridTasks = query.Skip(args.Skip ?? 0).Take(args.Top ?? gridPageSize).ToList();

        }

        /// <summary>
        /// Show dialog popup of the project description
        /// </summary>
        private async Task ViewDescription()
        {
            await DialogService.OpenAsync<ProjectDescriptionPopupComponent>(project?.GetFullName(), new Dictionary<string, object>() { { "Project", project } });
        }

        /// <summary>
        /// Resets the actuals timestamp after a prompt
        /// </summary>
        private async void ResetActualsTimeStamp()
        {
            // Prompt
            bool confirmed = await DialogService.Confirm($"By clicking this button you are confirming that you have checked the actuals against timesheet data. This will silence any warning about out-of-date actuals for a month. This cannot be undone!",
                "Have you checked the actuals?") ?? false;
            if (confirmed)
            {
                LogInformation($"Silencing actuals warning for {project?.GetFullName()}");

                // Set timestamp and save to DB
                project.ActualsLastUpdated = DateTime.Now.ToString("R");
                ProjectService.Update(Context, project);
                StateHasChanged();
            }
        }
    }
}
