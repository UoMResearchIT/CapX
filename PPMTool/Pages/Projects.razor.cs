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
    public partial class Projects : ComponentBase
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private Project[] projects;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            using var context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).ToArray();
            if (proj.Count() > 0) projects = proj;
        }
    }
}
