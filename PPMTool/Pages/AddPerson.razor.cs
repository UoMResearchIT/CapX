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

        private Person personModel = new();

        private IEnumerable<Tag> AvailableTags { get; set; }
        private IEnumerable<SkillTag> AvailableEntities { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            using var context = new PPMToolContext();
            AvailableEntities = TagService.GetAllTags(context);
            var list = new List<Tag>();
            foreach (var t in AvailableEntities) list.Add(new Tag(t.Name));
            AvailableTags = list;
        }

        private void HandleValidSubmit()
        {
            Logger.LogInformation("Adding new person...");

            // Add tags to person model
            personModel.SkillTags = new List<SkillTag>();
            foreach (var t in AvailableTags)
            {
                var skillTag = AvailableEntities.FirstOrDefault(x => x.Name == t.Name);
                if (skillTag != null)
                    personModel.SkillTags.Add(skillTag);
            }

            using var context = new PPMToolContext();
            if (!PersonService.AddPerson(context, personModel))
            {
                // TODO: Duplicate found -- do something
            }

            Navigation.NavigateTo("people");
        }
    }
}
