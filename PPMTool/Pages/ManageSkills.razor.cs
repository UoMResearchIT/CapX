using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
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
        private PPMToolContext context;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            context = new PPMToolContext();
            skillTags = TagService.GetAll(context).ToList();
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

                // Remove the tag from all the people to whom it is attached
                var people = PersonService.GetAll(context).Where(x => x.SkillTags.Contains(tag));
                foreach (var person in people)
                {
                    person.SkillTags.Remove(tag);
                    PersonService.Update(context, person);
                }

                // Remove tag
                TagService.Delete(context, tag);

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
            TagService.Add(context, tag);
        }
    }
}
