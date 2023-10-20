using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class ManageSkills : DataGridPage<SkillTag>
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private TagService TagService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Set up the base page
            dataGridEntityService = TagService;
            dataGridEntities = TagService.GetAll(context).OrderBy(x => x.Name).ToList();
        }

        protected override async Task DeleteRow(SkillTag entity)
        {
            if (entity == entityToInsert)
            {
                entityToInsert = null;
            }

            if (dataGridEntities.Contains(entity))
            {

                // Remove the tag from all the people to whom it is attached
                var people = PersonService.GetAll(context).Where(x => x.SkillTags.Contains(entity));
                foreach (var person in people)
                {
                    person.SkillTags.Remove(entity);
                    PersonService.Update(context, person);
                }

                // Remove tag
                dataGridEntityService.Delete(context, entity);

                await dataGrid.Reload();
            }
            else
            {
                dataGrid.CancelEditRow(entity);
            }
        }
    }
}