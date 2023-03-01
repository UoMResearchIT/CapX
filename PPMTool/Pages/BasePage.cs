using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace PPMTool.Pages
{
    [Authorize]
    public class BasePage : ComponentBase
    {
        [Inject]
        protected ILogger<AddPerson> Logger { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }

        protected bool isLoading;
    }
}
