using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class People : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        private IEnumerable<Person> people;

        private bool IsLoading { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;

            await Task.Run(async () =>
            {
                // Get people from the database
                using var context = new PPMToolContext();
                var peo = PersonService.GetAll(context);
                if (peo.Count() > 0)
                {
                    // Update all the next available dates of the people
                    await Task.Delay(2000);



                    people = peo;
                }
            }).ContinueWith(t =>
            {
                IsLoading = false;
                StateHasChanged();
            });
            
        }
    }
}
