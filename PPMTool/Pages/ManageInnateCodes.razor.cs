using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageInnateCodes : DataGridPage<InnateCode>
    {
        [Inject]
        public InnateCodeService InnateCodeService { get; set; }

        private List<InnateCode> codesToDeactivate;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = InnateCodeService;
            Loading = true;
            EnqueueLoadData(GetLoadTask);
            LogInformation($"Viewing innate code grid");
        }

        private Task GetLoadTask()
        {
            return Task.Run(() =>
            {
                dataGridEntities = InnateCodeService.GetAll(Context).ToList();
                codesToDeactivate = InnateCodeService.GetCodesToDeactivate(Context).ToList() ?? new List<InnateCode>();

            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    try
                    {
                        StateHasChanged();
                    }
                    catch
                    {

                    }

                });
            });
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

        private async Task DeleteCode(InnateCode code)
        {
            if (await DialogService.Confirm($"You are about to delete innate code {code.GetCodeAsString()}.", "Delete Code") ?? false)
            {
                await base.DeleteRow(code);
                dataGridEntityService.Delete(Context, code);
                LogInformation($"Deleted innate code {code.GetCodeAsString()}");
            }
        }

        private void EditCode(InnateCode code)
        {
            Navigation.NavigateTo($"managecodes/addinnatecode/{code.InnateCodeId}");
        }

        private void AddCode()
        {
            Navigation.NavigateTo("managecodes/addinnatecode/-1");
        }

        /// <summary>
        /// Deactivate a timesheet code and save to DB
        /// </summary>
        /// <param name="code"></param>
        private void DeactivateCode(InnateCode code)
        {
            code.IsActive = false;
            LogInformation($"Deactivating timesheet code {code.GetSensibleObjectName()}");
            InnateCodeService.Update(Context, code);
            Loading = true;
            EnqueueLoadData(GetLoadTask);
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
            InnateCode code = null;
            for (int i = 0; i < codesToDeactivate.Count - 1; ++i)
            {
                code = codesToDeactivate[i];
                code.IsActive = false;
                LogInformation($"Deactivating timesheet code {code.GetSensibleObjectName()}");
                InnateCodeService.Update(Context, code, false);
            }

            // Do last one and save changes
            code = codesToDeactivate.Last();
            code.IsActive = false;
            LogInformation($"Deactivating timesheet code {code.GetSensibleObjectName()}");
            InnateCodeService.Update(Context, code);
            Loading = true;
            EnqueueLoadData(GetLoadTask);
            StateHasChanged();
        }
    }
}