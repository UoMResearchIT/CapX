using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexCharts;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public abstract class BaseCapacityPage : BasePage
    {
        /// <summary>
        /// Represents a model for a particular gantt chart
        /// </summary>
        public class ChartModel
        {
            public IList<ChartItem> ConfirmedChartItems { get; set; }
            public IList<ChartItem> ProvisionalChartItems { get; set; }
            public string ChartTitle { get; set; }
            public ApexChartOptions<ChartItem> ChartOptions { get; set; }
        }

        [Inject]
        protected PersonService PersonService { get; set; }

        [Inject]
        protected ProjectService ProjectService { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected ISessionStorageService SessionStorage { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "filterid")]
        public int? FilterPersonId { get; set; }

        private IEnumerable<string> chosenPeople = new List<string>();
        public IEnumerable<string> ChosenPeople
        {
            get => chosenPeople;
            set
            {
                if (chosenPeople != value)
                {
                    chosenPeople = value;
                    SavePeopleState();
                }
            }
        }

        private bool includeUnFunded = true;
        public bool IncludeUnFunded
        {
            get => includeUnFunded;
            set
            {
                if (includeUnFunded != value)
                {
                    includeUnFunded = value;
                    SessionStorage.SetItemAsync<bool?>($"{GetSessionStorageTag()}-include-unfunded", includeUnFunded);

                    // Update the chart source
                    ConfigureChartSource();
                }
            }
        }

        private bool includeLeavers = false;
        public bool IncludeLeavers
        {
            get => includeLeavers;
            set
            {
                if (includeLeavers != value)
                {
                    includeLeavers = value;
                    SessionStorage.SetItemAsync<bool?>($"{GetSessionStorageTag()}-include-leavers", includeLeavers);

                    // Refresh the people source
                    ReloadDropDownSources();

                    // Update the chart source
                    ConfigureChartSource();

                }
            }
        }

        private bool includeFinished = false;
        public bool IncludeFinished
        {
            get => includeFinished;
            set
            {
                if (includeFinished != value)
                {
                    includeFinished = value;
                    SessionStorage.SetItemAsync<bool?>($"{GetSessionStorageTag()}-include-finished", includeFinished);

                    // Update the chart source
                    ConfigureChartSource();
                }
            }
        }



        protected CancellationTokenSource configureChartTaskCancellationTokenSource = null;
        protected Task configureChartTask = null;
        protected IList<ChartModel> chartModels = new List<ChartModel>();
        protected IEnumerable<Project> cachedProjects;
        protected IEnumerable<Person> cachedPeople;
        protected IDictionary<object, IEnumerable<Assignment>> groupedAssignments;
        protected List<Person> people;
        protected List<Person> filteredPeople;

        /// <summary>
        /// Generate chart items for a given person in person mode
        /// </summary>
        /// <param name="person"></param>
        /// <param name="assignments"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        protected abstract IEnumerable<ChartItem> GetPersonModeChartItemsFromAssignments(
            Person person,
            IEnumerable<Assignment> assignments,
            DateTime startDate,
            DateTime endDate
        );

        /// <summary>
        /// Generate chart items for a given person in project mode
        /// </summary>
        /// <param name="seriesName"></param>
        /// <param name="groupedAssignments"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="person"></param>
        /// <param name="isTotalRow"></param>
        /// <returns></returns>
        protected abstract IEnumerable<ChartItem> GetProjectModeChartItemsFromAssignments(
            string seriesName,
            KeyValuePair<object, IEnumerable<Assignment>> groupedAssignments,
            DateTime startDate,
            DateTime endDate,
            Person person,
            bool isTotalRow = false
        );

        /// <summary>
        /// Method to configure the sources for the capacity chart objects
        /// </summary>
        protected abstract void ConfigureChartSource();

        /// <summary>
        /// Method to get a unique session storage tag for the page
        /// </summary>
        /// <returns></returns>
        protected abstract string GetSessionStorageTag();

        /// <summary>
        /// Method to reload the dropdown sources on the page
        /// </summary>
        protected virtual void ReloadDropDownSources()
        {
            Debug.WriteLine("** Reloading dropdown sources...");

            // Get people and filter if PM selected
            people = cachedPeople.ToList();

            // Filter out leavers if necessary
            if (!IncludeLeavers)
            {
                people = people
                    .Where(x => x.EndDate == null || x.EndDate >= DateTime.Today)
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            // Apply autocomplete box filters
            LoadFilteredPeople(new LoadDataArgs());

            // Remove any people not in the dropdown source from the selected people list
            if (ChosenPeople != null)
            {
                var temp = new List<string>();
                foreach (var p in ChosenPeople)
                {
                    if (filteredPeople.Any(x => x.Name == p))
                    {
                        temp.Add(p);
                    }
                }
                ChosenPeople = temp;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = true;

            // Get all projects not finished or cancelled
            cachedProjects = ProjectService.GetAll(Context).Where(x => !x.ProjectStatus.IsCancelled());

            // Cache all the people
            cachedPeople = PersonService.GetAll(Context).OrderBy(x => x.Name);

            // Refresh the dropdown
            ReloadDropDownSources();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            ChosenPeople = await SessionStorage.GetItemAsync<IEnumerable<string>>($"{GetSessionStorageTag()}-chosen-people");

            // If there is a query parameter then use it
            if (FilterPersonId != null)
            {
                var matchingPerson = cachedPeople.FirstOrDefault(x => x.PersonId == FilterPersonId);
                if (matchingPerson != null)
                {
                    ChosenPeople = new List<string>
                    {
                        matchingPerson.Name
                    };
                }
            }

            // Check that the boolean flags are not null (i.e. that they exist in session storage) before overwriting defaults
            var temp = await SessionStorage.GetItemAsync<bool?>($"{GetSessionStorageTag()}-include-leavers");
            if (temp != null) IncludeLeavers = temp ?? false;
            temp = await SessionStorage.GetItemAsync<bool?>($"{GetSessionStorageTag()}-include-unfunded");
            if (temp != null) IncludeUnFunded = temp ?? false;
            temp = await SessionStorage.GetItemAsync<bool?>($"{GetSessionStorageTag()}-include-finished");
            if (temp != null) IncludeFinished = temp ?? false;
        }

        /// <summary>
        /// Determine whether any people are chosen
        /// </summary>
        /// <returns></returns>
        protected bool PeopleChosen()
        {
            return ChosenPeople != null && ChosenPeople.Count() > 0;
        }

        /// <summary>
        /// Save the chosen people to session storage
        /// </summary>
        protected void SavePeopleState()
        {
            SessionStorage.SetItemAsync($"{GetSessionStorageTag()}-chosen-people", chosenPeople);
        }

        /// <summary>
        /// Fire and forget when selection of the multi-select people down changes
        /// </summary>
        /// <param name="selectedOptions"></param>
        protected void PeopleSelectionChanged(object selectedOptions)
        {
            var items = selectedOptions as IEnumerable<string>;
            Debug.WriteLine("** Selected People:");
            if (items != null)
            {
                foreach (var i in items)
                {
                    Debug.WriteLine($"** {i}");
                }
            }

            // Save the new state
            SavePeopleState();

            // Regenerate the chart data
            ConfigureChartSource();

            LogInformation($"Selected people: {(items == null ? "" : string.Join("|", items))}");
        }

        /// <summary>
        /// Use the master list of people to filter the data source for the dropdown based on user typing
        /// </summary>
        /// <param name="args"></param>
        protected void LoadFilteredPeople(LoadDataArgs args)
        {
            var temp = people.AsQueryable();
            if (!string.IsNullOrEmpty(args.Filter))
            {
                temp = temp.Where(p => p.Name.ToLower().Contains(args.Filter.ToLower()));
            }
            filteredPeople = temp.ToList();
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Updates all the chart models that do not have the matching options object with the min and max provided
        /// </summary>
        /// <param name="options"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        protected void UpdateZoomAcrossCharts(ApexChartOptions<ChartItem> options, object min, object max)
        {
            // Go through all the chart options objects and for all not associated with the chart making this call
            // and whose values of the X limits differ from those give can then be updated.
            foreach (var opt in chartModels.Select(x => x.ChartOptions))
            {
                if (opt != options)
                {
                    Debug.WriteLine($"** Updating zoom for {opt.Chart.Id}: {min} to {max}");
                    JSRuntime.InvokeVoidAsync("apexChartsUpdateAxis", opt.Chart.Id, min, max);
                }
            }
        }

        /// <summary>
        /// Method to automatically zoom the charts to the number of months in the future
        /// </summary>
        /// <param name="numberOfMonths"></param>
        protected void SetZoomToMonthsAhead(int numberOfMonths)
        {
            var zoomTo = DateTime.Today.AddMonths(numberOfMonths).ToUnixTimeMilliseconds();
            var opt = chartModels.FirstOrDefault()?.ChartOptions;
            if (opt != null)
            {
                Debug.WriteLine($"** Updating zoom for {opt.Chart.Id}: {opt.Xaxis.Min} to {zoomTo}");
                JSRuntime.InvokeVoidAsync("apexChartsUpdateAxis", opt.Chart.Id, opt.Xaxis.Min, zoomTo);
                UpdateZoomAcrossCharts(opt, opt.Xaxis.Min, zoomTo);
            }
        }

        /// <summary>
        /// Callback when an item is zoomed
        /// </summary>
        /// <param name="zoomedData"></param>
        protected void OnChartZoomed(ZoomedData<ChartItem> zoomedData)
        {
            if (zoomedData != null)
            {
                Debug.WriteLine($"** {zoomedData.Chart.ChartId} Zoomed {zoomedData.XAxis.Min} to {zoomedData.XAxis.Max}");
                UpdateZoomAcrossCharts(zoomedData.Chart.Options, zoomedData.XAxis.Min, zoomedData.XAxis.Max);
            }
        }

        /// <summary>
        /// Takes the cached projects on the page and filters them based on the state of the switches and dropdowns
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Project> GetValidProjects()
        {
            var validProjects = cachedProjects;

            // Filter projects based on finished
            if (!IncludeFinished)
            {
                Debug.WriteLine("** Removing finished projects...");
                validProjects = validProjects.Where(p => p.ProjectStatus != ProjectStatus.Finished);
            }

            // Filter projects based on unfunded
            if (!IncludeUnFunded)
            {
                Debug.WriteLine("** Removing unfunded projects...");
                validProjects = validProjects.Where(p => !p.ProjectStatus.IsUnfunded());
            }

            return validProjects;
        }

        /// <summary>
        /// Creates a standard chart object to be pass to all chart instances -- they cannot share the same object
        /// </summary>
        /// <returns></returns>
        protected virtual ApexChartOptions<ChartItem> BuildNewChartOptionsObject()
        {
            return new ApexChartOptions<ChartItem>
            {
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = true,
                        RangeBarOverlap = true,
                        RangeBarGroupRows = true
                    }
                },
                Legend = new Legend
                {
                    Show = false
                },
                Xaxis = new XAxis { },
                Fill = new Fill
                {
                    Opacity = 1,
                    Type = new FillTypeSelections(new FillType[] { FillType.Solid, FillType.Pattern }),
                    Pattern = new FillPattern
                    {
                        Style = new FillPatternStyleSelections(new FillPatternStyle[] { FillPatternStyle.SlantedLines }),
                    }
                },
                Chart = new Chart
                {
                    Zoom = new Zoom
                    {
                        AllowMouseWheelZoom = false
                    }
                },
                Annotations = new Annotations
                {
                    Xaxis = new List<AnnotationsXAxis>
                    {
                        new AnnotationsXAxis()
                        {
                            X = DateTime.Today.ToUnixTimeMilliseconds(),
                            BorderWidth = 2,
                            StrokeDashArray = 5,
                            BorderColor = "#888",
                            Label = new Label
                            {
                                Text = "Today",
                                Position = LabelPosition.Left
                            }
                        }
                    }
                }
            };
        }
    }
}
