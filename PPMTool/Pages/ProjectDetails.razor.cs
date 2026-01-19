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
        private IJSRuntime JS { get; set; }

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

        // Chart stuff
        private List<GanttBlock> confirmedBlocks;
        private List<GanttBlock> provisionalBlocks;
        private List<ChartHelper.WeeklyTaskEffort> burnUpChartSource;
        private ApexChartOptions<GanttBlock> ganttChartOptions;
        private ApexChartOptions<ChartHelper.WeeklyTaskEffort> burnUpChartOptions;
        private bool groupLinkedTasks;
        private ApexChart<GanttBlock> scheduleChart;
        private bool loadingBurnUpChart = false;
        IEnumerable<Person> resources = new List<Person>();
        IList<Person> selectedResources = new List<Person>();

        // Basics
        private Project project;
        private FinanceSummaryItem financeSummaryItem;
        private bool isCurrentUserFollowing;
        private bool isProjectManager;
        private IEnumerable<SkillTag> skillsRequiredForProject;

        // Task grid
        private int count;
        private readonly int gridPageSize = 10;
        private List<SubTask> allTasks;
        private IList<SubTask> gridTasks;

        // Notes
        private List<Note> allNotes;
        private bool isEditExistingNote;
        private bool editorVisible;
        private Note noteModel;
        private IList<Person> mentions;
        private string noteSearchTerms;
        private List<Note> filteredNotes;
        private bool showOnlyFinanceNotes;
        private bool showOnlyDueItems;
        private bool sortByDueDate;
        private MentionState mention = new();
        private IList<Person> mentionables;
        private IList<Person> cachedMentionables;
        private RadzenHtmlEditor htmlEditor;
        private bool bound;
        private bool suppressNextInput;

        // Loading parameter cache
        private int? lastRTP;
        private int? lastId;
        private int? lastNote;
        private bool lastDue;
        private CancellationTokenSource loadCts;

        /// <summary>
        /// Mention state
        /// </summary>
        private class MentionState
        {
            public bool Visible { get; set; }
            public char? Trigger { get; set; } = '@';
            public string Query { get; set; } = string.Empty;
            public string TopPx { get; set; } = "0px";
            public string LeftPx { get; set; } = "0px";
            public int? HighlightedId { get; set; }
            public List<Person> FilteredPeople { get; set; } = new();
        }


        /// <summary>
        ///  Container for JS interop result
        /// </summary>
        private class TokenInfo
        {
            public bool HasTrigger { get; set; }
            public char Trigger { get; set; }
            public string Text { get; set; }
            public double ClientTop { get; set; }
            public double ClientLeft { get; set; }
            public double ClientHeight { get; set; }
        }


        /// <summary>
        /// Fired when the paramters are changed
        /// </summary>
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            // Detect parameter change safely
            bool changed =
                lastRTP != RTP ||
                lastId != ProjectId ||
                lastNote != FilteredNote ||
                lastDue != FilterDueNotes;

            Debug.WriteLine($"** [Project Details] OnParameters fired - changed={changed}");

            if (!changed)
                return;

            // Cancel any in-flight loads
            loadCts?.Cancel();
            loadCts = new CancellationTokenSource();

            // Snapshot parameters (null-safe)
            lastRTP = RTP;
            lastId = ProjectId;
            lastNote = FilteredNote;
            lastDue = FilterDueNotes;

            try
            {
                await LoadDataAsync(loadCts.Token);

                if (ProjectId is null)
                {
                    Navigation.NavigateTo("nothinghere");
                    return;
                }

                if (!Navigation.Uri.Contains("projects/projectdetails"))
                {
                    Navigation.NavigateTo(Navigation.Uri.Replace("/projectdetails", "/projects/projectdetails"));
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("** Load cancelled");
            }
        }

        /// <summary>
        /// Method to get the background task that does all the intialisation work
        /// </summary>
        /// <returns></returns>
        private async Task LoadDataAsync(CancellationToken ct)
        {
            Debug.WriteLine("** [Project Details] Loading Data...");
            try
            {
                Loading = true;
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

                // Check if we can proceed
                if (ct.IsCancellationRequested) return;

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

                    // Check if we can proceed
                    if (ct.IsCancellationRequested) return;

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

                    // Check if we can proceed
                    if (ct.IsCancellationRequested) return;

                    // Go fetch the notes
                    LoadNotes();

                    // Check if we can proceed
                    if (ct.IsCancellationRequested) return;

                    // Load charts
                    LoadGanttChart();
                    LoadBurnUpChart();
                }

                LogInformation($"Viewing project details for RTP-{project?.RTP}");
            }
            finally
            {
                Debug.WriteLine("** [Project Details] ...Finished Loading Data!");
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

            if (firstRender)
            {
                Debug.WriteLine("** [Project Details] After Render - first render!");

                // Create a reference to self in JS
                await JS.InvokeVoidAsync("setDotNetReference", DotNetObjectReference.Create(this));

                // Bind keydown event
                if (!bound)
                {
                    bound = true;
                    await JS.InvokeVoidAsync("mentions.bindKeydown", "#editor-entry");
                }

                // Do the JS highlighting and scrolling if required
                await FilterHighlightScrollNotesAsync();
            }
        }

        /// <summary>
        /// Loads notes from the DB based on the state of the filter switches
        /// </summary>
        /// <returns></returns>
        private void LoadNotes()
        {
            // Set defaults applying from the parameters later
            showOnlyDueItems = false;
            sortByDueDate = false;

            // Set the switches based on the parameters
            if (FilterDueNotes)
            {
                showOnlyDueItems = true;
                sortByDueDate = true;
            }

            // If parameter is present to filter to a specific note then set in search box
            // if the search box is empty
            if (string.IsNullOrWhiteSpace(noteSearchTerms) && FilteredNote != null)
            {
                // Set the search term to filter
                noteSearchTerms = $"#id={FilteredNote}";
            }

            // Get the notes from the DB based on what is needed
            LoadNotesFromDB();
        }

        /// <summary>
        /// Method to filter the notes and invoke JS to based on the search terms to highlight and scroll to notes as required
        /// </summary>
        private async Task FilterHighlightScrollNotesAsync()
        {
            // Clear any existing highlights
            await JS.InvokeVoidAsync("clearHighlightInNotes");
            await Task.Delay(500);

            // No search terms so show all
            if (string.IsNullOrWhiteSpace(noteSearchTerms))
            {
                filteredNotes = allNotes;
                Debug.WriteLine($"** Notes reset");
                StateHasChanged();
                await Task.Yield();
            }

            // Search terms are present so filter the list content
            else
            {
                // Search by DB ID (useful for resolving links)
                if (noteSearchTerms.StartsWith("#id=") && noteSearchTerms.Length > 4 && int.TryParse(noteSearchTerms.Substring(4), out int noteId))
                {
                    filteredNotes = allNotes.Where(x => x.NoteId == noteId).ToList();
                    Debug.WriteLine($"** Filtered based on ID {noteId} giving {filteredNotes.Count} notes.");

                    // Re-render the view and allow a redraw by yiedling
                    StateHasChanged();
                    await Task.Yield();

                    // Call JS function to scroll to the note based on what should now be displayed
                    await JS.InvokeVoidAsync("scrollToElement", $"note_{noteId}");
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

                    // Re-render the view and allow a redraw by yiedling
                    StateHasChanged();
                    await Task.Yield();

                    // Call JS function to highlight the terms of what now will be displayed
                    await JS.InvokeVoidAsync("highlightInNotes", noteSearchTerms.Trim());
                    await Task.Delay(500);
                }
            }

            // Check whether the parameter is present to scroll to the due notes
            if (FilterDueNotes)
            {
                // Refresh then scroll last due note into view
                await Task.Delay(300);
                await JS.InvokeVoidAsync("scrollToElement", $"note_{filteredNotes.LastOrDefault()?.NoteId}");
            }
        }

        /// <summary>
        /// Method to load the data for the schedule chart
        /// </summary>
        private void LoadGanttChart()
        {
            Debug.WriteLine("** [Project Details] Loading Gantt...");

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
            var managementTasks = project.GenerateLeadershipTasks();
            foreach (var task in managementTasks)
            {
                var leadershipName = "(Leadership)";
                allBlocks.Insert(0, new GanttBlock(task, leadershipName, isLeadershipTask: true));
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
            Debug.WriteLine("** [Project Details] ...Finished Loading Gantt!");
        }

        /// <summary>
        /// Method to load the burn-up chart
        /// </summary>
        private void LoadBurnUpChart()
        {
            Debug.WriteLine("** [Project Details] Loading Burn-Up...");

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

            Debug.WriteLine("** [Project Details] ...Finished Loading Burn-Up!");
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
        /// Callback for when the values of the dropdown are changed
        /// </summary>
        /// <param name="values"></param>
        private async Task ResourceSelectionChangedAsync(object values)
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
            loadingBurnUpChart = true;
            await Task.Yield();
            LoadBurnUpChart();
            loadingBurnUpChart = false;
            StateHasChanged();
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


        // --------------------- New stuff ----------------------- //

        private async Task OnEditorInput(string html)
        {
            if (suppressNextInput)
            {
                suppressNextInput = false;
                return; // ignore the input triggered by our InsertHtml
            }

            // Ask JS for the current token and caret position
            var info = await JS.InvokeAsync<TokenInfo>("mentions.getTokenInfo", "#editor-entry", "@,#");

            if (info?.HasTrigger == true)
            {
                mention.Trigger = info.Trigger;
                mention.Query = info.Text ?? string.Empty;

                // Filter your people list (case-insensitive initials, name, etc.)
                mention.FilteredPeople = FilterPeople(mention.Query);

                // Position the panel
                mention.TopPx = $"{info.ClientTop + info.ClientHeight}px";
                mention.LeftPx = $"{info.ClientLeft}px";

                mention.Visible = mention.FilteredPeople.Count > 0;
                mention.HighlightedId = mention.FilteredPeople.FirstOrDefault()?.PersonId;

                // Tell JS whether to suppress keys
                await SetMentionActiveAsync(mention.Visible);

            }
            else
            {
                await HideMentionPanelAsync();
            }

            StateHasChanged();
        }

        private async Task OnEditorKeyDown(KeyboardEventArgs e)
        {
            if (!mention.Visible)
            {
                // Start mention on '@' or '#'
                if (e.Key is "@" or "#")
                {
                    // Save caret so we can restore/replace safely if focus ever moves
                    await htmlEditor!.SaveSelectionAsync();

                    // Prime the popup at caret even before any query chars
                    var info = await JS.InvokeAsync<TokenInfo>("mentions.getTokenInfo", "#editor-entry", "@,#");
                    mention.Trigger = e.Key[0];
                    mention.Query = string.Empty;
                    mention.FilteredPeople = FilterPeople(""); // show top N
                    mention.TopPx = $"{info.ClientTop + info.ClientHeight}px";
                    mention.LeftPx = $"{info.ClientLeft}px";
                    mention.Visible = mention.FilteredPeople.Count > 0;
                    mention.HighlightedId = mention.FilteredPeople.FirstOrDefault()?.PersonId;

                    StateHasChanged();
                }
                return;
            }

            // When popup is visible, handle navigation/selection
            switch (e.Key)
            {
                case "ArrowDown":
                    MoveHighlight(1); break;
                case "ArrowUp":
                    MoveHighlight(-1); break;
                case "Enter":
                case "Tab":
                    if (TryGetHighlighted(out var person))
                    {
                        await SelectMention(person);
                        return;
                    }
                    break;
                case "Escape":
                    await HideMentionPanelAsync();
                    StateHasChanged();
                    break;
            }
        }

        private async Task SelectMention(Person p)
        {
            // Replace from trigger to caret, then insert semantic markup via Radzen API
            await JS.InvokeVoidAsync("mentions.selectFromTriggerToCaret", "#editor-entry", mention.Trigger?.ToString() ?? "@");

            var initials = p.ShortName ?? string.Empty;
            var markup = $"<span class=\"mention\" data-id=\"{p.PersonId}\">@{initials}</span>&nbsp;";

            // Prevent the follow-up Input from re-triggering
            suppressNextInput = true;

            await htmlEditor!.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, markup);

            await HideMentionPanelAsync();
            StateHasChanged();
        }

        private async Task HideMentionPanelAsync()
        {
            mention.Visible = false;
            mention.Trigger = null;
            mention.Query = string.Empty;
            mention.HighlightedId = null;
            mention.FilteredPeople.Clear();
            await SetMentionActiveAsync(false);
        }

        private List<Person> FilterPeople(string q)
        {
            q ??= string.Empty;
            var query = q.Trim();

            // Filter based on the query being in the name or short name
            return mentionables
                .Where(p => string.IsNullOrEmpty(query)
                         || p.ShortName.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .ToList();
        }

        private void MoveHighlight(int delta)
        {
            if (mention.FilteredPeople.Count == 0) return;
            var idx = mention.FilteredPeople.FindIndex(p => p.PersonId == mention.HighlightedId);
            if (idx < 0) idx = 0;
            idx = (idx + delta + mention.FilteredPeople.Count) % mention.FilteredPeople.Count;
            mention.HighlightedId = mention.FilteredPeople[idx].PersonId;
        }

        private bool TryGetHighlighted(out Person p)
        {
            p = mention.FilteredPeople.FirstOrDefault(x => x.PersonId == mention.HighlightedId) ?? mention.FilteredPeople.FirstOrDefault();
            return p is not null;
        }

        private async Task SetMentionActiveAsync(bool isActive)
            => await JS.InvokeVoidAsync("mentions.setActive", isActive);


        // ------------------------ End New Stuff ---------------------- //

        /// <summary>
        /// Invoked when the notes filter switch is toggled
        /// </summary>
        private void FilterSwitchToggled()
        {
            LoadNotesFromDB();
            InvokeAsync(async () => await FilterHighlightScrollNotesAsync());
        }

        /// <summary>
        /// Populates the notes to be show in the list
        /// </summary>
        private void LoadNotesFromDB()
        {
            Debug.WriteLine("** [Project Details] Populating notes...");
            allNotes = NoteService.GetAll(Context).Where(x => x.Project.ProjectId == ProjectId).ToList();
            if (showOnlyFinanceNotes) allNotes = allNotes.Where(x => x.IsFinanceInfo).ToList();
            if (showOnlyDueItems) allNotes = allNotes.Where(x => x.IsDue() || x.IsOverDue()).ToList();
            if (sortByDueDate) allNotes = allNotes.Where(x => x.DueDate != null).OrderBy(x => x.DueDate).Concat(allNotes.Where(x => x.DueDate == null)).ToList();
            filteredNotes = allNotes;
        }

        /// <summary>
        /// Clears the search terms and resets the filter
        /// </summary>
        private void ClearSearch()
        {
            noteSearchTerms = string.Empty;
            InvokeAsync(async () => await FilterHighlightScrollNotesAsync());
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
                await JS.InvokeVoidAsync("scrollToElement", "note-editor");

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
            InvokeAsync(async () => await FilterHighlightScrollNotesAsync());
            ShowOrHideEditor(false);
        }

        /// <summary>
        /// Saves a new note to the database and hides the editor
        /// </summary>
        private void SaveNote()
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
            InvokeAsync(async () => await FilterHighlightScrollNotesAsync());
            ShowOrHideEditor(false);
            _ = EmailService.SendMentionAndOwnerEmailNotificationsAsync(noteModel, mentions);
        }

        /// <summary>
        /// Updates an existing note in the DB and hides the editor
        /// </summary>
        private void UpdateNote()
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
            InvokeAsync(async () => await FilterHighlightScrollNotesAsync());
            ShowOrHideEditor(false);
            _ = EmailService.SendMentionAndOwnerEmailNotificationsAsync(noteModel, mentions, listOfNoteChanges);
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
                await FilterHighlightScrollNotesAsync();
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
            else
            {
                // By default sort by start date
                query = query.OrderBy(x => x.StartDate);
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
