using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class AddPersonSkill : DataGridPage<SkillTag>
    {
        [Inject]
        public PersonService PersonService { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (PersonId > 0)
            {
                personModel = PersonService.GetById(Context, PersonId);
                dataGridEntities = personModel.SkillTags.OrderBy(x => x.Name).ToList();
            }
            else
            {
                dataGridEntities = new List<SkillTag>();
            }

            LogInformation($"Viewing skills for {personModel?.Name}");
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding skills changes!");

            // Just navigate away as nothing will have been written to the database
            Navigation.NavigateTo($"people/addperson/{PersonId}");
        }

        private void HandleValidSubmit()
        {
            if (personModel != null)
            {
                // TODO: Validation?


                // Reset error
                ErrorMessage = null;

                // Assign the absences from the data grid to the model
                personModel.SkillTags.Clear();
                foreach (var tag in dataGridEntities)
                {
                    personModel.SkillTags.Add(tag);
                }

                // Write to the database
                LogInformation($"Saving skills for {personModel.Name}.");
                PersonService.Update(Context, personModel);

                // Navigate back
                Navigation.NavigateTo($"people/addperson/{PersonId}");
            }
        }
    }
}
