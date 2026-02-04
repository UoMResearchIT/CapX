using System.Diagnostics;
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
        private SkillTagService SkillTagService { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel;
        private IEnumerable<SkillTag> availableTags;
        private IList<OwnedSkill> ownedTags = new List<OwnedSkill>();
        private string autoCompleteText;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            Debug.WriteLine($"** Loading owned skills for person {PersonId}");

            // Map entities to checkbox list items
            availableTags = SkillTagService.GetAll(Context).OrderBy(x => x.Name).ToList();

            // Assign active user if the parameter is zero
            if (PersonId == 0)
            {
                PersonId = ActiveUser?.Person?.PersonId ?? 0;
                Debug.WriteLine($"** Setting person ID to {PersonId}");
            }

            // If a valid person then load if not the same person to avoid an infinite loop
            if (PersonId > 0 && personModel?.PersonId != PersonId)
            {
                personModel = PersonService.GetById(Context, PersonId);

                // Update the chosen tags and permissions
                if (personModel != null)
                {
                    // Update chosen tags
                    ownedTags = personModel.OwnedSkills.OrderBy(x => x.SkillTag.Name).ToList();

                    // Edit should only be authorised for the line manager or superusers
                    EditAuthorised = IsSuperuserOrLineManagerOrPerson(personModel);

                    // Update action bar button state
                    SetDefaultActionBar(HandleValidSubmit, DiscardChanges);
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
            if (match != null && !ownedTags.Any(x => x.SkillTag.SkillTagId == match.SkillTagId))
            {
                ownedTags.Add(new OwnedSkill
                {
                    SkillTag = match,
                    Owner = personModel
                });
                ClearSearch();
                ownedTags = ownedTags.OrderBy(x => x.SkillTag.Name).ToList();
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
        /// <param name="ownedTag"></param>
        void OnDelete(OwnedSkill ownedTag)
        {
            var match = ownedTags.FirstOrDefault(x => x.SkillTag.SkillTagId == ownedTag.SkillTag.SkillTagId);
            if (match != null)
            {
                LogInformation($"Removing skill tag {ownedTag.SkillTag.Name}");
                ownedTags.Remove(match);
                ownedTags = ownedTags.OrderBy(x => x.SkillTag.Name).ToList();
            }
        }

        private void HandleValidSubmit()
        {
            if (personModel != null)
            {
                // Reset error
                ClearErrorMessage();

                // Add tags to person model
                personModel.OwnedSkills = ownedTags.ToList();

                // Write to the database
                LogInformation($"Saving skills for {personModel.Name}.");
                PersonService.Update(Context, personModel);

                // Update the skills tag rareness based on these changes
                foreach (var ownedSkill in personModel.OwnedSkills)
                {
                    var tag = ownedSkill.SkillTag;
                    SkillTagService.UpdateSkillTagRareness(Context, tag);
                }

                // Navigate back
                Navigation.NavigateTo($"people/addperson/{PersonId}");
            }
        }

        /// <summary>
        /// Toggles the favourite setting in the model
        /// </summary>
        /// <param name="skill"></param>
        private void ToggleFavourite(OwnedSkill skill)
        {
            skill.FavouriteSkill = !skill.FavouriteSkill;
        }
    }
}
