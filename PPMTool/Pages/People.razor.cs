// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class People : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private SkillTagService TagService { get; set; }

        private IEnumerable<Person> people;
        private int count;
        private bool skillsEnabled;

        private bool includeLeavers;
        public bool IncludeLeavers
        {
            get => includeLeavers;
            private set
            {
                if (value != includeLeavers)
                {
                    includeLeavers = value;
                    Loading = true;
                    EnqueueLoadData(GetLoadTask);
                }
            }
        }

        protected override string GetSessionStorageTag() => "people";

        protected override void OnInitialized()
        {
            base.OnInitialized();
            skillsEnabled = FeatureService.IsFeatureEnabled(FeatureType.Skills);

            Loading = true;
            EnqueueLoadData(GetLoadTask);

            LogInformation($"Viewing people grid");
        }

        /// <summary>
        /// Generates a task to call the load data method
        /// </summary>
        /// <returns></returns>
        private Task GetLoadTask()
        {
            return Task.Run(() =>
            {
                // Get people from the database
                LoadData(new LoadDataArgs());
            })
                .ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });
            });
        }

        /// <summary>
        /// Callback for add person clicked
        /// </summary>
        private void AddPerson()
        {
            Navigation.NavigateTo($"people/addperson/-1");
        }

        /// <summary>
        /// Callback for edit person clicked
        /// </summary>
        /// <param name="person"></param>
        private void EditPerson(Person person)
        {
            Navigation.NavigateTo($"people/addperson/{person.PersonId}");
        }

        /// <summary>
        /// Manual load of datagrid data. Necessary to ensure that we can filter the skills tags on the fly.
        /// </summary>
        /// <param name="args"></param>
        private void LoadData(LoadDataArgs args)
        {
            // Order by name by default
            var query = PersonService.GetAll(Context).OrderBy(x => x.Name).AsQueryable();

            // Reduce to just current people
            if (!IncludeLeavers)
            {
                query = query.Where(x => x.EndDate == null || x.EndDate >= DateTime.Now);
            }

            if (!EditAuthorised)
            {
                // Only show the person themselves if in developer view
                query = query.Where(x => ActiveUser.Person != null && x.PersonId == ActiveUser.Person!.PersonId);
            }

            // ---- GRID FILTERING ----
            if (!string.IsNullOrWhiteSpace(args.Filter))
            {
                query = query.Where(args.Filter);
            }

            // ---- SORTING ----
            if (args.Sorts != null && args.Sorts.Count() > 0)
            {
                var sort = args.Sorts.First();

                // Special-case sort
                if (sort.Property == "SkillsCount")
                {
                    query = sort.SortOrder == SortOrder.Ascending
                        ? query.OrderBy(x => x.OwnedSkills.Count())
                        : query.OrderByDescending(x => x.OwnedSkills.Count());
                }
                else
                {
                    query = query.OrderBy(args.OrderBy);
                }
            }
            else if (!string.IsNullOrWhiteSpace(args.OrderBy))
            {
                query = query.OrderBy(args.OrderBy);
            }

            // ---- COUNT BEFORE PAGING ----
            count = query.Count();

            // ---- PAGING ----
            var skip = args.Skip ?? 0;
            var take = args.Top ?? PageCount;

            people = query.Skip(skip).Take(take).ToList();

            Debug.WriteLine($"** {count} people loaded. {people.Count()} displayed.");
        }

    }
}