using System.Diagnostics;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer,Reader,Finance")]
    public partial class Projects : BaseProjectPage
    {
        [Inject]
        private PaymentService PaymentService { get; set; }

        private IEnumerable<Project> projects;
        private RadzenDataGrid<Project> dataGrid;
        private int count;
        private int pageCount = 15;

        private bool includeFinished;
        public bool IncludeFinished
        {
            get
            {
                return includeFinished;
            }
            set
            {
                if (includeFinished != value)
                {
                    includeFinished = value;
                    SessionStorage.SetItemAsync("project-show-active", includeFinished);
                }

                Debug.WriteLine($"** Include finished set to {value}. Reloading...");
                dataGrid?.Reload();
            }
        }

        private DataGridSettings settings;
        public DataGridSettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                if (settings != value)
                {
                    settings = value;
                    SessionStorage.SetItemAsync("project-settings", settings);
                }
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = true;
            EnqueueLoadData(() => GetLoadTask());
            LogInformation("Viewing project grid");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Load settings the first time
            if (firstRender)
            {
                // Get data grid filters and sort settings
                settings = await SessionStorage.GetItemAsync<DataGridSettings>("project-settings");

                // Get switch setting
                includeFinished = await SessionStorage.GetItemAsync<bool>("project-show-active");
            }
        }

        /// <summary>
        /// Gets the task responsible for loading the data
        /// </summary>
        /// <returns></returns>
        private Task GetLoadTask(LoadDataArgs args = null)
        {
            return Task.Run(() =>
            {
                // Get people from the database
                OnLoadData(args ?? new LoadDataArgs());
            })
                .ContinueWith(t =>
                {
                    InvokeAsync(() =>
                    {
                        Loading = false;
                        StateHasChanged();
                    });
                });
        }

        private void OnLoadData(LoadDataArgs args)
        {
            Debug.WriteLine($"** Loading data...");

            // Initialise the project list -- developers can only see projects to which they are assigned
            IQueryable<Project> query = ProjectService.GetAll(Context).OrderBy(x => x.RTP).AsQueryable();
            if (ActiveUserRoleType == RoleType.Developer)
            {
                query = query.Where(x => x.SubTasks.Any(x => x.AssignedResources.Any(x => x.Person.PersonId == ActiveUser.Person.PersonId)));
            }

            // Remove the ones that are not active if necessary
            if (!includeFinished) query = query.Where(x => !x.ProjectStatus.IsFinishedOrCancelled());

            // Filtering
            if (!string.IsNullOrEmpty(args.Filter))
            {
                // Apply standard filters to the DTOs
                query = query.Where(args.Filter);
            }

            // Filter on Faculty and School
            if (args.Filters is { } filters && filters.Any())
            {
                var filter = filters.FirstOrDefault(f => f.Property == "Faculty");
                var filterValue = (filter?.FilterValue as string)?.Trim();
                if (!string.IsNullOrEmpty(filterValue))
                {
                    var filterValueLower = filterValue.ToLower();
                    query = query.Where(x =>
                        x.School != null &&
                        x.School.Faculty != null &&
                        ((x.School.Faculty.Code ?? "").Trim().ToLower()).Contains(filterValueLower));
                }

                filter = filters.FirstOrDefault(f => f.Property == "School");
                filterValue = (filter?.FilterValue as string)?.Trim();
                if (!string.IsNullOrEmpty(filterValue))
                {
                    var filterValueLower = filterValue.ToLower();
                    query = query.Where(x =>
                        x.School != null &&
                        ((x.School.Code ?? "").Trim().ToLower()).Contains(filterValueLower));
                }
            }

            // Sorting
            if (!string.IsNullOrEmpty(args.OrderBy))
            {
                if (args.OrderBy.StartsWith("Faculty") || args.OrderBy.StartsWith("School"))
                {
                    // Sorting for Faculty and School
                    if (args.Sorts is { } sorts && sorts.Any())
                    {
                        var sort = args.Sorts?.FirstOrDefault();
                        if (sort != null)
                        {

                            if (sort.Property == "Faculty")
                            {
                                query = sort.SortOrder == SortOrder.Ascending
                                    ? query.OrderBy(x =>
                                        x.School != null && x.School.Faculty != null
                                            ? x.School.Faculty.Code
                                            : "")
                                    : query.OrderByDescending(x =>
                                        x.School != null && x.School.Faculty != null
                                            ? x.School.Faculty.Code
                                            : "");
                            }
                            else if (sort.Property == "School")
                            {
                                query = sort.SortOrder == SortOrder.Ascending
                                    ? query.OrderBy(x =>
                                        x.School != null
                                            ? x.School.Code
                                            : "")
                                    : query.OrderByDescending(x =>
                                        x.School != null
                                            ? x.School.Code
                                            : "");
                            }
                        }
                        else
                        {
                            // Should never happen but default to this if it ever did
                            query = query.OrderBy(args.OrderBy);
                        }
                    }
                    else
                    {
                        // Apply standard sorting
                        if (args.OrderBy != null)
                        {
                            query = query.OrderBy(args.OrderBy);
                        }
                    }
                }
            }


            // Assign to grid source
            var data = query.ToList();
            count = data.Count;
            if (args.Skip == null)
            {
                projects = data.Take(pageCount).ToList();
            }
            else
            {
                projects = data.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
            }

            Debug.WriteLine($"** {data.Count()} projects loaded. {projects.Count()} displayed.");
        }

        /// <summary>
        /// Navigate to the add project page
        /// </summary>
        private void AddProject()
        {
            Navigation.NavigateTo($"projects/addproject/-1");
        }
    }
}
