using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class People : ComponentBase
    {
        [Inject]
        private PersonService PersonService { get; set; }

        private Person[] people;

        protected override async Task OnInitializedAsync()
        {
            // TODO: Get people from the database
        }
    }
}
