using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageFinancialReferences : DataGridPage<FinancialReference>
    {
        [Inject]
        public FinancialReferenceService FinancialReferenceService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = FinancialReferenceService;
            dataGridEntities = FinancialReferenceService.GetAll(context)
                .OrderBy(x => x.FinancialYear)
                .ToList();
            LogInformation($"Viewing finref grid");
        }

        protected override async Task DeleteRow(FinancialReference entity)
        {
            if (await DialogService.Confirm($"You are about to delete reference {entity.GetSensibleObjectName()}.", "Delete") ?? false)
            {
                await base.DeleteRow(entity);
                dataGridEntityService.Delete(context, entity);
                LogInformation($"Deleted finref {entity.GetSensibleObjectName()}");
            }
        }

        protected override void OnCreateRow(FinancialReference entity)
        {
            var result = FinancialReferenceService.Add(context, entity);
            if (result == -1)
            {
                dataGridEntities.Remove(entity);
                dataGrid.Reload();
                Reset();
                errorMessage = new StatusMessage("An entry for the same financial year already exists.", StatusMessage.MessageType.Error);
                return;
            }
            LogInformation($"Added finref {entity.GetSensibleObjectName()}");
            Reset();
        }

        protected override void OnUpdateRow(FinancialReference entity)
        {
            var result = FinancialReferenceService.Update(context, entity);
            if (result == -1)
            {
                CancelEdit(entity);
                errorMessage = new StatusMessage("An entry for the same financial year already exists.", StatusMessage.MessageType.Error);
                return;
            }
            LogInformation($"Updated finref {entity.GetSensibleObjectName()}");
            Reset();
        }
    }
}