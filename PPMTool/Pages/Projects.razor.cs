using System.Diagnostics;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Enums;
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

            query = ApplySorting(query, args);


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
        /// Applies data grid sorting, including custom sort orders for derived columns.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private IQueryable<Project> ApplySorting(IQueryable<Project> query, LoadDataArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.OrderBy))
            {
                return query;
            }

            var sort = args.Sorts?.FirstOrDefault();
            if (sort == null || string.IsNullOrWhiteSpace(sort.Property))
            {
                return query.OrderBy(args.OrderBy);
            }

            return sort.Property switch
            {
                "Faculty" => ApplyFacultySort(query, sort.SortOrder),
                "School" => ApplySchoolSort(query, sort.SortOrder),
                "CostModel" => ApplyCostModelSort(query, sort.SortOrder),
                "FundsReceived" => ApplyFundsReceivedSort(query, sort.SortOrder),
                _ => query.OrderBy(args.OrderBy)
            };
        }

        /// <summary>
        /// Applies sorting for the derived faculty column.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        private IQueryable<Project> ApplyFacultySort(IQueryable<Project> query, SortOrder? sortOrder)
        {
            return sortOrder == SortOrder.Descending
                ? query.OrderByDescending(x =>
                    x.School != null && x.School.Faculty != null
                        ? x.School.Faculty.Code
                        : "")
                : query.OrderBy(x =>
                    x.School != null && x.School.Faculty != null
                        ? x.School.Faculty.Code
                        : "");
        }

        /// <summary>
        /// Applies sorting for the derived school column.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        private IQueryable<Project> ApplySchoolSort(IQueryable<Project> query, SortOrder? sortOrder)
        {
            return sortOrder == SortOrder.Descending
                ? query.OrderByDescending(x =>
                    x.School != null
                        ? x.School.Code
                        : "")
                : query.OrderBy(x =>
                    x.School != null
                        ? x.School.Code
                        : "");
        }

        /// <summary>
        /// Applies the cost model display order to grid sorting.
        /// The ordering here must match <see cref="CostModelSelectionOptions.DisplayOrder"/>.
        /// A ternary chain is used because EF Core must translate this to SQL CASE WHEN;
        /// arbitrary method calls (e.g. <see cref="CostModelSelectionOptions.GetSortIndex"/>)
        /// cannot be used inside <see cref="IQueryable{T}"/> expressions.
        /// </summary>
        private IQueryable<Project> ApplyCostModelSort(IQueryable<Project> query, SortOrder? sortOrder)
        {
            // Ordering must stay in sync with CostModelSelectionOptions.DisplayOrder.
            return sortOrder == SortOrder.Descending
                ? query.OrderByDescending(x =>
                    x.CostModel == CostModel.TechAndLeadershipWithIndirects ? 0 :
                    x.CostModel == CostModel.TechAndLeadership ? 1 :
                    x.CostModel == CostModel.TechOnlyWithIndirects ? 2 :
                    x.CostModel == CostModel.TechOnly ? 3 : 4)
                : query.OrderBy(x =>
                    x.CostModel == CostModel.TechAndLeadershipWithIndirects ? 0 :
                    x.CostModel == CostModel.TechAndLeadership ? 1 :
                    x.CostModel == CostModel.TechOnlyWithIndirects ? 2 :
                    x.CostModel == CostModel.TechOnly ? 3 : 4);
        }

        /// <summary>
        /// Applies sorting by the sum of payment values (funds received) for each project.
        /// </summary>
        private IQueryable<Project> ApplyFundsReceivedSort(IQueryable<Project> query, SortOrder? sortOrder)
        {
            return sortOrder == SortOrder.Descending
                ? query.OrderByDescending(x => x.Payments != null ? x.Payments.Sum(p => p.Value) : 0)
                : query.OrderBy(x => x.Payments != null ? x.Payments.Sum(p => p.Value) : 0);
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
