using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class ProjectDetails : ComponentBase
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private Project projectModel;

        [Parameter]
        public int? ProjectID { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ProjectID != null)
            {
                using var context = new PPMToolContext();
                projectModel = ProjectService.GetById(context, ProjectID);
            }
        }
    }
}
