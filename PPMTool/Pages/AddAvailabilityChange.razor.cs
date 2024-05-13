using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PPMTool.Data.Entities;
using PPMTool.Services;
using static PPMTool.Data.Entities.Project;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddAvailabilityChange : AddPersonProperty<AvailabilityChange>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (PersonId > -1)
            {
                personModel = PersonService.GetById(context, PersonId);
                dataGridEntities = personModel.AvailabilityChanges.ToList();
            }
            else
            {
                dataGridEntities = new List<AvailabilityChange>();
            }

            LogInformation($"Viewing availability changes for {personModel?.Name}");
        }

        protected override async Task InsertRow()
        {
            await base.InsertRow();
            entityToInsert.ChangeDate = DateTime.Today;
            await dataGrid.InsertRow(entityToInsert);
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding availability changes!");

            // Just navigate away as nothing will have been written to the database
            Navigation.NavigateTo($"addperson/{PersonId}");
        }

        private void HandleValidSubmit()
        {
            if (personModel != null)
            {
                // Check it doesn't duplicate the date, otherwise reject update
                if (dataGridEntities.DistinctBy(x => x.ChangeDate).Count() != dataGridEntities.Count())
                {
                    LogWarning($"Availability change duplicates a change date!");
                    errorMessage = new StatusMessage("You cannot have multiple changes in availability on the same day!", StatusMessage.MessageType.Error);
                    return;
                }
                else
                {
                    errorMessage = null;
                }

                // Update the person model, save to database, refresh the list and reset the model
                personModel.AvailabilityChanges.Clear();
                foreach (var avail in dataGridEntities)
                {
                    personModel.AvailabilityChanges.Add(avail);
                }

                LogInformation($"Saving availability changes for {personModel.Name}.");
                PersonService.Update(context, personModel);
                Navigation.NavigateTo($"addperson/{PersonId}");
            }
        }
    }
}
