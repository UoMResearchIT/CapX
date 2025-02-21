using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Reader")]
    public partial class ManageFinancialItems : BasePage
    {
        [Parameter]
        [SupplyParameterFromQuery(Name = "rtp")]
        public int? RTP { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private InvoiceService InvoiceService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        private Project selectedProject;
        private IEnumerable<Invoice> invoices;
        private IEnumerable<Payment> payments;
        private IEnumerable<Project> projects;
        private IEnumerable<Project> cachedProjects;
        private int selectedTab;
        private RadzenDataGrid<Payment> dataGridPayments;
        private RadzenDataGrid<Invoice> dataGridInvoices;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (RTP != null)
            {
                selectedProject = ProjectService.GetByRTP(Context, RTP);
            }

            // Cache the projects
            cachedProjects = ProjectService.GetAllShallow(Context).OrderBy(x => x.RTP).ToList();
            LoadProjectDropdownWithFilter(null);
            LoadData();

            LogInformation("Viewing project finance");
        }

        /// <summary>
        /// Invoked when the project selection is changed to load the datagrid items again
        /// </summary>
        /// <param name="value"></param>
        private void OnProjectChange(object value)
        {
            // Load invoices and payments for the selected project
            LoadData();

            LogInformation($"Viewing project finance for {selectedProject?.GetFullName()}");

            Debug.WriteLine($"** {(value as Project)?.GetFullName() ?? "Nothing"}");
        }

        /// <summary>
        /// Navigate to project details page for selected project
        /// </summary>
        private void GoToProjectDetails()
        {
            if (selectedProject != null)
            {
                Navigation.NavigateTo($"projects/projectdetails/{selectedProject.ProjectId}");
            }
        }

        /// <summary>
        /// Opens the URL to the invoice document in a new tab
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        private async Task ViewInvoiceDocumentAsync(Invoice invoice)
        {
            await JSRuntime.InvokeAsync<object>("open", $"{invoice.InvoiceUrl}", "_blank");
        }

        /// <summary>
        /// Loads the data for the datagrids
        /// </summary>
        private void LoadData()
        {
            invoices = InvoiceService.GetAll(Context).OrderByDescending(x => x.KeyDate).ThenByDescending(x => x.InvoiceId);
            payments = InvoiceService.GetAllPayments(Context).OrderByDescending(x => x.KeyDate).ThenByDescending(x => x.PaymentId);

            // Filter if a project is selected
            if (selectedProject != null)
            {
                invoices = invoices.Where(x => x.Project.ProjectId == selectedProject.ProjectId);
                payments = payments.Where(x => x.Project.ProjectId == selectedProject.ProjectId);
            }

            Debug.WriteLine($"** Selected Project = {selectedProject?.GetFullName()}. {invoices?.Count()} Invoices. {payments?.Count()} Payments.");
        }

        /// <summary>
        /// Method to take the cached project list and filter the dropdown source to only those which contain the search terms
        /// </summary>
        /// <param name="args"></param>
        void LoadProjectDropdownWithFilter(LoadDataArgs args)
        {
            var temp = cachedProjects;
            if (!string.IsNullOrEmpty(args?.Filter))
            {
                Debug.WriteLine($"** Filter projects on: {args?.Filter}");
                temp = temp.Where(x => x.GetFullName().ToLower().Contains(args.Filter.ToLower()));
                Debug.WriteLine($"** {temp.Count()} matched.");
            }
            projects = temp.ToList();
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Method to load the dialog to add or edit a payment or invoice
        /// </summary>
        /// <param name="item"></param>
        private void AddOrEditPaymentOrInvoice(FinanceItem item)
        {
            if ((item == null && selectedTab == 0) || item is Invoice invoice)
            {
                DialogService.Open<InvoiceFormComponent>(
                    $"{(item == null ? "Add" : "Edit")} Invoice ({(selectedProject == null ? item.Project.GetFullName() : selectedProject.GetFullName())})",
                    new Dictionary<string, object>
                    {
                        { nameof(InvoiceFormComponent.Invoice), item },
                        { nameof(InvoiceFormComponent.Project), selectedProject == null ? item.Project : selectedProject },
                        { nameof(InvoiceFormComponent.Logger), Logger },
                        { nameof(InvoiceFormComponent.Context), Context },
                        { nameof(InvoiceFormComponent.ActiveUser), ActiveUser },
                        { nameof(PaymentFormComponent.FormClosed), () => FormClosedHandler() }
                    },
                    new DialogOptions
                    {
                        ShowClose = false
                    }
                );
            }
            else if ((item == null && selectedTab == 1) || item is Payment)
            {
                DialogService.Open<PaymentFormComponent>(
                    $"{(item == null ? "Add" : "Edit")} Payment ({(selectedProject == null ? item.Project.GetFullName() : selectedProject.GetFullName())})",
                    new Dictionary<string, object>
                    {
                        { nameof(PaymentFormComponent.Payment), item },
                        { nameof(PaymentFormComponent.Project), selectedProject == null ? item.Project : selectedProject },
                        { nameof(PaymentFormComponent.Logger), Logger },
                        { nameof(PaymentFormComponent.Context), Context },
                        { nameof(PaymentFormComponent.ActiveUser), ActiveUser },
                        { nameof(PaymentFormComponent.FormClosed), () => FormClosedHandler() }
                    },
                    new DialogOptions
                    {
                        ShowClose = false
                    }
                );
            }
            else
            {
                LogError("Unkown finance item type!");
            }
        }

        /// <summary>
        /// Callback which runs when the form closes
        /// </summary>
        private void FormClosedHandler()
        {
            dataGridInvoices?.Reload();
            dataGridPayments?.Reload();
        }
    }
}
