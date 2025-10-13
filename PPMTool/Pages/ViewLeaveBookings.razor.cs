using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager")]
    public partial class ViewleaveBookings: BasePage
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
    }
}
