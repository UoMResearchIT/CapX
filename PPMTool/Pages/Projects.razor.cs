using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class Projects : ComponentBase
    {
        [Inject]
        private ProjectService ProjectService { get; }

        private Project[] projects;

        protected override async Task OnInitializedAsync()
        {
        }
    }
}
