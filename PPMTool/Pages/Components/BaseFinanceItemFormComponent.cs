using System;
using System.ComponentModel;
using DotNetExtensions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;
using PPMTool.Shared;
using Radzen;

namespace PPMTool.Pages.Components
{
    public abstract class BaseFinanceItemFormComponent : BaseComponent
    {
        [Parameter]
        public Project Project { get; set; }

        [Parameter]
        public new PPMToolContext Context { get; set; }

        [Parameter]
        public ILogger Logger { get; set; }

        [Parameter]
        public Action FormClosed { get; set; }

        [Inject]
        protected InvoiceService InvoiceService { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NoteService NoteService { get; set; }

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

        /// <summary>
        /// Get the ID of the item
        /// </summary>
        /// <returns></returns>
        protected abstract int GetItemId();

        /// <summary>
        /// The type of change we wish to post a note about
        /// </summary>
        protected enum FinanceItemChangeType
        {
            [Description("[ADDED]")]
            Add,
            [Description("[UPDATED]")]
            Update,
            [Description("[DELETED]")]
            Delete
        }

        /// <summary>
        /// Post a note on the project attached to the finance item to record the change
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isInvoice"></param>
        protected void PostNoteToProject(FinanceItemChangeType type, FinanceItem item)
        {
            // Create a formatted message
            string message = $"<p><span class=\"badge badge-{(item is Invoice ? "warning" : "success")}\">{(item is Invoice ? "Invoice" : "Payment")}</span><br/><em>[{type.GetDescription()}]</em>" +
                $"<br/>ID: {GetItemId()}<br />{item.Description}</p>";

            // Add the note to the DB
            NoteService.Add(Context, new Note
            {
                Author = UserService.GetByUsername(Context, ActiveUserName),
                Project = Project,
                CreatedDate = DateTime.Now,
                HtmlContent = message,
                IsFinanceInfo = true
            });
        }
    }
}
