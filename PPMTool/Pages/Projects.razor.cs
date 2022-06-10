using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class Projects : ComponentBase
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        private List<Project> projects;

        private bool isSortedAscending;
        private string activeSortColumn;

        private string SetSortIcon(string columnName)
        {
            if (activeSortColumn != columnName)
            {
                return string.Empty;
            }
            if (isSortedAscending)
            {
                return "oi oi-sort-ascending";
            }
            else
            {
                return "oi oi-sort-descending";
            }
        }

        private void SortTable(string columnName)
        {
            if (columnName != activeSortColumn)
            {
                projects = projects.OrderBy(x => x.GetType().GetProperty(columnName).GetValue(x, null)).ToList();
                isSortedAscending = true;
                activeSortColumn = columnName;

            }
            else
            {
                if (isSortedAscending)
                {
                    projects = projects.OrderByDescending(x => x.GetType().GetProperty(columnName).GetValue(x, null)).ToList();
                }
                else
                {
                    projects = projects.OrderBy(x => x.GetType().GetProperty(columnName).GetValue(x, null)).ToList();
                }

                isSortedAscending = !isSortedAscending;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get people from the database
            using var context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).ToArray();
            if (proj.Count() > 0)
            {
                projects = proj.ToList();
            }
        }

        private void ProjectClicked(int id)
        {
            NavigationManager.NavigateTo($"/projectdetails/{id}");
        }
    }
}
