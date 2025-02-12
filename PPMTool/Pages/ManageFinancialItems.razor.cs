using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using PPMTool.Data.Entities;
using PPMTool.Pages.Components;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize("Superuser,Manager,Reader")]
    public partial class ManageFinancialItems : BasePage
    {
        [FromQuery(Name = "rtp")]
        public string RTP { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        private Project selectedProject;
        private IEnumerable<Invoice> invoices;
        private IEnumerable<Payment> payments;
        private IEnumerable<Project> projects;
        private string itemType;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        private void OnProjectChange(object value)
        {
            // Load invoices and payments for the selected project
            Debug.WriteLine($"** {value?.ToString() ?? "Nothing"}");
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
