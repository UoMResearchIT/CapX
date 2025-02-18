using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages.Components
{
    public abstract class BaseFinanceItemFormComponent : ComponentBase
    {
        [Parameter]
        public Project Project { get; set; }

        [Parameter]
        public PPMToolContext Context { get; set; }

        [Parameter]
        public ILogger Logger { get; set; }

        [Inject]
        protected InvoiceService InvoiceService { get; set; }

        protected virtual void HandleValidSubmit()
        {
            // Check it has a project
            if (Project == null)
            {
                Logger?.LogError("Project is null");
                return;
            }
        }
    }
}
