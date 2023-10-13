using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class AddAvailabilityChange : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel;
        private PPMToolContext context;
        private AvailabilityChange changeModel = new AvailabilityChange() { ChangeDate = DateTime.Now.Date };
        private List<AvailabilityChange> changeList = new List<AvailabilityChange>();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            context = new PPMToolContext();

            if (PersonId > -1)
            {
                personModel = PersonService.GetById(context, PersonId);
                changeList = personModel.AvailabilityChanges.ToList();
            }
        }

        private async void DeleteChange(int changeId)
        {
            var changeToBeDeleted = changeList.First(x => x.AvailabilityChangeId == changeId);
            bool confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"You are about to delete availability change {changeToBeDeleted.ChangeDate.ToShortDateString()} at {changeToBeDeleted.AvailabilityFTE} FTE");
            if (confirmed)
            {
                // Modify the person model, save changes and reload the change list
                personModel.AvailabilityChanges.Remove(changeToBeDeleted);
                PersonService.Update(context, personModel);
                changeList = personModel.AvailabilityChanges.ToList();
                StateHasChanged();
            }
        }

        private void HandleValidSubmit()
        {
            if (personModel != null)
            {
                // Check it doesn't duplicate the date, otherwise reject update
                if (personModel.AvailabilityChanges.Any(x => x.ChangeDate.Date == changeModel.ChangeDate.Date)) { return; }

                // Update the person model, save to database, refresh the list and reset the model
                personModel.AvailabilityChanges.Add(changeModel);
                PersonService.Update(context, personModel);
                changeList = personModel.AvailabilityChanges.ToList();
                changeModel = new AvailabilityChange() { ChangeDate = DateTime.Now.Date };
                StateHasChanged();
            }
        }
    }
}
