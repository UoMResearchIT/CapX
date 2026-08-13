// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Components;
using PPMTool.Pages;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Shared
{
    /// <summary>
    /// A custom data grid component that extends the RadzenDataGrid and provides additional functionality for paging.
    /// </summary>
    /// <typeparam name="TItem">Type of item in the datagrid</typeparam>
    public class DataGridWithPaging<TItem> : RadzenDataGrid<TItem>
    {
        /// <summary>
        /// A static array of available page size options for the data grid.
        /// This is used to provide a set of predefined page sizes that users can select from when interacting with the data grid.
        /// </summary>
        private static int[] AvaialblePageSizeOptions { get; } = new[] { 5, 10, 25, 50, 100 };

        /// <summary>
        /// A reference to the page that contains the data grid, which is used to handle paging events.
        /// </summary>
        [Parameter]
        public BasePage PagingPage { get; set; }

        /// <summary>
        /// Overrides the SetParametersAsync method to set default values for certain parameters if they are not provided.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            // Call the base method to set the parameters
            await base.SetParametersAsync(parameters);

            // Check if the parameters have been provided through the properties
            var hasAllowPaging = parameters.TryGetValue<bool>(nameof(AllowPaging), out _);
            var hasShowPagingSummary = parameters.TryGetValue<bool>(nameof(ShowPagingSummary), out _);
            var hasPageSizeOptions = parameters.TryGetValue<IEnumerable<int>>(nameof(AvaialblePageSizeOptions), out _);
            var hasPagerHorizontalAlign = parameters.TryGetValue<HorizontalAlign>(nameof(PagerHorizontalAlign), out _);
            var hasPagerPosition = parameters.TryGetValue<PagerPosition>(nameof(PagerPosition), out _);
            var hasPageSize = parameters.TryGetValue<int>(nameof(PageSize), out _);
            var hasPageSizeChanged = parameters.TryGetValue<EventCallback<int>>(nameof(PageSizeChanged), out _);

            // Set default values for the parameters if they are not provided
            if (!hasAllowPaging)
            {
                AllowPaging = true;
            }

            if (!hasShowPagingSummary)
            {
                ShowPagingSummary = true;
            }

            if (!hasPageSizeOptions)
            {
                PageSizeOptions = AvaialblePageSizeOptions;
            }

            if (!hasPagerHorizontalAlign)
            {
                PagerHorizontalAlign = HorizontalAlign.Left;
            }

            if (!hasPagerPosition)
            {
                PagerPosition = PagerPosition.TopAndBottom;
            }

            if (!hasPageSize)
            {
                PageSize = PagingPage?.PageCount ?? 15;
            }

            if (!hasPageSizeChanged)
            {
                PageSizeChanged = EventCallback.Factory.Create<int>(this, HandlePageSizeChangedAsync);
            }
        }

        /// <summary>
        /// Handles the page size change event and updates the page size accordingly.
        /// If a PagingPage is provided, it will call the OnPageSizeChangedAsync method of that page.
        /// Otherwise, it will simply trigger a state change to update the UI.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private async Task HandlePageSizeChangedAsync(int value)
        {
            PageSize = value;

            if (PagingPage != null)
            {
                await PagingPage.OnPageSizeChangedAsync(value);
            }
            else
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
