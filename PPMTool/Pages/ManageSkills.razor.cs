using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

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
            dataGridEntityService = TagService;
            dataGridEntities = TagService.GetAll(context).OrderBy(x => x.Name).ToList();
            LogInformation($"Viewing skills tags");
        }

        protected override async Task DeleteRow(SkillTag entity)
        {
            if (await DialogService.Confirm($"You are about to delete tag {entity.GetSensibleObjectName()}.", "Delete Tag") ?? false)
            {
                await base.DeleteRow(entity);

                // Remove the tag from all the people to whom it is attached
                var people = PersonService.GetAll(context).Where(x => x.SkillTags.Contains(entity));
                foreach (var person in people)
                {
                    LogInformation($"Removing skills tag {entity.GetSensibleObjectName()} from {person.Name}");
                    person.SkillTags.Remove(entity);
                    PersonService.Update(context, person);
                }

                dataGridEntityService.Delete(context, entity);
                LogInformation($"Deleted skills tag {entity.GetSensibleObjectName()}");
            }
        }
    }
}