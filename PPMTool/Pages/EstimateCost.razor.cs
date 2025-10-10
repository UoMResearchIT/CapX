using Microsoft.AspNetCore.Authorization;
using PPMTool.Enums;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class EstimateCost : BasePage
    {
        private CostModel costModel;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = false;
        }
    }
}
