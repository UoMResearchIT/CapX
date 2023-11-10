using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace PPMTool.Pages
{
    [Authorize]
    public abstract class BasePage : ComponentBase
    {
        [Inject]
        protected ILogger Logger { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }
    }
}
