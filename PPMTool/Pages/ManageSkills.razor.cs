using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public partial class ManageSkills : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private TagService TagService { get; set; }

        RadzenDataGrid<SkillTag> skillTagGrid;
        IList<SkillTag> skillTags;
        SkillTag tagToInsert;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            using var context = new PPMToolContext();
            skillTags = TagService.GetAllTags(context).ToList();
        }

        async Task EditRow(SkillTag tag)
        {
            await skillTagGrid.EditRow(tag);
        }

        void OnUpdateRow(SkillTag tag)
        {
            if (tag == tagToInsert)
            {
                tagToInsert = null;
            }
            using var context = new PPMToolContext();
            TagService.Update(context, tag);
        }

        async Task SaveRow(SkillTag tag)
        {
            if (tag == tagToInsert)
            {
                tagToInsert = null;
            }

            await skillTagGrid.UpdateRow(tag);
        }

        async Task CancelEdit(SkillTag tag)
        {
            if (tag == tagToInsert)
            {
                tagToInsert = null;
            }

            skillTagGrid.CancelEditRow(tag);

            using var context = new PPMToolContext();
            var tagEntry = TagService.GetEntry(context, tag);
            if (tagEntry.State == EntityState.Modified)
            {
                tagEntry.CurrentValues.SetValues(tagEntry.OriginalValues);
                tagEntry.State = EntityState.Unchanged;
            }

            await skillTagGrid.Reload();
        }

        async Task DeleteRow(SkillTag tag)
        {
            if (tag == tagToInsert)
            {
                tagToInsert = null;
            }

            if (skillTags.Contains(tag))
            {
                using (var context = new PPMToolContext())
                {
                    // Remove the tag from all the people to whom it is attached
                    var people = PersonService.GetAll(context).Where(x => x.SkillTags.Contains(tag));
                    foreach (var person in people)
                    {
                        person.SkillTags.Remove(tag);
                        PersonService.Update(context, person);
                    }

                    // Remove tag
                    TagService.Delete(context, tag);
                }

                await skillTagGrid.Reload();
            }
            else
            {
                skillTagGrid.CancelEditRow(tag);
            }
        }

        async Task InsertRow()
        {
            tagToInsert = new SkillTag();
            await skillTagGrid.InsertRow(tagToInsert);
        }

        void OnCreateRow(SkillTag tag)
        {
            using var context = new PPMToolContext();
            TagService.Add(context, tag);
        }
    }
}
