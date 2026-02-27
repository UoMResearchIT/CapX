using System.Diagnostics;
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

        [Inject]
        private SkillTagService TagService { get; set; }

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
                    Loading = true;
                    EnqueueLoadData(GetLoadTask);
                }
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
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
                query = query.Where(x => x.PersonId == ActiveUser.Person.PersonId);
            }

            // Apply filter
            if (!string.IsNullOrEmpty(args.Filter))
            {
                // Filter via the Where method
                query = query.Where(args.Filter);
            }

            // Now apply the custom filters
            if (args.Filters is { } filters && filters.Any())
            {
                var filter = args.Filters.FirstOrDefault(x => x.Property == "SkillTags");
                var filterValue = filter?.FilterValue as string;
                if (filter != null && filterValue != null)
                {
                    query = query.Where(x => x.OwnedSkills.Any(x => x.SkillTag.Name.Trim().ToLower().Contains(filterValue.Trim().ToLower())));
                }

                filter = filters.FirstOrDefault(f => f.Property == "LineManager");
                filterValue = (filter?.FilterValue as string)?.Trim();
                if (!string.IsNullOrEmpty(filterValue))
                {
                    var filterValueLower = filterValue.ToLower();
                    query = query.Where(x =>
                        x.LineManager != null &&
                        ((x.LineManager.Name ?? "").Trim().ToLower()).Contains(filterValueLower));
                }
            }

            // Apply custom order
            if (!string.IsNullOrEmpty(args.OrderBy))
            {
                if (args.Sorts is { } sorts && sorts.Any())
                {
                    var sort = args.Sorts?.First();
                    if (sort.Property == "SkillsCount")
                    {
                        // Apply the ordering process on skills count manually
                        if (sort.SortOrder == SortOrder.Ascending)
                        {
                            query = query.OrderBy(x => TagService.GetCountForPerson(Context, x.PersonId));
                        }
                        else
                        {
                            query = query.OrderByDescending(x => TagService.GetCountForPerson(Context, x.PersonId));
                        }
                    }
                    else if (sort.Property == "LineManager")
                    {
                        query = sort.SortOrder == SortOrder.Ascending ?
                            query.OrderBy(x => x.LineManager != null ? x.LineManager.Name : "") :
                            query.OrderByDescending(x => x.LineManager != null ? x.LineManager.Name : "");
                    }
                }
                else
                {
                    // Sort via the OrderBy method
                    query = query.OrderBy(args.OrderBy);
                }
            }

            // Important!!! Make sure the Count property of RadzenDataGrid is set.
            var data = query.ToList();
            count = data.Count();

            // Perform paging via Skip and Take.
            if (args.Skip == null)
            {
                people = data.Take(pageCount).ToList();
            }
            else
            {
                people = data.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
            }

            Debug.WriteLine($"** {data.Count()} people loaded. {people.Count()} displayed.");
        }
    }
}