using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
    }
}