using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

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

        [Parameter]
        public Action FormClosed { get; set; }

        [Inject]
        protected InvoiceService InvoiceService { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        protected string errorMessage;

        protected virtual void HandleValidSubmit()
        {
            errorMessage = null;
            // Check it has a project
            if (Project == null)
            {
                errorMessage = "No project associated with this form!";
                Logger?.LogError("Project is null");
                return;
            }
        }

        protected virtual void CloseForm(bool status)
        {
            DialogService.Close(status);
            FormClosed?.Invoke();
        }
    }
}
