using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddAbsence : AddPersonProperty<Absence>
    {
        [Inject]
        public EmailService EmailService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (PersonId > 0)
            {
                personModel = PersonService.GetById(Context, PersonId);
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
            entityToInsert.StartDate = DateTime.Today;
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
                        x.AbsenceId != y.AbsenceId &&
                        ((x.EndDate == null && x.StartDate <= y.EndDate && x.StartDate >= y.StartDate) ||
                        (x.EndDate != null && y.EndDate != null && x.StartDate <= y.EndDate && x.EndDate >= y.StartDate));

                }));
                if (absence != null)
                {
                    ErrorMessage = new StatusMessage($"Problem with absence beginning on {absence.StartDate.ToShortDateString()}. Absence periods cannot overlap!", StatusMessage.MessageType.Error);
                    return;
                }

                // Check absence periods have to start before they end
                absence = dataGridEntities.FirstOrDefault(x => x.EndDate != null ? x.StartDate > x.EndDate : false);
                if (absence != null)
                {
                    ErrorMessage = new StatusMessage($"Problem with absence beginning on {absence.StartDate.ToShortDateString()}. Absence period ends before it starts!", StatusMessage.MessageType.Error);
                    return;
                }

                // Check only one open-ended absence
                if (dataGridEntities.Where(x => x.EndDate == null).Count() > 1)
                {
                    ErrorMessage = new StatusMessage("Only one open-ended absence permitted!", StatusMessage.MessageType.Error);
                    return;
                }

                // Reset error
                ErrorMessage = null;

                // Get tracking information for the absences (added or deleted won't be tracked yet)
                var newAbsences = dataGridEntities.Where(x => !personModel.Absences.Contains(x)).ToList();
                var deletedAbsences = personModel.Absences.Where(x => !dataGridEntities.Contains(x)).ToList();
                var updatedAbsences = PersonService.GetDiffList<Absence>(Context).Where(x => x.State == EntityState.Modified).GroupBy(x => x.Entity);
                var delAbsencesDictionary = deletedAbsences.ToDictionary(x => x.Person.PersonId);

                // If there are no changes then just navigate back
                if (newAbsences.Count > 0 || updatedAbsences.Count() > 0 || deletedAbsences.Count > 0)
                {

                    // Send emails based on diff information
                    EmailService.SendAbsenceEmailNotifications(newAbsences, updatedAbsences, delAbsencesDictionary);

                    // Reset assign the absences from the data grid to the model
                    personModel.Absences.Clear();
                    foreach (var ab in dataGridEntities)
                    {
                        personModel.Absences.Add(ab);
                    }

                    // Write to the database
                    LogInformation($"Saving absences for {personModel.Name}.");
                    PersonService.Update(Context, personModel);
                }
                else
                {
                    LogInformation($"Save clicked but no changes identified in absences for {personModel.Name}.");
                }

                // Navigate back
                Navigation.NavigateTo($"addperson/{PersonId}");
            }
        }
    }
}
