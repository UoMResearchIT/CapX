// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Pages.Components;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class UserProfile : BasePage
    {
        [Inject]
        private ApiKeyService ApiKeyService { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        private IEnumerable<ApiKey> apiKeys;
        private RadzenDataList<ApiKey> dataList;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = true;
            EnqueueLoadData(GetLoadTask);
            LogInformation("Viewing profile");
        }

        /// <summary>
        /// Gets the load data task
        /// </summary>
        /// <returns></returns>
        private Task GetLoadTask()
        {
            return Task.Run(() =>
            {
                apiKeys = ApiKeyService.GetForUser(Context, ActiveUser.UserId);

            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    try
                    {
                        StateHasChanged();
                    }
                    catch
                    {
                        // Sometimes this throws a wobbler.
                        // Not sure why so putting this here to just stop a hard crash.
                        // #Classy
                    }
                });
            });
        }

        /// <summary>
        /// Opens the dialog to generate a new API key
        /// </summary>
        private void GenerateApiKey()
        {
            // Pop up a dialog to ask them to add a description
            DialogService.OpenAsync<ApiKeyConfigComponent>(
                "Configure API Key",
                new Dictionary<string, object>
                    {
                        { nameof(ApiKeyConfigComponent.Logger), Logger },
                        { nameof(ApiKeyConfigComponent.Context), Context },
                        { nameof(ApiKeyConfigComponent.ActiveUser), ActiveUser },
                        { nameof(ApiKeyConfigComponent.FormClosed), () => FormClosedHandler() }
                    },
                    new DialogOptions
                    {
                        ShowClose = false
                    });

            LogInformation("Generated API Key");
        }

        /// <summary>
        /// Delete the API key
        /// </summary>
        /// <param name="key"></param>
        private async void OnDelete(ApiKey key)
        {
            var confirm = await DialogService.Confirm(
                "Are you sure you want to delete this API key?",
                "Delete API Key",
                new ConfirmOptions()
                {
                    OkButtonText = "Yes",
                    CancelButtonText = "No"
                }) ?? false;
            if (confirm)
            {
                ApiKeyService.Delete(Context, key);
                apiKeys = ApiKeyService.GetForUser(Context, ActiveUser.UserId);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Callback which runs when the form closes
        /// </summary>
        private void FormClosedHandler()
        {
            dataList?.Reload();
            StateHasChanged();
        }
    }
}
