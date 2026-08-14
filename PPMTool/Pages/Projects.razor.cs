// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Data.Helpers;
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

        // Expression for sorting by cost model display order
        private Expression<Func<Project, int>> costModelSortKey;

        protected override string GetStorageTag() => "projects";

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Initialise the sort key expression for the data grid sort
            costModelSortKey = DisplayOrderHelper.CreateOrderAttributeSortingExpression<Project, CostModel>(p => p.CostModel);

            LogInformation("Viewing project grid");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Ensure base class OnAfterRenderAsync runs so BasePage can perform its first-render work
            await base.OnAfterRenderAsync(firstRender);

            // Load settings the first time
            if (firstRender)
            {
                // Navigate away if feature not enabled
                if (!FeatureService.IsFeatureEnabled(FeatureType.ProjectsAndCapacity))
                {
                    Navigation.NavigateTo("people");
                    return;
                }

                // Get data grid filters and sort settings
                settings = await SessionStorage.GetItemAsync<DataGridSettings>("project-settings");

                // Get switch setting
                includeFinished = await SessionStorage.GetItemAsync<bool>("project-show-active");

                // Load the project data
                await LoadProjectsAsync();
            }
        }

        /// <summary>
        /// Wrapper for loading the data
        /// </summary>
        /// <returns></returns>
        private async Task LoadProjectsAsync()
        {
            Loading = true;
            StateHasChanged();
            await Task.Yield();

            Debug.WriteLine("** [Projects] Loading Data...");
            LoadData(new LoadDataArgs());

            Loading = false;
            StateHasChanged();
        }

        /// <summary>
        /// Loads the project data for the grid, applying filtering and sorting as specified in the LoadDataArgs.
        /// </summary>
        /// <param name="args"></param>
        private void LoadData(LoadDataArgs args)
        {
            Debug.WriteLine($"** Loading data...");

            // Initialise the project list -- developers can only see projects to which they are assigned
            IQueryable<Project> query = ProjectService.GetAll(Context).OrderBy(x => x.RTP).AsQueryable();

            if (ActiveUserRoleType == RoleType.Developer)
            {
                query = query.Where(x => ActiveUser.Person != null && x.SubTasks.Any(x => x.AssignedResources.Any(x => x.Person.PersonId == ActiveUser.Person!.PersonId)));
            }

            // Remove the ones that are not active if necessary
            if (!includeFinished) query = query.Where(x => !x.ProjectStatus.IsFinishedOrCancelled());

            // Filtering
            if (!string.IsNullOrEmpty(args.Filter))
            {
                // Apply standard filters to the DTOs
                query = query.Where(args.Filter);
            }

            query = ApplySorting(query, args);

            // Assign to grid source
            var data = query.ToList();
            count = data.Count;

            List<Project> projectsToDisplay;
            if (args.Skip == null)
            {
                projectsToDisplay = data.Take(PageCount).ToList();
            }
            else
            {
                projectsToDisplay = data.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
            }

            // Load FundsReceived for displayed projects only (after filtering and paging)
            var projectIds = projectsToDisplay.Select(p => p.ProjectId).ToList();
            var fundsReceivedLookup = PaymentService.GetFundsReceivedForProjects(Context, projectIds);

            // Populate FundsReceived from the lookup
            foreach (var project in projectsToDisplay)
            {
                project.FundsReceived = fundsReceivedLookup.TryGetValue(project.ProjectId, out var funds) ? funds : 0;
            }

            // Now assign to bound variable for display
            projects = projectsToDisplay;

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
        /// Ordering is derived directly from [DisplayOrder] on <see cref="CostModel"/>.
        /// EF Core translates this to SQL CASE WHEN.
        /// </summary>
        private IQueryable<Project> ApplyCostModelSort(
            IQueryable<Project> query,
            SortOrder? sortOrder)
        {
            return sortOrder == SortOrder.Descending
                ? query.OrderByDescending(costModelSortKey)
                : query.OrderBy(costModelSortKey);
        }

        /// <summary>
        /// Applies sorting by funds received for each project.
        /// Delegates to <see cref="PaymentService.GetFundsReceived"/> so that the sort
        /// order is consistent with the displayed column value.
        /// </summary>
        private IQueryable<Project> ApplyFundsReceivedSort(IQueryable<Project> query, SortOrder? sortOrder)
        {
            return sortOrder == SortOrder.Descending
                ? query.OrderByDescending(x => PaymentService.GetFundsReceived(Context, x.ProjectId))
                : query.OrderBy(x => PaymentService.GetFundsReceived(Context, x.ProjectId));
        }

        /// <summary>
        /// Navigate to the add project page
        /// </summary>
        private void AddProject()
        {
            Navigation.NavigateTo($"projects/addproject/-1");
        }

        /// <summary>
        /// Navigate to the finance summary page
        /// </summary>
        private void GoToFinanceSummary()
        {
            Navigation.NavigateTo($"managefinancialitems/summary");
        }
    }
}
