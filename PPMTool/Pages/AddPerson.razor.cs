using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddPerson : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private TagService TagService { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel = new();
        private IEnumerable<SkillTag> availableTags;
        private IList<SkillTag> chosenTags = new List<SkillTag>();
        private string autoCompleteText;
        private EditContext editContext;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Map entities to checkbox list items
            availableTags = TagService.GetAll(context).OrderBy(x => x.Name).ToList();

            // Load the person model if necessary
            if (PersonId > -1)
            {
                personModel = PersonService.GetAll(context).FirstOrDefault(x => x.PersonId == PersonId);

                // Update the chosen tags
                if (personModel != null)
                {
                    chosenTags = personModel.SkillTags.OrderBy(x => x.Name).ToList();
                }
            }

            // Instantiate the edit context so we have a reference to it
            editContext = new EditContext(personModel);

            LogInformation(personModel?.PersonId > 0 ? $"Editing person {personModel?.Name}" : $"Adding new person");
        }

        void OnChange(dynamic args)
        {
            var match = availableTags.FirstOrDefault(x => x.Name == autoCompleteText);
            if (match != null && !chosenTags.Contains(match))
            {
                chosenTags.Add(match);
                chosenTags = chosenTags.OrderBy(x => x.Name).ToList();
            }
        }

        void OnDelete(SkillTag tag)
        {
            var match = chosenTags.FirstOrDefault(x => x.Name == tag.Name);
            if (match != null)
            {
                LogInformation($"Removing skill tag {tag.Name}");
                chosenTags.Remove(match);
                chosenTags = chosenTags.OrderBy(x => x.Name).ToList();
            }
        }

        private void EditAvailability()
        {
            // Check the existing model is valid first
            if (editContext.Validate())
            {
                HandleValidSubmit();

                LogInformation("Editing availability changes...");
                Navigation.NavigateTo($"/addavailabilitychange/{personModel.PersonId}");
            }
        }

        private void EditAbsence()
        {
            // Check the existing model is valid first
            if (editContext.Validate())
            {
                HandleValidSubmit();

                LogInformation("Editing absences...");
                Navigation.NavigateTo($"/addabsence/{personModel.PersonId}");
            }
        }

        private void HandleValidSubmit()
        {
            // Add tags to person model
            personModel.SkillTags = chosenTags.ToList();

            if (PersonId > -1)
            {
                LogInformation($"Saving person {personModel?.Name}...");

                // Edit
                PersonService.Update(context, personModel);
            }
            else
            {
                // Add new
                if (PersonService.Add(context, personModel) < 0)
                {
                    // TODO: Duplicate found -- do something
                    LogWarning($"Duplicate person found with name {personModel?.Name}");
                }
            }

            Navigation.NavigateTo("people");
        }
    }
}
