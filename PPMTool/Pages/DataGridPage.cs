using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public abstract class DataGridPage : BasePage
    {
        protected RadzenDataGrid<SkillTag> dataGrid;
        protected IList<SkillTag> entities;
        protected SkillTag entityToInsert;
        protected IEntityService<SkillTag> entityService;
        protected PPMToolContext context;

        [Inject]
        private PersonService PersonService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            context = new PPMToolContext();
        }

        protected async virtual Task EditRow(SkillTag tag)
        {
            await dataGrid.EditRow(tag);
        }

        protected virtual void OnUpdateRow(SkillTag tag)
        {
            if (tag == entityToInsert)
            {
                entityToInsert = null;
            }
            entityService.Update(context, tag);
        }

        protected async virtual Task SaveRow(SkillTag tag)
        {
            if (tag == entityToInsert)
            {
                entityToInsert = null;
            }

            await dataGrid.UpdateRow(tag);
        }

        protected async virtual Task CancelEdit(SkillTag tag)
        {
            if (tag == entityToInsert)
            {
                entityToInsert = null;
            }

            dataGrid.CancelEditRow(tag);

            entityService.RestoreModel(context, ref tag);

            await dataGrid.Reload();
        }

        protected async virtual Task DeleteRow(SkillTag tag)
        {
            if (tag == entityToInsert)
            {
                entityToInsert = null;
            }

            if (entities.Contains(tag))
            {

                // Remove the tag from all the people to whom it is attached
                var people = PersonService.GetAll(context).Where(x => x.SkillTags.Contains(tag));
                foreach (var person in people)
                {
                    person.SkillTags.Remove(tag);
                    PersonService.Update(context, person);
                }

                // Remove tag
                entityService.Delete(context, tag);

                await dataGrid.Reload();
            }
            else
            {
                dataGrid.CancelEditRow(tag);
            }
        }

        protected async virtual Task InsertRow()
        {
            entityToInsert = new SkillTag();
            await dataGrid.InsertRow(entityToInsert);
        }

        protected virtual void OnCreateRow(SkillTag tag)
        {
            entityService.Add(context, tag);
        }
    }
}
