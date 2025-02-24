using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class People : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        private bool tableEmpty;
        private IEnumerable<Person> people;
        private int count;
        private int pageCount = 10;

        private bool includeLeavers;
        public bool IncludeLeavers
        {
            get => includeLeavers;
            private set
            {
                if (value != includeLeavers)
                {
                    includeLeavers = value;
                    LoadData(new LoadDataArgs());
                }
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get people from the database
            LoadData(new LoadDataArgs());
            LogInformation($"Viewing people grid");
        }

        private void AddPerson()
        {
            Navigation.NavigateTo($"people/addperson/-1");
        }

        private void EditPerson(Person person)
        {
            Navigation.NavigateTo($"people/addperson/{person.PersonId}");
        }

        // Necessary to ensure that we can filter the skills tags on the fly
        private void LoadData(LoadDataArgs args)
        {
            // Order by name by default
            var loadedPeople = PersonService.GetAll(Context).OrderBy(x => x.Name).ToList();

            // Reduce to just current people
            if (!IncludeLeavers)
            {
                loadedPeople = loadedPeople.Where(x => x.EndDate == null || x.EndDate >= DateTime.Now).ToList();
            }

            if (!EditAuthorised)
            {
                // Only show the person themselves if in developer view
                loadedPeople = loadedPeople.Where(x => x.PersonId == ActiveUser?.Person?.PersonId).ToList();
            }

            // Set the table empty flag
            tableEmpty = loadedPeople.Count == 0;

            Debug.WriteLine($"** {loadedPeople.Count()} people loaded!");

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
        }
    }
}