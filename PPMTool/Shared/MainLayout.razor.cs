using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Shared
{
    public partial class MainLayout : LayoutComponentBase
    {
        private bool sidebarExpanded = true;
        private string versionString;
        private string searchTerm;
        private PPMToolContext context;
        private List<MagicBarItem> sourceData = new();
        private DotNetObjectReference<MainLayout> razorComponentReference;

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

        private LoginView loginView;
        private int totalTimesheetIssues;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            versionString = $"v{Configuration["VersionNumber"]}";
            context = ContextFactory.CreateDbContext();
            razorComponentReference = DotNetObjectReference.Create(this);
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (loginView != null && loginView.ActiveUser != null)
            {
                var oldValue = totalTimesheetIssues;
                totalTimesheetIssues = TimesheetService.GetIssueCount(context, loginView.ActiveUser?.Person?.PersonId ?? 0);
                if (oldValue != totalTimesheetIssues)
                {
                    StateHasChanged();
                }
            }
        }

        private void ClearMagicBar()
        {
            searchTerm = string.Empty;
            OnSearchTermEntered();
            StateHasChanged();
        }

        private async void OnSearchTermEntered()
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
            var matchingPeople = PersonService.GetAllShallow(context)
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

        public void Dispose()
        {
            razorComponentReference?.Dispose();
        }

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
    }
}
