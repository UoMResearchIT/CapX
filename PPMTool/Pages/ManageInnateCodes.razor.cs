using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageInnateCodes : DataGridPage<InnateCode>
    {
        [Inject]
        public InnateCodeService InnateCodeService { get; set; }

        [Inject]
        public IJSRuntime JsRuntime { get; set; }

        private StatusMessage statusMessage;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = InnateCodeService;
            dataGridEntities = InnateCodeService.GetAll(context)
                .OrderBy(x => x.ActivityCode)
                .ToList();
            LogInformation($"Viewing innate code grid");
        }

        protected override async Task DeleteRow(InnateCode entity)
        {
            if (await JsRuntime.InvokeAsync<bool>("confirm", $"You are about to delete innate code {entity.GetCodeAsString()}. Are you sure?"))
            {
                await base.DeleteRow(entity);
                dataGridEntityService.Delete(context, entity);
                LogInformation($"Deleted innate code {entity.GetCodeAsString()}");
            }
        }

        protected override void OnCreateRow(InnateCode entity)
        {
            var result = InnateCodeService.Add(context, entity);
            if (result == -1)
            {
                dataGridEntities.Remove(entity);
                dataGrid.Reload();
                Reset();
                statusMessage = new StatusMessage("An entry with the same name or code already exists.", StatusMessage.MessageType.Error);
                return;
            }
            LogInformation($"Added innate code {entity.GetCodeAsString()}");
            Reset();
        }

        protected override void OnUpdateRow(InnateCode entity)
        {
            var result = InnateCodeService.Update(context, entity);
            if (result == -1)
            {
                CancelEdit(entity);
                statusMessage = new StatusMessage("An entry with the same name or code already exists.", StatusMessage.MessageType.Error);
                return;
            }
            LogInformation($"Updated innate code {entity.GetCodeAsString()}");
            Reset();
        }

        protected override void Reset()
        {
            base.Reset();
            statusMessage = null;
        }
    }
}