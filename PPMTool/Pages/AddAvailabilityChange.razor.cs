using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddAvailabilityChange : DataGridPage<AvailabilityChange>
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel;
        private bool isValid = true;

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
        }

        protected override void CancelEdit(AvailabilityChange entity)
        {
            Reset();
            PersonService.RestoreModel(context, ref entity);
            dataGrid.CancelEditRow(entity);
        }

        protected override async Task InsertRow()
        {
            entityToInsert = Activator.CreateInstance(typeof(AvailabilityChange)) as AvailabilityChange;
            entityToInsert.Person = personModel;
            entityToInsert.ChangeDate = DateTime.Now.Date;
            await dataGrid.InsertRow(entityToInsert);
        }

        protected override void OnCreateRow(AvailabilityChange entity)
        {
            entity.Person = personModel;
            dataGridEntities.Add(entity);
            entityToInsert = null;
        }

        protected override void OnUpdateRow(AvailabilityChange entity)
        {
            Reset();
        }

        private void DiscardChanges()
        {
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
                    isValid = false;
                    return;
                }
                else
                {
                    isValid = true;
                }

                // Update the person model, save to database, refresh the list and reset the model
                personModel.AvailabilityChanges.Clear();
                foreach (var avail in dataGridEntities)
                {
                    personModel.AvailabilityChanges.Add(avail);
                }
                PersonService.Update(context, personModel);
                Navigation.NavigateTo($"addperson/{PersonId}");
            }
        }
    }
}
