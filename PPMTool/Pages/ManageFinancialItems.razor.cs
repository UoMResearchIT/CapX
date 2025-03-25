using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Pages.Components;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Reader,Finance")]
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
        private PaymentService PaymentService { get; set; }

        [Inject]
        private FundingSourceService FundingSourceService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        private Project selectedProject;
        private FinanceSummaryItem financeSummaryItem;
        private IEnumerable<Invoice> invoices;
        private IEnumerable<Payment> payments;
        private IEnumerable<FundingSource> sources;
        private IEnumerable<Project> projects;
        private IEnumerable<Project> cachedProjects;
        private int selectedTab;
        private RadzenDataGrid<Payment> dataGridPayments;
        private RadzenDataGrid<Invoice> dataGridInvoices;
        private RadzenDataGrid<FundingSource> dataGridSources;
        private bool exportRunning;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            EditAuthorised =
                ActiveUserRoleType == RoleType.Superuser ||
                ActiveUserRoleType == RoleType.Manager ||
                ActiveUserRoleType == RoleType.Finance;

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
        /// Got to the finance summary page
        /// </summary>
        private void GoToFinanceSummary()
        {
            Navigation.NavigateTo("managefinancialitems/summary");
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
            UpdateSummaryComponent();

            invoices = InvoiceService.GetAll(Context).OrderByDescending(x => x.KeyDate).ThenByDescending(x => x.InvoiceId);
            payments = PaymentService.GetAll(Context).OrderByDescending(x => x.KeyDate).ThenByDescending(x => x.PaymentId);
            sources = FundingSourceService.GetAll(Context).OrderByDescending(x => x.FundingSourceId);

            // Filter if a project is selected
            if (selectedProject != null)
            {
                invoices = invoices.Where(x => x.Project.ProjectId == selectedProject.ProjectId);
                payments = payments.Where(x => x.Project.ProjectId == selectedProject.ProjectId);
                sources = sources.Where(x => x.Project.ProjectId == selectedProject.ProjectId);
            }

            Debug.WriteLine($"** Selected Project = {selectedProject?.GetFullName()}. {invoices?.Count()} Invoices. {payments?.Count()} Payments.");
        }

        /// <summary>
        /// Builds a new finance summary item for the summary component
        /// </summary>
        private void UpdateSummaryComponent()
        {
            if (selectedProject != null)
            {
                financeSummaryItem = new FinanceSummaryItem(
                    selectedProject,
                    InvoiceService.GetFundsRequested(Context, selectedProject.ProjectId),
                    PaymentService.GetFundsReceived(Context, selectedProject.ProjectId)
                );
            }
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
        private void AddOrEditFinanceItem(BaseFinanceItem item)
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
                        { nameof(InvoiceFormComponent.FormClosed), () => FormClosedHandler() },
                        { nameof(InvoiceFormComponent.EditAuthorised), EditAuthorised }
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
                        { nameof(PaymentFormComponent.FormClosed), () => FormClosedHandler() },
                        { nameof(PaymentFormComponent.EditAuthorised), EditAuthorised }
                    },
                    new DialogOptions
                    {
                        ShowClose = false
                    }
                );
            }
            else if ((item == null && selectedTab == 2) || item is FundingSource)
            {
                DialogService.Open<FundingSourceFormComponent>(
                    $"{(item == null ? "Add" : "Edit")} Funding Source ({(selectedProject == null ? item.Project.GetFullName() : selectedProject.GetFullName())})",
                    new Dictionary<string, object>
                    {
                        { nameof(FundingSourceFormComponent.Source), item },
                        { nameof(FundingSourceFormComponent.Project), selectedProject == null ? item.Project : selectedProject },
                        { nameof(FundingSourceFormComponent.Logger), Logger },
                        { nameof(FundingSourceFormComponent.Context), Context },
                        { nameof(FundingSourceFormComponent.ActiveUser), ActiveUser },
                        { nameof(FundingSourceFormComponent.FormClosed), () => FormClosedHandler() },
                        { nameof(FundingSourceFormComponent.EditAuthorised), EditAuthorised }
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
            dataGridSources?.Reload();
            UpdateSummaryComponent();
            StateHasChanged();
        }

        /// <summary>
        /// Exports the finance items on two tabs in an Excel workbook
        /// </summary>
        private void ExportFinanceItems()
        {
            LogInformation($"Exporting finance items...");

            exportRunning = true;
            Task.Run(async () =>
            {
                // Run the file export on the render context
                await InvokeAsync(async () =>
                {
                    try
                    {
                        var filename = $"CapX-Finace-Item-Export{(selectedProject == null ? "" : $"-RTP{selectedProject.RTP}")}-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.xlsx";
                        var workbook = new XLWorkbook();
                        var sheet1 = workbook.Worksheets.Add("Invoices");
                        var sheet2 = workbook.Worksheets.Add("Payments");

                        // Write headers and data for Invoices sheet
                        WriteDataToSheet(sheet1, invoices);

                        // Write headers and data for Payments sheet
                        WriteDataToSheet(sheet2, payments);

                        var stream = new MemoryStream();
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        // Invoke JS on the client to download the file
                        var streamRef = new DotNetStreamReference(stream);
                        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Could not download file: {ex}");
                    }
                });

            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    LogInformation($"Export task finished {t.Status}");
                    exportRunning = false;
                    StateHasChanged();
                });
            });
        }

        /// <summary>
        /// Give any type, write the values to an Excel sheet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sheet"></param>
        /// <param name="data"></param>
        private void WriteDataToSheet<T>(IXLWorksheet sheet, IEnumerable<T> data)
        {
            // Get public, non-static properties by reflection
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Write headers
            for (int i = 0; i < properties.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = properties[i].Name;
            }

            // Write data
            for (int i = 0; i < data.Count(); i++)
            {
                var item = data.ElementAt(i);
                for (int j = 0; j < properties.Length; j++)
                {
                    var value = properties[j].GetValue(item);
                    string textValue = value is ILoggableClass ? (value as ILoggableClass)?.GetSensibleObjectName() : value?.ToString();
                    if (value is IEnumerable && value is not string)
                    {
                        textValue = "[";
                        foreach (var colItem in (value as IEnumerable))
                        {
                            textValue += colItem is ILoggableClass ? (colItem as ILoggableClass)?.GetSensibleObjectName() : colItem?.ToString();
                        }
                        textValue += "]";
                    }
                    sheet.Cell(i + 2, j + 1).Value = textValue;
                }
            }
        }
    }
}
