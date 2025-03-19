using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Pages.Components;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    [Authorize]
    public partial class UserProfile : BasePage
    {
        [Inject]
        private ApiKeyService ApiKeyService { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        private IEnumerable<ApiKey> ApiKeys;
        private RadzenDataList<ApiKey> dataList;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LogInformation("Viewing profile");
        }

        /// <summary>
        /// Generates a new API key and saves it to the DB
        /// </summary>
        private void GenerateApiKey()
        {
            // TODO: Pop up a dialog to ask them to add a description
            DialogService.Open<ApiKeyConfigComponent>(
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
                ApiKeys = ApiKeyService.GetForUser(Context, ActiveUser.UserId);
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


        /// <summary>
        /// Copy the API key to the clipboard
        /// </summary>
        /// <param name="key"></param>
        private void OnCopy(ApiKey key)
        {

        }
    }
}
