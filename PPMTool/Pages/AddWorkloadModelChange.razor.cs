using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddWorkloadModelChange : AddPersonProperty<WorkloadModelChange>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (PersonId > 0)
            {
                personModel = PersonService.GetById(Context, PersonId);
                dataGridEntities = personModel.WorkloadModelChanges.ToList();
            }
            else
            {
                dataGridEntities = new List<WorkloadModelChange>();
            }

            LogInformation($"Viewing workload model changes for {personModel?.Name}");
        }

        protected override async Task InsertRow()
        {
            await base.InsertRow();
            entityToInsert.ChangeDate = DateTime.Today;
            await dataGrid.InsertRow(entityToInsert);
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding workload model changes!");

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
                    ErrorMessage = new StatusMessage("You cannot have multiple changes in availability on the same day!", StatusMessage.MessageType.Error);
                    return;
                }
                else
                {
                    ErrorMessage = null;
                }

                // Update the person model, save to database, refresh the list and reset the model
                personModel.WorkloadModelChanges.Clear();
                foreach (var avail in dataGridEntities)
                {
                    personModel.WorkloadModelChanges.Add(avail);
                }

                LogInformation($"Saving workload model changes for {personModel.Name}.");
                PersonService.Update(Context, personModel);
                Navigation.NavigateTo($"addperson/{PersonId}");
            }
        }
    }
}
