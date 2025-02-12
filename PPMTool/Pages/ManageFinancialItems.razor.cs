using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Pages.Components;
using PPMTool.Services;
using Radzen;

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

        private Project selectedProject;
        private IEnumerable<Invoice> invoices;
        private IEnumerable<Payment> payments;
        private IEnumerable<Project> projects;
        private IEnumerable<Project> cachedProjects;

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

        private void OnProjectChange(object value)
        {
            // Load invoices and payments for the selected project
            LoadData();

            Debug.WriteLine($"** {(value as Project)?.GetFullName() ?? "Nothing"}");
        }

        /// <summary>
        /// Loads the data for the datagrids
        /// </summary>
        private void LoadData()
        {
            invoices = InvoiceService.GetAll(Context);
            payments = InvoiceService.GetAllPayments(Context);

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

        private void AddPaymentOrInvoice()
        {
            DialogService.Open<PaymentFormComponent>("Add Payment", new Dictionary<string, object> { { "Payment", new Payment() } });
        }

        private void EditPaymentOrInvoice(FinanceItem item)
        {
            DialogService.Open<PaymentFormComponent>("Edit Payment", new Dictionary<string, object> { { "Payment", item } });
        }
    }
}
