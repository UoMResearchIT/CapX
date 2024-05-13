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
    public partial class AddAbsence : AddPersonProperty<Absence>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (PersonId > -1)
            {
                personModel = PersonService.GetById(context, PersonId);
                dataGridEntities = personModel.Absences.ToList();
            }
            else
            {
                dataGridEntities = new List<Absence>();
            }

            LogInformation($"Viewing absences for {personModel?.Name}");
        }

        protected override async Task InsertRow()
        {
            await base.InsertRow();
            entityToInsert.StartDate = DateTime.Now.Date;
            await dataGrid.InsertRow(entityToInsert);
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding absence changes!");

            // Just navigate away as nothing will have been written to the database
            Navigation.NavigateTo($"addperson/{PersonId}");
        }

        private void HandleValidSubmit()
        {
            if (personModel != null)
            {
                // Check no period of absence overlaps with another
                Absence absence = dataGridEntities.FirstOrDefault(x => dataGridEntities.Any(y =>
                {
                    return
                        x != y &&
                        (x.EndDate == null && x.StartDate <= y.EndDate && x.StartDate >= y.StartDate) ||
                        (x.EndDate != null && y.EndDate != null && x.StartDate <= y.EndDate && x.EndDate >= y.StartDate);
                }));
                if (absence != null)
                {
                    errorMessage = new StatusMessage($"Problem with absence beginning on {absence.StartDate.ToShortDateString()}. Absence periods cannot overlap!", StatusMessage.MessageType.Error);
                    return;
                }

                // Check absence periods have to start before they end
                absence = dataGridEntities.FirstOrDefault(x => x.EndDate != null ? x.StartDate > x.EndDate : false);
                if (absence != null)
                {
                    errorMessage = new StatusMessage($"Problem with absence beginning on {absence.StartDate.ToShortDateString()}. Absence period ends before it starts!", StatusMessage.MessageType.Error);
                    return;
                }

                // Check only one open-ended absence
                if (dataGridEntities.Where(x => x.EndDate == null).Count() > 1)
                {
                    errorMessage = new StatusMessage("Only one open-ended absence permitted!", StatusMessage.MessageType.Error);
                    return;
                }

                // Reset error
                errorMessage = null;

                // Update the person model, save to database, refresh the list and reset the model
                personModel.Absences.Clear();
                foreach (var ab in dataGridEntities)
                {
                    personModel.Absences.Add(ab);
                }

                LogInformation($"Saving absences for {personModel.Name}.");
                PersonService.Update(context, personModel);
                Navigation.NavigateTo($"addperson/{PersonId}");
            }
        }
    }
}
