using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class AddPerson : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private TagService TagService { get; set; }

        [Inject]
        private IConfiguration Configuration { get; set; }

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel = new();
        private PPMToolContext context;
        private IEnumerable<Tag> availableTags;
        private EditContext editContext;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            context = new PPMToolContext();

            // Map entities to checkbox list items
            var entities = TagService.GetAll(context).ToList();
            var list = new List<Tag>();
            foreach (var t in entities) list.Add(new Tag(t.Name));
            availableTags = list;

            // Load the person model if necessary
            if (PersonId > -1)
            {
                personModel = PersonService.GetAll(context).FirstOrDefault(x => x.PersonId == PersonId);

                // Update the available tags state
                if (personModel != null)
                {
                    foreach (var tag in personModel.SkillTags)
                    {
                        var item = availableTags.FirstOrDefault(x => x.Name == tag.Name);
                        if (item != null) item.Checked = true;
                    }
                }
            }

            // Set the default day rate in the model if doesn't already exist
            else
            {
                var success = double.TryParse(Configuration["DefaultDayRate"], out var temp);
                if (success) personModel.DayRate = temp;
            }

            // Instantiate the edit context so we have a reference to it
            editContext = new EditContext(personModel);
        }

        private void EditAvailability()
        {
            // Check the existing model is valid first
            if (editContext.Validate())
            {
                HandleValidSubmit();

                Logger.LogInformation("Editing availability changes...");
                Navigation.NavigateTo($"/addavailabilitychange/{personModel.PersonId}");
            }
        }

        private void HandleValidSubmit()
        {
            Logger.LogInformation("Adding / editing person...");

            // Add tags to person model
            personModel.SkillTags = new List<SkillTag>();
            foreach (var t in availableTags)
            {
                if (t.Checked)
                {
                    var skillTagEntity = TagService.GetAll(context).FirstOrDefault(x => x.Name == t.Name);

                    if (skillTagEntity != null)
                        personModel.SkillTags.Add(skillTagEntity);
                }
            }

            if (PersonId > -1)
            {
                // Edit
                PersonService.Update(context, personModel);
            }
            else
            {
                // Add new
                if (PersonService.Add(context, personModel) < 0)
                {
                    // TODO: Duplicate found -- do something
                }
            }

            Navigation.NavigateTo("people");
        }
    }
}
