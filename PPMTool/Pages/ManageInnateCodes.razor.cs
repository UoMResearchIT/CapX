using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageInnateCodes : DataGridPage<InnateCode>
    {
        [Inject]
        public InnateCodeService InnateCodeService { get; set; }

        private RadzenDataGrid<InnateCode> dataGrid;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = InnateCodeService;
            dataGridEntities = InnateCodeService.GetAll(Context)
                .ToList();
            LogInformation($"Viewing innate code grid");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

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
    }
}