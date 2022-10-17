using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
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

        private Project project;

        [Parameter]
        public int? ProjectID { get; set; }

        private List<SubTask> Data { get; set; }
        private ApexChartOptions<SubTask> options;
        private PPMToolContext context;
        private int count;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ProjectID != null)
            {
                context = new PPMToolContext();
                project = ProjectService.GetById(context, ProjectID);
                Data = project.SubTasks.ToList();
                count = Data.Count;

                options = new ApexChartOptions<SubTask>
                {
                    PlotOptions = new PlotOptions
                    {
                        Bar = new PlotOptionsBar
                        {
                            Horizontal = true
                        }
                    }
                };
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
            Data = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
        }
    }
}
