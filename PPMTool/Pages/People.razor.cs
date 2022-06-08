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

        private List<Person> people;
        private PeopleSortAndFilter[] sortAndFilterOptions = Enum.GetValues<PeopleSortAndFilter>();

        private PeopleSortAndFilter sortingOption;
        public PeopleSortAndFilter SortingOption
        {
            get => sortingOption;
            set
            {
                if (sortingOption != value)
                {
                    sortingOption = value;
                    UpdateSource();
                }
            }
        }

        private PeopleSortAndFilter filterOption;
        public PeopleSortAndFilter FilterOption
        {
            get => filterOption;
            set
            {
                if (filterOption != value)
                {
                    filterOption = value;
                    UpdateSource();
                }
            }
        }

        /// <summary>
        /// Method to update the people list based on the chosen filter/sorting combination
        /// </summary>
        private void UpdateSource()
        {
            // Get people from the database
            using var context = new PPMToolContext();
            var peo = PersonService.GetAll(context);
            if (peo.Count() > 0)
            {
                people = peo.ToList();

                // Apply sort
                switch (SortingOption)
                {
                    case PeopleSortAndFilter.ShortName:
                        people.Sort((x, y) => x.ShortName.CompareTo(y.ShortName));
                        break;

                    case PeopleSortAndFilter.FTE:
                        people.Sort((x, y) => x.AvailabilityFTE.CompareTo(y.AvailabilityFTE));
                        break;

                    case PeopleSortAndFilter.HourlyRate:
                        people.Sort((x, y) => x.HourlyRate.CompareTo(y.HourlyRate));
                        break;

                    case PeopleSortAndFilter.NextAvailable:
                        people.Sort((x, y) => x.NextAvailable.CompareTo(y.NextAvailable));
                        break;

                    default:
                        people.Sort((x, y) => x.Name.CompareTo(y.Name));
                        break;
                }
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            UpdateSource();
        }
    }
}
