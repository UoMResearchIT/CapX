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

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var tags = TagService.GetAllTags();
            var list = new List<Tag>();
            foreach (var t in tags) list.Add(new Tag(t));
            AvailableTags = list;
        }

        private void HandleValidSubmit()
        {
            Logger.LogInformation("Adding new person...");

            using var context = new PPMToolContext();
            if (!PersonService.AddPerson(context, personModel))
            {
                // TODO: Duplicate found -- do something
            }

            Navigation.NavigateTo("people");
        }
    }
}
