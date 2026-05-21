using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
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

        [Parameter]
        public User ActiveUser { get; set; }

        [Parameter]
        public bool EditAuthorised { get; set; }

        [Inject]
        protected InvoiceService InvoiceService { get; set; }

        [Inject]
        protected PaymentService PaymentService { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NoteService NoteService { get; set; }

        protected string errorMessage;
        private BaseFinanceItem model;

        /// <summary>
        /// What to do when the form is submitted and valid. Performs an additional check to ensure the project is not null, setting an error message if it is.
        /// </summary>
        /// <returns>False if there is any further issue detected</returns>
        protected virtual bool HandleValidSubmit()
        {
            errorMessage = null;
            // Check it has a project
            if (Project == null)
            {
                errorMessage = "No project associated with this form!";
                Logger?.LogError("Project is null");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Method to close the form. This invokes the event to notify listeners.
        /// </summary>
        /// <param name="status"></param>
        protected virtual void CloseForm(bool status)
        {
            // If cancelling then reset the model
            if (!status)
            {
                ResetModel();
            }

            // Close the dialog and invoke the form closed event
            DialogService.Close(status);
            FormClosed?.Invoke();
        }

        /// <summary>
        /// Get the ID of the item
        /// </summary>
        /// <returns></returns>
        protected abstract int GetItemId();

        /// <summary>
        /// Method to set the generic model to allow resetting on close.
        /// </summary>
        /// <param name="item"></param>
        protected void SetFinanceItemModel(BaseFinanceItem item)
        {
            model = item;
        }

        /// <summary>
        /// Resets a model
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        protected void ResetModel()
        {
            // Reset the model to the original item
            if (model != null)
            {
                // Can be called from any service which inherits from the base service class
                InvoiceService.RestoreModel(Context, ref model);
            }
            else
            {
                throw new InvalidOperationException("Model is not set. Cannot reset.");
            }
        }

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
        /// <param name="item"></param>
        protected void PostNoteToProject(FinanceItemChangeType type, BaseFinanceItem item)
        {
            // Select options
            var badgeType = item is Invoice ? "warning" : (item is Payment ? "success" : "info");
            var badgeTitle = item is Invoice ? "Invoice" : (item is Payment ? "Payment" : "Funding Source");

            // Create a formatted message
            string message = $"<p><span class=\"badge badge-{badgeType}\">{badgeTitle}</span>&nbsp;<b>{type.GetDescription()} ID: {GetItemId()}</b>" +
                $"<br />{item.GetDescription()}</p>";

            // Add the note to the DB
            NoteService.Add(Context, new Note
            {
                Author = ActiveUser,
                Project = Project,
                CreatedDate = DateTime.Now,
                HtmlContent = message,
                IsFinanceInfo = true
            });
        }
    }
}
