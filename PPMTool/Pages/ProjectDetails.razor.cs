using System;
using System.Collections.Generic;
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
    [Authorize(Roles = "Manager,Superuser,Developer,Reader")]
    public partial class ProjectDetails : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private NoteService NoteService { get; set; }

        [Inject]
        private RolesService RolesService { get; set; }

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
        private List<ChartItem> burnUpChartSource = new List<ChartItem>();
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
        private Person activeUser;
        private bool isProjectManager;
        private bool groupLinkedTasks = false;
        private ApexChart<GanttBlock> gantt;

        internal class GanttBlock : IChartItem
        {
            public GanttBlock(SubTask t, string groupName, bool isFake = false)
            {
                Task = t;
                PredecessorGroupName = groupName;
                this.isFake = isFake;
            }

            public SubTask Task { get; private set; }

            public string PredecessorGroupName { get; private set; }

            private bool isFake;

            public bool IsFake()
            {
                return isFake;
            }

            public bool IsHatched()
            {
                return Task.AssignedResources.Any(x => x.IsProvisional);
            }
        }


        protected override void OnInitialized()
        {
            base.OnInitialized();
            var role = RolesService.GetByUsername(context, ActiveUserName);
            activeUser = role?.Person;
            allProjects = ProjectService.GetAll(context).ToList();

            cachedMentionables = RolesService.GetAll(context).Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser).DistinctBy(x => x.Person).Select(x => x.Person).ToList();
            FilterMentionables();

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
                isCurrentUserFollowing = project.Followers.Any(x => x.Name == activeUser.Name) ||
                    project.ProjectManager?.Name == activeUser.Name;
                isProjectManager = role.RoleType == RoleType.Superuser || (role.RoleType == RoleType.Manager && activeUser == project?.ProjectManager);

                ganttChartOptions = new ApexChartOptions<GanttBlock>
                {
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

                // Create the burn-up chart items
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
                        return assignments.RoundedSum(task => task.GetPlannedWorkWithinCurrentWeek(currentWeek) * (1 - (task.UnmetDemand / task.Demand)));
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
                InvokeAsync(StateHasChanged);
            }
            LogInformation($"Viewing project details for RTP-{project?.RTP}");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            // If no project ID set by the time the page is renderered then navigate away
            if (ProjectId == null) Navigation.NavigateTo("/nothinghere");

            if (firstRender)
            {
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
                        InvokeAsync(async () =>
                        {
                            // Refresh then scroll last due note into view
                            StateHasChanged();
                            await Task.Delay(300);
                            await JSRuntime.InvokeVoidAsync("scrollToElement", $"note_{filteredNotes.LastOrDefault()?.NoteId}");
                        });
                    }
                }
                else
                {
                    PopulateNotes();
                }
            }
        }

        private void GroupTasksChanged(bool value)
        {
            // Set the axis limits?
            ganttChartOptions.Yaxis = new List<YAxis>
            {
                new YAxis
                {
                    Min = confirmedBlocks.Concat(provisionalBlocks).Min(x => x.Task.StartDate).ToUnixTimeMilliseconds(),
                    Max = confirmedBlocks.Concat(provisionalBlocks).Max(x => x.Task.EndDate).ToUnixTimeMilliseconds()
                }
            };
            gantt?.UpdateOptionsAsync(false, false, false);

            // Redraw the chart
            gantt?.RenderAsync();
        }

        /// <summary>
        /// Toggle the following status by adding or removing the current acitve user to the project's follower list
        /// </summary>
        private void ToggleFollowing()
        {
            if (project.Followers.Contains(activeUser))
            {
                project.Followers.Remove(activeUser);
                ProjectService.Update(context, project);
                isCurrentUserFollowing = false;
                LogInformation($"Stopped following project {project.GetFullName()}");
            }
            else
            {
                project.Followers.Add(activeUser);
                ProjectService.Update(context, project);
                isCurrentUserFollowing = true;
                LogInformation($"Now following project {project.GetFullName()}");
            }
            StateHasChanged();
        }

        private void HighlightMention(Person person)
        {
            highlightedPerson = person;
        }

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

        private void ProcessEditorInput(KeyboardEventArgs args)
        {
            Debug.WriteLine($"** Key pressed in the editor {args.Key}");

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

        private async Task OnMentionPopupOpenAsync()
        {
            // Focus on the search box
            await JSRuntime.InvokeVoidAsync("eval", "setTimeout(function(){ document.getElementById('search').focus(); }, 200)");
        }

        private void MentionPerson(Person person)
        {
            htmlEditor.RestoreSelectionAsync().ContinueWith(async t =>
            {
                await JSRuntime.InvokeVoidAsync("insertTextAtCaret", $"{person?.ShortName ?? ""}");

                // Close the popup
                MentionSearchString = string.Empty;
                await popup.CloseAsync();
            });
        }

        private void FilterSwitchToggled()
        {
            PopulateNotes();
        }

        private void PopulateNotes()
        {
            allNotes = NoteService.GetAll(context).Where(x => x.Project.ProjectId == ProjectId).ToList();
            if (showOnlyFinanceNotes) allNotes = allNotes.Where(x => x.IsFinanceInfo).ToList();
            if (showOnlyDueItems) allNotes = allNotes.Where(x => x.IsDue() || x.IsOverDue()).ToList();
            if (sortByDueDate) allNotes = allNotes.Where(x => x.DueDate != null).OrderBy(x => x.DueDate).Concat(allNotes.Where(x => x.DueDate == null)).ToList();
            filteredNotes = allNotes;
            FilterNotes();
        }

        private void FilterNotes()
        {
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

        private void ClearSearch()
        {
            noteSearchTerms = string.Empty;
            FilterNotes();
        }

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

        private void EditProject(Project project)
        {
            Navigation.NavigateTo($"/addproject/{project.ProjectId}");
        }

        private void AddClicked()
        {
            noteModel = new Note();
            isEditExistingNote = false;
            ShowOrHideEditor(true);
        }

        private void DiscardClicked()
        {
            LogInformation($"Discarding changes to note {noteModel?.NoteId} on {project.GetFullName()}");
            if (isEditExistingNote)
            {
                NoteService.RestoreModel(context, ref noteModel);
            }
            isEditExistingNote = false;
            PopulateNotes();
            ShowOrHideEditor(false);
        }

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
            var role = RolesService.GetByUsername(context, ActiveUserName);
            noteModel.Author = role.Person;
            noteModel.CreatedDate = DateTime.Now;
            ResolveMentionsInCurrentNoteModel();
            NoteService.Add(context, noteModel);
            LogInformation($"Added note for {project.GetFullName()}");
            PopulateNotes();
            ShowOrHideEditor(false);
            EmailService.SendMentionAndOwnerEmailNotifications(noteModel, mentions);
        }

        private void UpdateNote()
        {
            // Update model in DB
            noteModel.EditedDate = DateTime.Now;
            var role = RolesService.GetByUsername(context, ActiveUserName);
            noteModel.Editor = role.Person;
            ResolveMentionsInCurrentNoteModel();
            NoteService.Update(context, noteModel, false);
            var listOfNoteChanges = NoteService.GetDiffList<Note>(context);
            NoteService.Update(context, noteModel, true);
            LogInformation($"Updated note {noteModel.NoteId} for {project.GetFullName()}");
            PopulateNotes();
            ShowOrHideEditor(false);
            EmailService.SendMentionAndOwnerEmailNotifications(noteModel, mentions, listOfNoteChanges);
        }

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

        private async void DeleteNote(Note noteToDelete)
        {
            bool confirmed = await DialogService.Confirm($"You are about to delete a note from {project.GetFullName()}!", "Delete Note") ?? false;
            if (confirmed)
            {
                LogInformation($"Deleting note {noteToDelete.NoteId} | {noteToDelete.HtmlContent} | {noteToDelete.GetNoteAuthorText()}");
                NoteService.Delete(context, noteToDelete);
                PopulateNotes();
                StateHasChanged();
            }
        }

        private async void CopyLinkToNoteToClipboard(Note noteTolink)
        {
            var link = $"{Configuration["Authentication:HostUrl"]}/projectdetails/{project.ProjectId}?filteredNote={noteTolink.NoteId}";
            await JSRuntime.InvokeVoidAsync("copyText", link);
        }

        private void MarkComplete(Note note)
        {
            LogInformation($"Completing note {note.NoteId} for {project.GetFullName()}");
            note.CompletedDate = DateTime.Now;
            NoteService.Update(context, note);
            StateHasChanged();
        }

        /// <summary>
        /// Attempts to resolve the mentions and links in the note content for the current note model.
        /// </summary>
        private void ResolveMentionsInCurrentNoteModel()
        {
            // Get list of all new mentions in the note content
            var newMentions = new List<string>();
            var matches = Regex.Matches(noteModel.HtmlContent, @"@\w+");
            newMentions.AddRange(matches.Select(x => x.Value).Distinct());

            // Load in the list of managers
            var managers = RolesService.GetAll(context).Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser).Select(x => x.Person).ToList();

            // For each mention, attempt to resolve it and replace in the HTMl content
            foreach (var m in newMentions)
            {
                var match = managers.FirstOrDefault(x => x.ShortName.Equals(m.Substring(1), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    noteModel.HtmlContent = noteModel.HtmlContent.Replace(m, $"<span class=\"badge badge-primary\">{match.Name}</span>");
                }
                else
                {
                    // Warning if the mention cannot be resolved
                    ShowNotification(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Mention Failure",
                        Detail = $"The mention {m} could not be resolved! Please edit your note to correct.",
                        Duration = 4000
                    });
                }
            }

            // Update the mentions list (for notifications) by extracting the formatted tags
            var resolvedMentions = new List<string>();
            matches = Regex.Matches(noteModel.HtmlContent, @"<span class=""badge badge-primary"">(.*?)<\/span>");
            resolvedMentions.AddRange(matches.Select(x => x.Groups[1].Value).Distinct());
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
            matches = Regex.Matches(noteModel.HtmlContent, @"#RTP-\w+", RegexOptions.IgnoreCase);
            newRtpRefs.AddRange(matches.Select(x => x.Value).Distinct());

            // For each reference, attempt to resolve it and replace in the HTMl content
            foreach (var r in newRtpRefs)
            {
                var match = allProjects.FirstOrDefault(x => x.RTP.ToString().Equals(r.Substring(5), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    noteModel.HtmlContent = noteModel.HtmlContent.Replace(r, $"<a href=\"{Configuration["Authentication:HostUrl"]}/projectdetails/{match.ProjectId}\" class=\"badge badge-success\">{match.GetFullName()}</a>");
                }
                else
                {
                    // Warning if the reference cannot be resolved
                    ShowNotification(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "RTP Reference Failure",
                        Detail = $"The reference {r} could not be resolved! Please edit your note to correct.",
                        Duration = 4000
                    });
                }
            }
        }

        private void TaskSelected(SelectedData<GanttBlock> dataPoint)
        {
            if (!EditAuthorised) return;

            // Only so the navigation when in project view mode
            if (dataPoint.IsSelected)
            {
                var task = dataPoint.DataPoint.Items.FirstOrDefault()?.Task;
                if (task == null) return;
                Debug.WriteLine($"** Selected {task.Name}. Navigating to task edit page...");
                Navigation.NavigateTo($"/addtask/{ProjectId}/{task.SubTaskId}");
            }
        }

        void EditTask(SubTask task)
        {
            Navigation.NavigateTo($"/addtask/{project.ProjectId}/{task.SubTaskId}");
        }

        void AddTask()
        {
            Navigation.NavigateTo($"/addtask/{project.ProjectId}/-1");
        }

        void CopyTask(SubTask task)
        {
            // Navigate to the add task page passing the task ID to be copied and the query string parameter to indicate it is a copy
            Navigation.NavigateTo($"/addtask/{project.ProjectId}/{task.SubTaskId}?copy=true");
        }

        void SplitTask(SubTask task)
        {
            // Navigate to the split task page passing the task ID to be split
            Navigation.NavigateTo($"splittask/{project.ProjectId}/{task.SubTaskId}");
        }

        // Necessary to ensure that we can filter the resources on the fly
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
                ProjectService.Update(context, project);
                StateHasChanged();
            }
        }
    }
}
