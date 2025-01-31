using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.CodeAnalysis;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class AddPerson : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private TagService TagService { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel = new();
        private IEnumerable<SkillTag> availableTags;
        private IList<SkillTag> chosenTags = new List<SkillTag>();
        private IList<Person> managers = new List<Person>();
        private string autoCompleteText;
        private EditContext editContext;
        private ValidationMessageStore messageStore;
        private bool isSuperUser;
        private bool canView;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // Default view permission based on edit authorisation
            canView = EditAuthorised;

            // Load the person model if necessary
            if (PersonId > 0)
            {
                personModel = PersonService.GetAll(Context).FirstOrDefault(x => x.PersonId == PersonId);

                // Update the chosen tags
                if (personModel != null)
                {
                    // Update chosen tags
                    chosenTags = personModel.SkillTags.OrderBy(x => x.Name).ToList();

                    // Edit should only be authorised for the line manager or superusers
                    EditAuthorised = IsSuperuserOrLineManagerOfThisPerson(personModel);

                    // Developers can view their own page; managers can view all people pages
                    canView = EditAuthorised || ActiveUser?.PersonId == personModel.PersonId || ActiveUserRoleType == Enums.RoleType.Manager;
                }
            }

            // Instantiate the edit context so we have a reference to it
            editContext = new EditContext(personModel);
            messageStore = new ValidationMessageStore(editContext);

            LogInformation(personModel?.PersonId > 0 ? $"Editing person {personModel?.Name}" : $"Adding new person");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Find out if superuser for delete button
            isSuperUser = RolesService.GetRoleTypeForUsername(Context, ActiveUserName) == Enums.RoleType.Superuser;

            // Map entities to checkbox list items
            availableTags = TagService.GetAll(Context).OrderBy(x => x.Name).ToList();

            // Map the list of managers for drop down
            managers = RolesService.GetAll(Context)
                .Where(x => (x.RoleType == Enums.RoleType.Manager || x.RoleType == Enums.RoleType.Superuser) && x.Person.PersonId != personModel.PersonId)
                .Select(x => x.Person)
                .DistinctBy(x => x.PersonId)
                .OrderBy(x => x.Name)
                .ToList();

            LogInformation("Initialising add/edit person page");
        }

        void OnChange(dynamic args)
        {
            var match = availableTags.FirstOrDefault(x => x.Name.Trim() == autoCompleteText.Trim());
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
            messageStore.Clear();
            if (editContext.Validate())
            {
                HandleSubmit();

                // Check for any further messages added by DB interactions
                if (!editContext.GetValidationMessages().Any())
                {
                    LogInformation("Editing workload model changes...");
                    Navigation.NavigateTo($"people/addavailabilitychange/{personModel.PersonId}");
                }
            }
        }

        private void EditAbsence()
        {
            // Check the existing model is valid first
            messageStore.Clear();
            if (editContext.Validate())
            {
                HandleSubmit();

                // Check for any further messages added by DB interactions
                if (!editContext.GetValidationMessages().Any())
                {
                    LogInformation("Editing absences...");
                    Navigation.NavigateTo($"people/addabsence/{personModel.PersonId}");
                }
            }
        }

        private void HandleSubmit()
        {
            messageStore.Clear();
            if (editContext.Validate())
            {
                // Add tags to person model
                personModel.SkillTags = chosenTags.ToList();

                if (PersonId > 0)
                {
                    LogInformation($"Saving person {personModel?.Name}...");

                    // Edit
                    var res = PersonService.Update(Context, personModel);
                    if (res < 0)
                    {
                        // Duplicate found so show error message
                        LogWarning($"Duplicate person found with name {personModel?.Name} or initials {personModel?.ShortName}");
                        if (res == -1)
                        {
                            messageStore.Add(() => personModel.Name, "Duplicate person name found!");
                        }
                        else
                        {
                            messageStore.Add(() => personModel.ShortName, "Duplicate initials found!");
                        }
                        return;
                    };
                }
                else
                {
                    // Add new
                    var res = PersonService.Add(Context, personModel);
                    if (res < 0)
                    {
                        // Duplicate found so show error message
                        LogWarning($"Duplicate person found with name {personModel?.Name} or initials {personModel?.ShortName}");
                        if (res == -1)
                        {
                            messageStore.Add(() => personModel.Name, "Duplicate person name found!");
                        }
                        else
                        {
                            messageStore.Add(() => personModel.ShortName, "Duplicate initials found!");
                        }
                        return;
                    }
                }

                Navigation.NavigateTo("people");
            }
        }

        /// <summary>
        /// Deletes a person -- use with caution as it is very destructive!
        /// </summary>
        private async void DeletePersonAsync()
        {
            if (PersonId > 0)
            {
                // Prompt
                bool confirmed = await DialogService.Confirm($"You are about to delete person {personModel.Name}. This cannot be undone!",
                    "Delete Person") ?? false;
                if (confirmed)
                {
                    LogInformation($"Deleting person {personModel.Name}, ID {personModel.PersonId}");

                    // Delete from DB
                    PersonService.Delete(Context, personModel);

                    // Navigate back to the people list
                    Navigation.NavigateTo("people");
                }
            }
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding changes to person {personModel.Name}, ID {personModel.PersonId}");

            // Navigate back to the people list
            Navigation.NavigateTo("people");
        }

        /// <summary>
        /// Method to navigate to the capacity page for this person
        /// </summary>
        private void ViewCapacity()
        {
            Navigation.NavigateTo($"capacity?filterid={personModel.PersonId}");
        }
    }
}
