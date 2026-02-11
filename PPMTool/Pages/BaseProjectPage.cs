// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public abstract class BaseProjectPage : BasePage
    {
        [Inject]
        protected ProjectService ProjectService { get; set; }

        [Inject]
        protected ISessionStorageService SessionStorage { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        protected void NavigateToProjectDetails(int id, bool newWindow = false, bool filterDueNotes = false)
        {
            string url = $"projects/projectdetails/{id}";

            if (filterDueNotes)
            {
                url += "?filterDueNotes=true";
            }

            if (newWindow)
            {
                JSRuntime.InvokeVoidAsync("open", url, "_blank");
            }
            else
            {
                Navigation.NavigateTo(url);
            }
        }
    }
}
