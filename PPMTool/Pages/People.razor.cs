using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class People : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private RolesService RoleService { get; set; }

        private IEnumerable<Person> people;
        private PPMToolContext context;
        private int count;
        private int pageCount = 10;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get people from the database
            context = new PPMToolContext();
            LoadData(new LoadDataArgs());
            LogInformation($"Viewing people grid");
        }

        private void AddPerson()
        {
            Navigation.NavigateTo($"/addperson/-1");
        }

        private void ManageSkills()
        {
            Navigation.NavigateTo($"/manageskills");
        }

        private void EditPerson(Person person)
        {
            Navigation.NavigateTo($"addperson/{person.PersonId}");
        }

        // Necessary to ensure that we can filter the skills tags on the fly
        private void LoadData(LoadDataArgs args)
        {
            // Order by name by default
            var loadedPeople = PersonService.GetAll(context).OrderBy(x => x.Name).ToList();

            if (!EditAuthorised)
            {
                // Look up the username
                var role = RoleService.GetByUsername(context, AuthenticationState.User.Identity.Name.Trim().ToLower());

                // Only show the person themselves if in developer view
                loadedPeople = loadedPeople.Where(x => x == role.Person).ToList();
            }

            // Convert to queryable
            var query = loadedPeople.AsQueryable();

            if (!string.IsNullOrEmpty(args.Filter))
            {
                // Filter via the Where method
                query = query.Where(args.Filter);
            }

            // Now apply the skills tag filter
            if (args.Filters != null && args.Filters.Count() > 0)
            {
                var filter = args.Filters.FirstOrDefault(x => x.Property == "SkillTags");
                var filterValue = filter?.FilterValue as string;
                if (filter != null && filterValue != null)
                {
                    query = query.Where(x => x.SkillTags.Any(x => x.Name.Contains(filterValue)));
                }
            }

            if (!string.IsNullOrEmpty(args.OrderBy))
            {
                // Sort via the OrderBy method
                query = query.OrderBy(args.OrderBy);
            }

            // Important!!! Make sure the Count property of RadzenDataGrid is set.
            count = query.Count();

            // Perform paging via Skip and Take.
            if (args.Skip == null)
            {
                people = query.Take(pageCount).ToList();
            }
            else
            {
                people = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
            }

            Debug.WriteLine($"{people.Count()} people loaded!");
        }
    }
}
