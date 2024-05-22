using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
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

        [Parameter]
        public int? ProjectId { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "rtp")]
        public int? RTP { get; set; }

        private List<SubTask> confirmedTasks;
        private List<SubTask> provisionalTasks;
        private List<SubTask> allTasks;
        private List<Note> allNotes;
        private Project project;
        private List<ChartItem> burnUpChartSource = new List<ChartItem>();
        private ApexChartOptions<SubTask> ganttChartOptions;
        private ApexChartOptions<ChartItem> burnUpChartOptions;
        private int count;
        private string plannedCostColour;
        private string actualCostColour;
        private string fundsReceivedColour;
        private bool addNote;
        private bool editNote;
        private Note noteModel = new Note();
        private string noteSearchTerms;
        private List<Note> filteredNotes;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Query string only consulted when Project ID is not specified in URL
            if (ProjectId == null && RTP != null)
            {
                // Try get the project
                ProjectId = ProjectService.GetByRTP(context, RTP)?.ProjectId;
            }

            // Carry on and load the project details
            if (ProjectId != null)
            {
                project = ProjectService.GetById(context, ProjectId);
                confirmedTasks = project.SubTasks.Where(x => !x.AssignedResources.Any(x => x.IsProvisional)).OrderBy(x => x.StartDate).ToList();
                provisionalTasks = project.SubTasks.Where(x => x.AssignedResources.Any(x => x.IsProvisional)).OrderBy(x => x.StartDate).ToList();
                allTasks = confirmedTasks.Concat(provisionalTasks).ToList();
                plannedCostColour = project.PlannedCost > project.Budget ? "red" : "green";
                actualCostColour = project.ActualCost > project.PlannedCost ? "red" : "green";
                fundsReceivedColour = project.FundsReceived < project.Budget ? "red" : "green";
                PopulateNotes();
                count = allTasks.Count;

                ganttChartOptions = new ApexChartOptions<SubTask>
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
                    Legend = new Legend
                    {
                        Show = false
                    },
                };

                // Create the chart items
                var temp = ChartHelper.AggregateSubTasksByWeek(
                    project.GetFullName(),
                    project.SubTasks,
                    (task, currentWeek) =>
                    {
                        // Value 1 requires the number of days is simply the planned work hours up to the end of that week
                        return task.GetPlannedWorkUpToEndOfWeek(currentWeek);
                    },
                    (task, currentWeek) =>
                    {
                        // Value 2 is corrected for the unmet demand on the task
                        return task.GetPlannedWorkUpToEndOfWeek(currentWeek) * (1 - (task.UnmetDemand / task.Demand));
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
        }

        private void PopulateNotes()
        {
            allNotes = NoteService.GetAll(context).Where(x => x.Project.ProjectId == ProjectId).ToList();
            FilterNotes();
        }

        private void FilterNotes()
        {
            if (!string.IsNullOrWhiteSpace(noteSearchTerms))
            {
                filteredNotes = allNotes.Where(x =>
                {
                    var plainText = HtmlHelper.ConvertToPlainText(x.HtmlContent);
                    return plainText.ToLower().Contains(noteSearchTerms.ToLower());
                }).ToList();
            }
            else
            {
                filteredNotes = allNotes;
            }
            StateHasChanged();
        }

        private void ClearSearch()
        {
            noteSearchTerms = string.Empty;
            FilterNotes();
        }

        private async void SetShowNoteEditor(bool show)
        {
            // Toggle state
            addNote = show;
            if (addNote)
            {
                noteModel = new Note();

                // Scroll to the new editor window after a delay to allow the page to render
                await Task.Delay(500);
                await JSRuntime.InvokeVoidAsync("scrollToElement", "note-editor");
                StateHasChanged();
            }
            else
            {
                editNote = false;
            }
        }

        private void SaveNote()
        {
            if (project == null || project.ProjectId < 0)
            {
                SetShowNoteEditor(false);
                LogError("Attempt to add a note when no project model present!");
                return;
            }

            // Populate model and add to DB
            noteModel.Project = project;
            var role = RolesService.GetByUsername(context, ActiveUserName);
            noteModel.Author = role.Person;
            noteModel.CreatedDate = DateTime.Now;
            LogInformation($"Added note for {project.GetFullName()}");
            NoteService.Add(context, noteModel);
            PopulateNotes();
            SetShowNoteEditor(false);
        }

        private void UpdateNote()
        {
            // Update model in DB
            noteModel.EditedDate = DateTime.Now;
            var role = RolesService.GetByUsername(context, ActiveUserName);
            noteModel.Editor = role.Person;
            LogInformation($"Updated note for {project.GetFullName()}");
            NoteService.Update(context, noteModel);
            PopulateNotes();
            SetShowNoteEditor(false);
        }

        private void EditNote(Note noteToEdit)
        {
            SetShowNoteEditor(true);
            LogInformation($"Editing note {noteModel.NoteId} for {project.GetFullName()}");
            noteModel = noteToEdit;
            editNote = true;
        }

        private async void DeleteNote(Note noteToDelete)
        {
            bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", $"You are about to delete a note from {project.GetFullName()}!");
            if (confirmed)
            {
                LogInformation($"Deleting note {noteToDelete.NoteId} | {noteToDelete.HtmlContent} | {noteToDelete.GetNoteAuthorText()}");
                NoteService.Delete(context, noteToDelete);
                PopulateNotes();
                StateHasChanged();
            }
        }

        private void TaskSelected(SelectedData<SubTask> dataPoint)
        {
            if (!EditAuthorised) return;

            // Only so the navigation when in project view mode
            if (dataPoint.IsSelected)
            {
                var task = dataPoint.DataPoint.Items.FirstOrDefault();
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

        void EditProject()
        {
            Navigation.NavigateTo($"addproject/{project.ProjectId}");
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
    }
}
