using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        private InvoiceService InvoiceService { get; set; }

        private IEnumerable<Project> projects;
        private RadzenDataGrid<Project> dataGrid;

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
            List<Project> proj;
            if (ActiveUserRoleType == RoleType.Developer)
            {
                proj = ProjectService.GetAll(Context)
                    .Where(x => x.SubTasks.Any(x => x.AssignedResources.Any(x => x.Person?.PersonId == ActiveUser?.Person?.PersonId)))
                    .OrderBy(x => x.RTP).ToList();
            }
            else
            {
                proj = ProjectService.GetAll(Context).OrderBy(x => x.RTP).ToList();
            }

            // Remove the ones that are not active if necessary
            if (!includeFinished) proj = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled()).ToList();


            // TODO: Filtering and sorting






            // Assign data for the data grid
            projects = proj;

            Debug.WriteLine($"** {proj.Count()} projects loaded.");
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
