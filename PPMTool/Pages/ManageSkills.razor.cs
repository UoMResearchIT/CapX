using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class ManageSkills : DataGridPage
    {
        //[Inject]
        //private PersonService PersonService { get; set; }

        [Inject]
        private TagService TagService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Set up the base page
            entityService = TagService;
            entities = TagService.GetAll(context).OrderBy(x => x.Name).ToList();
        }

        protected override Task DeleteRow(SkillTag tag)
        {
            return base.DeleteRow(tag);
        }
    }
}
