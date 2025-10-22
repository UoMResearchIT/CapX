using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Shared
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        protected IDbContextFactory<PPMToolContext> ContextFactory { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private ILogger<MainLayout> Logger { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        [Inject]
        private SkillTagService SkillTagService { get; set; }

        /// <summary>
        /// Whether there are any buttons or error messages to show in the action bar.
        /// </summary>
        public bool ShowActionBar => Buttons.Any() || ErrorMessage != null;

        /// <summary>
        /// Error message to show
        /// </summary>
        public StatusMessage ErrorMessage { get; private set; }

        // Action buttons to show in the action bar
        public List<ActionButton> Buttons { get; } = new();

        private bool sidebarExpanded = true;
        private string versionString;
        private string searchTerm;
        private List<MagicBarItem> sourceData = new();
        private DotNetObjectReference<MainLayout> razorComponentReference;
        private LoginView loginView;
        private int totalTimesheetIssues;
        private int totalTimesheetCodesToDeactivate;
        private int totalIncompleteSkills;
        private bool adminMenuItemExpanded = false;
        private int? activeUserId;
        private RoleType activeUserRoleType;

        /// <summary>
        /// Specific object for populating the magic bar popup
        /// </summary>
        private class MagicBarItem
        {
            public int EntityId { get; }

            public string Name { get; }

            public string DisplayName { get; }

            public Type ItemType { get; }

            public MagicBarItem(Person person)
            {
                EntityId = person.PersonId;
                Name = person.Name;
                DisplayName = $"{person.Name} ({person.ShortName})";
                ItemType = typeof(Person);
            }

            public MagicBarItem(Project project)
            {
                EntityId = project.ProjectId;
                Name = project.Name;
                DisplayName = $"{project.GetFullName()} ({project.PI})";
                ItemType = typeof(Project);
            }
        }

        /// <summary>
        /// Model of an action button
        /// </summary>
        public class ActionButton
        {
            /// <summary>
            /// Text on the button
            /// </summary>
            public string Text { get; set; } = "Save";

            /// <summary>
            /// Icon on the button
            /// </summary>
            public string Icon { get; set; } = "check";

            /// <summary>
            /// Action to invoke when clicked
            /// </summary>
            public Action OnClick { get; set; }

            /// <summary>
            /// Whether the button should be disabled or not
            /// </summary>
            public bool Disabled { get; set; }

            /// <summary>
            /// Style of the button
            /// </summary>
            public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            versionString = $"v{Configuration["VersionNumber"]}";
            razorComponentReference = DotNetObjectReference.Create(this);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            // Update the badges in the sidebar if necessary
            if (loginView != null && loginView.ActiveUser != null)
            {
                using (var context = ContextFactory.CreateDbContext())
                {
                    // Store the active user ID
                    activeUserId = loginView.ActiveUser?.Person?.PersonId;
                    activeUserRoleType = loginView.ActiveUser?.RoleType ?? RoleType.None;

                    // Update timesheet badge
                    var oldTimesheetIssuesValue = totalTimesheetIssues;
                    totalTimesheetIssues = await TimesheetService.GetIssueCountAsync(context, activeUserId ?? 0);

                    // Update timesheet code badge
                    var oldTimesheetCodeIssuesValue = totalTimesheetCodesToDeactivate;
                    totalTimesheetCodesToDeactivate = (await InnateCodeService.GetCodesToDeactivateAsync(context)).Count();

                    // Update skills badge
                    var oldIncompleteSkillsValue = totalIncompleteSkills;
                    totalIncompleteSkills = await SkillTagService.GetIncompleteRecordCountAsync(context, activeUserId ?? 0);

                    // Only call state has changed when something has changed
                    if (oldTimesheetCodeIssuesValue != totalTimesheetCodesToDeactivate ||
                        oldTimesheetIssuesValue != totalTimesheetIssues ||
                        oldIncompleteSkillsValue != totalIncompleteSkills)
                    {
                        // Only expand the Admin menu item if SuperUser accessing the page and there are codes to be deactivated
                        adminMenuItemExpanded = totalTimesheetCodesToDeactivate > 0 && loginView.ActiveUser.RoleType == Enums.RoleType.Superuser;

                        // Now force a re-draw
                        StateHasChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Clear the magic bar
        /// </summary>
        private void ClearMagicBar()
        {
            searchTerm = string.Empty;
            OnSearchTermEntered();
            StateHasChanged();
        }

        /// <summary>
        /// Kicked off when a search term is entered into the magic bar
        /// </summary>
        private async void OnSearchTermEntered()
        {
            using (var context = ContextFactory.CreateDbContext())
            {
                // If nothing being typed
                if (string.IsNullOrWhiteSpace(searchTerm.Trim()))
                {
                    // Hide the popup if it is visible
                    Logger?.LogInformation("Hiding magic bar popup...");
                    await JSRuntime.InvokeVoidAsync("toggleAutocompletePopup", false, null, razorComponentReference);
                    return;
                }

                // Pull the sources from the DB
                var matchingPeople = (await PersonService.GetAllShallowAsync(context))
                .Where(x =>
                    x.Name.ToLower().Contains(searchTerm.Trim().ToLower()) ||
                    x.ShortName.ToLower().Contains(searchTerm.Trim().ToLower())
                );
                var matchingProjects = ProjectService.GetAllShallow(context)
                .Where(x =>
                    x.GetFullName().ToLower().Contains(searchTerm.Trim().ToLower()) ||
                    x.PI.ToLower().Contains(searchTerm.Trim().ToLower())
                );

                // Add to source
                sourceData.Clear();
                foreach (var person in matchingPeople)
                {
                    sourceData.Add(new MagicBarItem(person));
                }
                foreach (var project in matchingProjects)
                {
                    sourceData.Add(new MagicBarItem(project));
                }
                sourceData.OrderBy(x => x.DisplayName);

                Debug.WriteLine($"** Source data contains {sourceData.Count} items!");

                // Send the source data to JS to have it display it in a popup
                Logger?.LogInformation("Updating magic bar popup...");
                await JSRuntime.InvokeVoidAsync("toggleAutocompletePopup", sourceData.Count > 0, sourceData.Select(x => x.DisplayName), razorComponentReference);
            }
        }

        /// <summary>
        /// Can be invoked from JS when an item in the popup is clicked.
        /// Navigates to the apporpriate page
        /// </summary>
        /// <param name="selectedItem"></param>
        [JSInvokable]
        public void OnItemSelected(string selectedItem)
        {
            Debug.WriteLine($"** Selected item: {selectedItem}");
            var match = sourceData.FirstOrDefault(x => x.DisplayName == selectedItem);
            if (match != null)
            {
                if (match.ItemType == typeof(Project))
                {
                    Navigation.NavigateTo($"projects/projectdetails/{match.EntityId}");
                }
                else
                {
                    Navigation.NavigateTo($"people/addperson/{match.EntityId}");
                }
            }
            ClearMagicBar();
        }

        /// <summary>
        /// Called when dispose is called on the layout itself
        /// </summary>
        public void Dispose()
        {
            razorComponentReference?.Dispose();
        }

        /// <summary>
        /// Helper to set buttons
        /// </summary>
        /// <param name="buttons"></param>
        public void SetButtons(IEnumerable<ActionButton> buttons)
        {
            Buttons.Clear();
            Buttons.AddRange(buttons);
            StateHasChanged();
        }

        /// <summary>
        /// Set the error message
        /// </summary>
        /// <param name="message"></param>
        public void SetErrorMessage(StatusMessage message)
        {
            ErrorMessage = message;
            StateHasChanged();
        }

        /// <summary>
        /// Reset the action bar completely to initial state
        /// </summary>
        public void Reset()
        {
            Buttons.Clear();
            ErrorMessage = null;
            StateHasChanged();
        }

        /// <summary>
        /// Clears the error message
        /// </summary>
        public void ClearErrorMessage()
        {
            ErrorMessage = null;
            StateHasChanged();
        }
    }
}
