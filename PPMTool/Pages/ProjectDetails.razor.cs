// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Pages.Components;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Contractor,Reader")]
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
        private List<Project> allProjects;
        private List<Note> allNotes;
        private Project project;
        private List<ChartItem> burnUpChartSource;
        private ApexChartOptions<GanttBlock> ganttChartOptions;
        private ApexChartOptions<ChartItem> burnUpChartOptions;
        private int count;
        private string plannedCostColour;
        private string actualCostColour;
        private string fundsReceivedColour;
        private bool isEditExistingNote;
        private bool editorVisible;
        private Note noteModel = new Note();
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
        private ApexChart<ChartItem> burnUpChart;


        /// <summary>
        /// Represents a block on the schedule chart
        /// </summary>
        internal class GanttBlock : IChartItem
        {
            public GanttBlock(SubTask t, string groupName, bool isFake = false, bool isLeadershipTask = false)
            {
                Task = t;
                PredecessorGroupName = groupName;
                this.isFake = isFake;
                this.IsLeadershipTask = isLeadershipTask;
            }

            /// <summary>
            /// The subtask which is associated with the Gantt Block
            /// </summary>
            public SubTask Task { get; private set; }

            /// <summary>
            /// When grouping tasks that are linked, this is the name of the group
            /// </summary>
            public string PredecessorGroupName { get; private set; }

            /// <summary>
            /// Whether this task is a fake task which exists in either the provisional or confirmed series so they both match in length.
            /// This is to workaround a bug in Apex Charts where the sorting doesn't work if the series aren't all the same length
            /// </summary>
            private bool isFake;

            /// <summary>
            /// Whether this task is a leaderhsip task and hence doesn't have a proper subtask object associated with it in the DB.
            /// </summary>
            public bool IsLeadershipTask { get; private set; }

            public bool IsFake()
            {
                return isFake;
            }

            public bool IsHatched()
            {
                return Task.AssignedResources.Any(x => x.IsProvisional);
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            Loading = true;

            var role = RolesService.GetByUsername(Context, ActiveUserName);

            // Reset the search box
            noteSearchTerms = string.Empty;

            // Filter the mentions reset
            cachedMentionables = RolesService.GetAll(Context).Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser).DistinctBy(x => x.Person).Select(x => x.Person).ToList();
            FilterMentionables();

            Task.Run(() =>
            {
                // Query string only consulted when Project ID is not specified in URL
                if (ProjectId == null && RTP != null)
                {
                    // Try get the project
                    ProjectId = allProjects.FirstOrDefault(x => x.RTP == RTP)?.ProjectId;
                }

                // Carry on and load the project details
                if (ProjectId != null)
                {
                    project = allProjects.FirstOrDefault(x => x.ProjectId == ProjectId);

                    // Generate the blocks for the schedule chart
                    allTasks = project.SubTasks.OrderBy(x => x.StartDate).ToList();
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
                            EndDate = dateRange.EndDate.AddDays(-1),
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
                    plannedCostColour = project.PlannedCost > project.Budget ? "red" : "green";
                    actualCostColour = project.ActualCost > project.PlannedCost ? "red" : "green";
                    fundsReceivedColour = project.FundsReceived < project.Budget ? "red" : "green";
                    count = allTasks.Count;
                    isCurrentUserFollowing = project.Followers.Any(x => x.Name == ActiveUser?.Name) ||
                        project.ProjectManager?.Name == ActiveUser?.Name;
                    isProjectManager = role.RoleType == RoleType.Superuser || (role.RoleType == RoleType.Manager && ActiveUser == project?.ProjectManager);

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

                    // Create the burn-up chart items
                    burnUpChartSource = new List<ChartItem>();
                    var temp = ChartHelper.AggregateSubTasksByWeek(
                        project.GetFullName(),
                        project.SubTasks,
                        (assignments, currentWeek) =>
                        {
                            // Value 1 requires the number of days is simply the planned work hours up to the end of that week
                            return assignments.RoundedSum(task => task.GetPlannedWorkWithinCurrentWeek(currentWeek));
                        },
                        (assignments, currentWeek) =>
                        {
                            // Value 2 is corrected for the unmet demand on the task
                            return assignments.RoundedSum(task => task.Demand == 0 ? 0 : (task.GetPlannedWorkWithinCurrentWeek(currentWeek) * (1 - (task.UnmetDemand / task.Demand))));
                        }
                    ).ToList();

                    // Generate series by aggregating the values
                    double cumulativeValue1 = 0;
                    double cumulativeValue2 = 0;
                    foreach (var week in temp)
                    {
                        cumulativeValue1 += week.Value1;
                        cumulativeValue2 += week.Value2;
                        burnUpChartSource.Add(new ChartItem(null, week.Label, week.StartDate, week.EndDate, Math.Round(cumulativeValue1), Math.Round(cumulativeValue2), false));
                    }

                    // Early exit if chartSource has no data
                    if (burnUpChartSource.Count < 1) return;

                    // Create a new data point to indicate progress
                    var seriesStart = burnUpChartSource.Min(x => x.StartDate);
                    var seriesEnd = burnUpChartSource.Max(x => x.EndDate);
                    var actualsX = DateTime.Today;
                    var actualsY = project.SubTasks.RoundedSum(x => x.ActualWorkHours);

                    // If the task has started yet or has already finished then x coordinate is the limits of the series
                    if (DateTime.Today < seriesStart) actualsX = seriesStart;
                    else if (DateTime.Today > seriesEnd) actualsX = seriesEnd;

                    // Set options
                    burnUpChartOptions = new ApexChartOptions<ChartItem>
                    {
                        Chart = new Chart
                        {
                            Zoom = new Zoom
                            {
                                AllowMouseWheelZoom = false
                            }
                        },
                        Stroke = new Stroke
                        {
                            Curve = new CurveSelections(new Curve[] { Curve.Straight })
                        },
                        Colors = new List<string> { "#1151F3", "#FFC107" },
                        Annotations = new Annotations
                        {
                            Yaxis = new List<AnnotationsYAxis>
                            {
                                new AnnotationsYAxis()
                                {
                                    Y = actualsY,
                                    BorderWidth = 2,
                                    StrokeDashArray = 5,
                                    BorderColor = "red",
                                    Label = new Label
                                    {
                                        Text = "Actual (Hours)",
                                        Position = LabelPosition.Right
                                    }
                                }
                            },
                            Xaxis = new List<AnnotationsXAxis>
                            {
                                new AnnotationsXAxis()
                                {
                                    X = actualsX.ToUnixTimeMilliseconds(),
                                    BorderWidth = 2,
                                    StrokeDashArray = 5,
                                    BorderColor = "red",
                                    Label = new Label
                                    {
                                        Text = "Current Week",
                                        Position = LabelPosition.Left
                                    }
                                }
                            }
                        },
                        Xaxis = new XAxis { Title = new AxisTitle { Text = "Week Beginning" } },
                        Yaxis = new List<YAxis>
                        {
                            new YAxis { Title = new AxisTitle { Text = "Work (Hours)" } }
                        }
                    };
                }
                LogInformation($"Viewing project details for RTP-{project?.RTP}");
            }).ContinueWith(t =>
            {
                Loading = false;
                InvokeAsync(async () =>
                {
                    StateHasChanged();
                    await OnAfterRenderAsync(true);
                });
            });
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            allProjects = ProjectService.GetAll(Context).ToList();

            Debug.WriteLine($"** Initialised project details");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            // If no project ID set by the time the page is renderered then navigate away
            if (ProjectId == null) Navigation.NavigateTo("nothinghere");

            // If the path is the legacy path then redirect
            if (!Navigation.Uri.Contains("projects/projectdetails"))
            {
                Navigation.NavigateTo(Navigation.Uri.Replace("/projectdetails", "/projects/projectdetails"));
                return;
            }

            if (firstRender)
            {
                // Create a reference to self in JS
                await JSRuntime.InvokeVoidAsync("setDotNetReference", DotNetObjectReference.Create(this));

                // After the page has finished rendering then apply the search string from the parameter
                if (FilteredNote != null)
                {
                    // Set the search term
                    noteSearchTerms = $"#id={FilteredNote}";
                    PopulateNotes();
                }
                else if (FilterDueNotes)
                {
                    showOnlyDueItems = true;
                    sortByDueDate = true;
                    PopulateNotes();

                    // Check whether the parameter is present to scroll to the due notes
                    if (FilterDueNotes)
                    {
                        // Refresh then scroll last due note into view
                        StateHasChanged();
                        await Task.Delay(300);
                        await JSRuntime.InvokeVoidAsync("scrollToElement", $"note_{filteredNotes.LastOrDefault()?.NoteId}");
                    }
                }
                else
                {
                    PopulateNotes();
                }
            }
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
            if (project.Followers.Contains(ActiveUser))
            {
                project.Followers.Remove(ActiveUser);
                ProjectService.Update(Context, project);
                isCurrentUserFollowing = false;
                LogInformation($"Stopped following project {project.GetFullName()}");
            }
            else
            {
                project.Followers.Add(ActiveUser);
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
                mentionables = cachedMentionables.Where(x => x.Name.ToLower().Contains(mentionSearchString.ToLower()) || x.ShortName.ToLower().StartsWith(mentionSearchString.ToLower())).ToList();
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
        /// This method is fired from JS when the content of the HTML editor is changed from JS rather than a key press
        /// </summary>
        /// <param name="content"></param>
        [JSInvokable]
        public void OnEditorChangeFromJS(string content)
        {
            noteModel.HtmlContent = content;
            Debug.WriteLine($"** After Mention Insert: {noteModel.HtmlContent}");
        }

        /// <summary>
        /// Invoked when the notes filter switch is toggled
        /// </summary>
        private void FilterSwitchToggled()
        {
            PopulateNotes();
        }

        /// <summary>
        /// Populates the notes to be show in the list
        /// </summary>
        private void PopulateNotes()
        {
            Debug.WriteLine("** Populating notes...");
            allNotes = NoteService.GetAll(Context).Where(x => x.Project.ProjectId == ProjectId).ToList();
            if (showOnlyFinanceNotes) allNotes = allNotes.Where(x => x.IsFinanceInfo).ToList();
            if (showOnlyDueItems) allNotes = allNotes.Where(x => x.IsDue() || x.IsOverDue()).ToList();
            if (sortByDueDate) allNotes = allNotes.Where(x => x.DueDate != null).OrderBy(x => x.DueDate).Concat(allNotes.Where(x => x.DueDate == null)).ToList();
            filteredNotes = allNotes;
            FilterNotes();
        }

        /// <summary>
        /// Filters the notes in the list via JS and also applies text highlighting if searching
        /// </summary>
        private void FilterNotes()
        {
            Debug.WriteLine("** Filtering / Highlighting notes...");

            // Clear existing highlighting
            InvokeAsync(async () =>
            {
                await JSRuntime.InvokeVoidAsync("clearHighlightInNotes");
            }).ContinueWith(async t =>
            {
                // Wait for JS to finish
                await Task.Delay(500);

                // No search terms so show all
                if (string.IsNullOrWhiteSpace(noteSearchTerms))
                {
                    filteredNotes = allNotes;
                    Debug.WriteLine($"** Notes reset");
                    await InvokeAsync(StateHasChanged);
                }

                // Search terms are present
                else
                {
                    // Search by DB ID (useful for resolving links)
                    if (noteSearchTerms.StartsWith("#id=") && noteSearchTerms.Length > 4 && int.TryParse(noteSearchTerms.Substring(4), out int noteId))
                    {
                        filteredNotes = allNotes.Where(x => x.NoteId == noteId).ToList();
                        Debug.WriteLine($"** Filtered based on ID {noteId} giving {filteredNotes.Count} notes.");
                        await InvokeAsync(async () =>
                        {
                            // Refresh then scroll to note
                            StateHasChanged();
                            await Task.Delay(300);
                            await JSRuntime.InvokeVoidAsync("scrollToElement", $"note_{noteId}");
                        });
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
                        await InvokeAsync(async () =>
                        {
                            // Refresh
                            StateHasChanged();

                            // Wait for the page to render
                            await Task.Delay(500);

                            // Call highlighter JS function
                            await JSRuntime.InvokeVoidAsync("highlightInNotes", noteSearchTerms.Trim());
                        });
                    }
                }
            });
        }

        /// <summary>
        /// Clears the search terms and resets the filter
        /// </summary>
        private void ClearSearch()
        {
            noteSearchTerms = string.Empty;
            FilterNotes();
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
        /// Handles the add note button click
        /// </summary>
        private void AddClicked()
        {
            noteModel = new Note();
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
            PopulateNotes();
            ShowOrHideEditor(false);
        }

        /// <summary>
        /// Saves a new note to the database and hides the editor
        /// </summary>
        private void SaveNote()
        {
            if (project == null || project.ProjectId < 0)
            {
                ShowOrHideEditor(false);
                LogError("Attempt to add a note when no project model present!");
                return;
            }

            // Populate model and add to DB
            noteModel.Project = project;
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            noteModel.Author = role.Person;
            noteModel.CreatedDate = DateTime.Now;
            ResolveMentionsInCurrentNoteModel();
            NoteService.Add(Context, noteModel);
            LogInformation($"Added note for {project.GetFullName()}");
            PopulateNotes();
            ShowOrHideEditor(false);
            EmailService.SendMentionAndOwnerEmailNotifications(noteModel, mentions);
        }

        /// <summary>
        /// Updates an existing note in the DB and hides the editor
        /// </summary>
        private void UpdateNote()
        {
            // Update model in DB
            noteModel.EditedDate = DateTime.Now;
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            noteModel.Editor = role.Person;
            ResolveMentionsInCurrentNoteModel();
            NoteService.Update(Context, noteModel, false);
            var listOfNoteChanges = NoteService.GetDiffList<Note>(Context);
            NoteService.Update(Context, noteModel, true);
            LogInformation($"Updated note {noteModel.NoteId} for {project.GetFullName()}");
            PopulateNotes();
            ShowOrHideEditor(false);
            EmailService.SendMentionAndOwnerEmailNotifications(noteModel, mentions, listOfNoteChanges);
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
                PopulateNotes();
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
            var managers = RolesService.GetAll(Context).Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser).Select(x => x.Person).ToList();

            // For each mention, attempt to resolve it and replace in the HTMl content
            foreach (Match m in matches)
            {
                var person = managers.FirstOrDefault(x => x.ShortName.Equals(m.Value.Trim().Substring(1), StringComparison.OrdinalIgnoreCase));
                if (person != null)
                {
                    Debug.WriteLine($"** Replacing {m} with {person.Name}");
                    noteModel.HtmlContent = noteModel.HtmlContent.Replace(m.Value, $"&nbsp;<span class=\"badge badge-primary\">{person.Name}</span>&nbsp;");
                }
                else
                {
                    // Warning if the mention cannot be resolved
                    ShowNotification(new CapXNotificationMessage
                    {
                        Summary = "Mention Failure",
                        Detail = $"The mention {m} could not be resolved! Please edit your note to correct."
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
            newRtpRefs.AddRange(matches.Select(x => x.Value).Distinct());

            // For each reference, attempt to resolve it and replace in the HTMl content
            foreach (var r in newRtpRefs)
            {
                var match = allProjects.FirstOrDefault(x => x.RTP.ToString().Equals(r.Substring(5), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    noteModel.HtmlContent = noteModel.HtmlContent.Replace(r, $"&nbsp;<a href=\"{Configuration["Authentication:HostUrl"]}/projects/projectdetails/{match.ProjectId}\" class=\"badge badge-success\">{match.GetFullName()}</a>&nbsp;");
                }
                else
                {
                    // Warning if the reference cannot be resolved
                    ShowNotification(new CapXNotificationMessage
                    {
                        Summary = "RTP Reference Failure",
                        Detail = $"The reference {r} could not be resolved! Please edit your note to correct."
                    });
                }
            }
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
        private void LoadData(LoadDataArgs args)
        {
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

            // Perform paging via Skip and Take.
            allTasks = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
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
