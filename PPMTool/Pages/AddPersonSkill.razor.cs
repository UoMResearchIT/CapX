using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class AddPersonSkill : BasePage
    {
        [Inject]
        public PersonService PersonService { get; set; }

        [Inject]
        private SkillTagService TagService { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel;
        private IEnumerable<SkillTag> availableTags;
        private IList<SkillTag> chosenTags = new List<SkillTag>();
        private string autoCompleteText;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Map entities to checkbox list items
            availableTags = TagService.GetAll(Context).OrderBy(x => x.Name).ToList();

            if (PersonId > 0)
            {
                personModel = PersonService.GetById(Context, PersonId);

                // Update the chosen tags
                if (personModel != null)
                {
                    // Update chosen tags
                    chosenTags = personModel.SkillTags.OrderBy(x => x.Name).ToList();

                    // Edit should only be authorised for the line manager or superusers
                    EditAuthorised = IsSuperuserOrLineManagerOfThisPerson(personModel);
                }
            }

            LogInformation($"Viewing skills for {personModel?.Name}");
        }

        /// <summary>
        /// Leave the page without saving the state of the skills
        /// </summary>
        private void DiscardChanges()
        {
            LogInformation($"Discarding skills changes!");

            // Just navigate away as nothing will have been written to the database
            Navigation.NavigateTo($"people/addperson/{PersonId}");
        }

        /// <summary>
        /// Simply clear the search box
        /// </summary>
        private void ClearSearch()
        {
            autoCompleteText = string.Empty;
        }

        /// <summary>
        /// When the search box is changed
        /// </summary>
        /// <param name="args"></param>
        void OnChange(dynamic args)
        {
            var match = availableTags.FirstOrDefault(x => x.Name.Trim() == autoCompleteText.Trim());
            if (match != null && !chosenTags.Contains(match))
            {
                chosenTags.Add(match);
                ClearSearch();
                chosenTags = chosenTags.OrderBy(x => x.Name).ToList();
                ShowNotification(new CapXNotificationMessage
                {
                    Severity = Radzen.NotificationSeverity.Success,
                    Summary = "Skill Added",
                    Detail = $"Added \"{match.Name}\" to the skills list -- remember to save your changes to update the person record."
                });
            }
        }

        /// <summary>
        /// When a skills tag is removed from the data list
        /// </summary>
        /// <param name="tag"></param>
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

        private void HandleValidSubmit()
        {
            if (personModel != null)
            {
                // Reset error
                ErrorMessage = null;

                // Add tags to person model
                personModel.SkillTags = chosenTags.ToList();

                // Write to the database
                LogInformation($"Saving skills for {personModel.Name}.");
                PersonService.Update(Context, personModel);

                // Navigate back
                Navigation.NavigateTo($"people/addperson/{PersonId}");
            }
        }
    }
}
