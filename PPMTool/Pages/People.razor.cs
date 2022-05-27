using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class People : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        private List<Person> people;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get people from the database
            using var context = new PPMToolContext();
            people = PersonService.GetAll(context).ToList();
            people.Sort((x, y) => x.Name.CompareTo(y.Name));
        }
    }
}
