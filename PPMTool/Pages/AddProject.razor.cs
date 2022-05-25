using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class AddProject : ComponentBase
    {
        [Inject]
        private ProjectService ProjectService { get; }

        protected override async Task OnInitializedAsync()
        {
        }
    }
}
