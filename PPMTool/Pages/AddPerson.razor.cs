using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;
using static PPMTool.Data.StatusMessage;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class AddPerson : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private bool viewAuthorised;
        private Person personModel = new();
        private IList<Person> managers = new List<Person>();
        private EditContext editContext;
        private ValidationMessageStore messageStore;
        private bool isSuperUser;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // Load the person model if necessary
            if (PersonId > 0 && personModel?.PersonId != PersonId)
            {
                personModel = PersonService.GetAll(Context).FirstOrDefault(x => x.PersonId == PersonId);

                // Update permissions if viewing / editing an existing person
                if (personModel != null)
                {
                    // Edit should only be authorised for the line manager or superusers
                    EditAuthorised = IsSuperuserOrLineManagerOfThisPerson(personModel);

                    // Developers can view their own page; managers can view all people pages; superuser can view everything
                    viewAuthorised = IsSuperuserOrLineManagerOrPerson(personModel) || ActiveUserRoleType == RoleType.Manager;

                    // Reset the action bar
                    SetDefaultActionBar(HandleSubmit, DiscardChanges);
                }
            }

            // Instantiate the edit context so we have a reference to it
            editContext = new EditContext(personModel);
            messageStore = new ValidationMessageStore(editContext);

            LogInformation((personModel?.PersonId > 0 ? $"Editing person {personModel?.Name}" : $"Adding new person") + $" - ViewAuthorised = {viewAuthorised}; EditAuthorised = {EditAuthorised}");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Setup the default action bar
            SetDefaultActionBar(HandleSubmit, DiscardChanges);

            // Find out if superuser for delete button
            isSuperUser = ActiveUserRoleType == RoleType.Superuser;

            // Superusers and managers can add new users so must have at least view permissions by default
            viewAuthorised = isSuperUser || ActiveUserRoleType == RoleType.Manager;

            // Map the list of managers for drop down
            managers = UserService.GetAll(Context)
                .Where(x => (x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser) && x.Person != null && x.Person?.PersonId != personModel.PersonId)
                .Select(x => x.Person)
                .DistinctBy(x => x.PersonId)
                .OrderBy(x => x.Name)
                .ToList();

            LogInformation("Initialising add/edit person page");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender) return;

            Loading = false;
            StateHasChanged();
        }

        /// <summary>
        /// Method to navigate to the availability page for this person after validating the model
        /// </summary>
        private void EditAvailability()
        {
            // Check the existing model is valid first
            ClearErrorMessage();
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
            var messages = editContext.GetValidationMessages();
            if (messages.Any())
            {
                SetErrorMessage(new StatusMessage(messages.First(), MessageType.Error));
            }
        }

        /// <summary>
        /// Method to navigate to the absences page for this person after validating the model
        /// </summary>
        private void EditAbsence()
        {
            // Check the existing model is valid first
            ClearErrorMessage();
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
            var messages = editContext.GetValidationMessages();
            if (messages.Any())
            {
                SetErrorMessage(new StatusMessage(messages.First(), MessageType.Error));
            }
        }

        /// <summary>
        /// Method to navigate to the skills page for this person after validating the model
        /// </summary>
        private void EditSkills()
        {
            // Check the existing model is valid first
            ClearErrorMessage();
            if (editContext.Validate())
            {
                HandleSubmit();

                // Check for any further messages added by DB interactions
                if (!editContext.GetValidationMessages().Any())
                {
                    LogInformation("Editing skills...");
                    Navigation.NavigateTo($"skills/{personModel?.PersonId}");
                }
            }
            var messages = editContext.GetValidationMessages();
            if (messages.Any())
            {
                SetErrorMessage(new StatusMessage(messages.First(), MessageType.Error));
            }
        }

        private void HandleSubmit()
        {
            ClearErrorMessage();
            messageStore.Clear();
            editContext.NotifyValidationStateChanged();
            if (editContext.Validate())
            {
                // Extra validation
                if (!CheckLineManagerSet())
                {
                    UpdateErrorOnActionBarFromContextMessageStore();
                    return;
                }

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
                    }
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
                    }
                }

                if (!editContext.GetValidationMessages().Any())
                {
                    Navigation.NavigateTo("people");
                }
            }

            // Transfer message store to action bar
            UpdateErrorOnActionBarFromContextMessageStore();
        }

        /// <summary>
        /// Method to set the error message on the action bar from the edit context
        /// </summary>
        private void UpdateErrorOnActionBarFromContextMessageStore()
        {
            // Set error messages based on the message store
            var messages = editContext.GetValidationMessages();
            if (messages.Any())
            {
                SetErrorMessage(new StatusMessage(messages.First(), MessageType.Error));
            }
            else
            {
                ClearErrorMessage();
            }
        }

        /// <summary>
        /// Add custom message to the message store about the line manager
        /// </summary>
        /// <returns></returns>
        private bool CheckLineManagerSet()
        {
            if (personModel.LineManager == null)
            {
                messageStore.Add(() => personModel.LineManager, "Person must have a line manager set!");
                return false;
            }
            return true;
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
