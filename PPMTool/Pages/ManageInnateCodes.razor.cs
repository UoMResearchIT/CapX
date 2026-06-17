// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;
using static PPMTool.Services.InnateCodeService;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageInnateCodes : DataGridPage<InnateCode>
    {
        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private IConfiguration Configuration { get; set; }

        private List<CodeToDeactivate> codesToDeactivate;
        private List<int> codesWithBookings = new List<int>();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            dataGridEntityService = InnateCodeService;
            Loading = true;
            await Task.Yield();
            await LoadData();
            LogInformation($"Viewing innate code grid");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (!firstRender) return;

            // Set the default filter value of the columns to be just the active values
            if (dataGrid != null)
            {
                var col = dataGrid.ColumnsCollection.FirstOrDefault(x => x.Property == "IsActive");
                if (col != null)
                {
                    col.SetFilterValue(true);
                    dataGrid.Reload();
                }
            }
        }

        /// <summary>
        /// Load the data for the grid
        /// </summary>
        /// <returns></returns>
        private async Task LoadData()
        {
            dataGridEntities = InnateCodeService.GetAll(Context).ToList();
            codesToDeactivate = (await InnateCodeService.GetCodesToDeactivateAsync(Context)).ToList() ?? new List<CodeToDeactivate>();
            codesWithBookings = (await TimesheetService.GetCodeIdsWithBookingsAsync(Context)).ToList() ?? new List<int>();
            Loading = false;
        }

        /// <summary>
        /// Delete a timesheet code and save to DB
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private async Task DeleteCode(InnateCode code)
        {
            if (await DialogService.Confirm($"You are about to delete innate code {code.GetCodeAsString()}.", "Delete Code") ?? false)
            {
                await base.DeleteRow(code);
                dataGridEntityService.Delete(Context, code);
                LogInformation($"Deleted innate code {code.GetCodeAsString()}");
            }
        }

        /// <summary>
        /// Navigate to the edit page
        /// </summary>
        /// <param name="code"></param>
        private void EditCode(InnateCode code)
        {
            Navigation.NavigateTo($"managecodes/addinnatecode/{code.InnateCodeId}");
        }

        /// <summary>
        /// Navigate to the add page
        /// </summary>
        private void AddCode()
        {
            Navigation.NavigateTo("managecodes/addinnatecode/-1");
        }

        /// <summary>
        /// Deactivate a timesheet code and save to DB
        /// </summary>
        /// <param name="toDeactivate"></param>
        private void DeactivateCode(CodeToDeactivate toDeactivate)
        {
            var code = InnateCodeService.GetById(Context, toDeactivate.InnateCodeId);
            code.IsActive = false;
            code.DeactivateAllTasks();
            LogInformation($"Deactivating timesheet code {code.GetSensibleObjectName()}");
            InnateCodeService.Update(Context, code);
            Loading = true;
            EnqueueLoadData(LoadData);
            StateHasChanged();
        }

        /// <summary>
        /// Loop over all and deactivate
        /// </summary>
        private void DeactivateAll()
        {
            if (codesToDeactivate == null || codesToDeactivate.Count == 0)
            {
                return;
            }

            // Go over all codes and set their state
            CodeToDeactivate toDeactive = null;
            InnateCode code = null;
            for (int i = 0; i < codesToDeactivate.Count - 1; ++i)
            {
                toDeactive = codesToDeactivate[i];
                code = InnateCodeService.GetById(Context, toDeactive.InnateCodeId);
                code.IsActive = false;
                code.DeactivateAllTasks();
                LogInformation($"Deactivating timesheet code {code.GetSensibleObjectName()}");
                InnateCodeService.Update(Context, code, false);
            }

            // Do last one and save changes
            toDeactive = codesToDeactivate.Last();
            code = InnateCodeService.GetById(Context, toDeactive.InnateCodeId);
            code.IsActive = false;
            code.DeactivateAllTasks();
            LogInformation($"Deactivating timesheet code {code.GetSensibleObjectName()}");
            InnateCodeService.Update(Context, code);
            Loading = true;
            EnqueueLoadData(LoadData);
            StateHasChanged();
        }
    }
}