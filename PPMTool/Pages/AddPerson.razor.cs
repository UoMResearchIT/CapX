using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

        [Parameter]
        public int PersonId { get; set; }

        private Person personModel = new();
        private PPMToolContext context;

        private IEnumerable<Tag> AvailableTags { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            context = new PPMToolContext();

            // Map entities to checkbox list items
            var entities = TagService.GetAllTags(context).ToList();
            var list = new List<Tag>();
            foreach (var t in entities) list.Add(new Tag(t.Name));
            AvailableTags = list;

            // Load the person model if necessary
            if (PersonId > -1)
            {
                personModel = PersonService.GetAll(context).FirstOrDefault(x => x.PersonId == PersonId);

                // Update the available tags state
                if (personModel != null)
                {
                    foreach (var tag in personModel.SkillTags)
                    {
                        var item = AvailableTags.FirstOrDefault(x => x.Name == tag.Name);
                        if (item != null) item.Checked = true;
                    }
                }
            }
        }

        private void HandleValidSubmit()
        {
            Logger.LogInformation("Adding / editing person...");

            // Add tags to person model
            personModel.SkillTags = new List<SkillTag>();
            foreach (var t in AvailableTags)
            {
                if (t.Checked)
                {
                    var skillTagEntity = TagService.GetAllTags(context).FirstOrDefault(x => x.Name == t.Name);

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
                if (!PersonService.AddPerson(context, personModel))
                {
                    // TODO: Duplicate found -- do something
                }
            }

            Navigation.NavigateTo("people");
        }
    }
}
