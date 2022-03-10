using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;

namespace PPMTool.Pages
{
    public partial class Capacity : ComponentBase
    {
        private WeatherForecast[] data;

        protected override async Task OnInitializedAsync()
        {
            data = await ForecastService.GetForecastAsync(DateTime.Now);
        }
    }
}
