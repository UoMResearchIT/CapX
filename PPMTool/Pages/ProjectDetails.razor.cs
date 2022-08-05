using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

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

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ProjectID != null)
            {
                context = new PPMToolContext();
                project = ProjectService.GetById(context, ProjectID);
                Data = project.SubTasks.ToList();

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
    }
}
