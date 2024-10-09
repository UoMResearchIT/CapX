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

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = InnateCodeService;
            dataGridEntities = InnateCodeService.GetAll(context)
                .ToList();
            LogInformation($"Viewing innate code grid");
        }

        private async Task DeleteCode(InnateCode code)
        {
            if (await DialogService.Confirm($"You are about to delete innate code {code.GetCodeAsString()}.", "Delete Code") ?? false)
            {
                await base.DeleteRow(code);
                dataGridEntityService.Delete(context, code);
                LogInformation($"Deleted innate code {code.GetCodeAsString()}");
            }
        }

        private void EditCode(InnateCode code)
        {
            Navigation.NavigateTo($"/addinnatecode/{code.InnateCodeId}");
        }

        private void AddCode()
        {
            Navigation.NavigateTo("/addinnatecode/-1");
        }
    }
}