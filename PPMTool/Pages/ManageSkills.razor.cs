using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
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
        private SkillTagService TagService { get; set; }

        /// <summary>
        /// Method to detect a duplicate on save or update and display error message
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool IsDuplicatedSkill(SkillTag entity)
        {
            if (TagService.DuplicateDetected(Context, entity))
            {
                ErrorMessage = new StatusMessage("An entry with the same name or controlled name already exists.", StatusMessage.MessageType.Error);
                return true;
            }
            ErrorMessage = null;
            return false;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = TagService;
            dataGridEntities = TagService.GetAll(Context).OrderBy(x => x.Name).ToList();
            LogInformation($"Viewing skills tags");
        }

        protected override async Task SaveRow(SkillTag entity)
        {
            if (IsDuplicatedSkill(entity)) return;
            await base.SaveRow(entity);
        }

        protected override async Task DeleteRow(SkillTag entity)
        {
            if (await DialogService.Confirm($"You are about to delete tag {entity.GetSensibleObjectName()}.", "Delete Tag") ?? false)
            {
                await base.DeleteRow(entity);

                // Remove the tag from all the people to whom it is attached
                TagService.DeleteOwnedSkillsAssociatedWithTag(Context, entity);

                // Remove from data grid
                dataGridEntityService.Delete(Context, entity);
                LogInformation($"Deleted skills tag {entity.GetSensibleObjectName()}");
            }
        }

        /// <summary>
        /// Verify the controlled names for those pending and save to DB
        /// </summary>
        private void VerifyControlledNames()
        {
            Loading = true;
            Task.Run(async () =>
            {
                var toVerify = dataGridEntities.Where(x => x.HasValidWikiLink == LinkCheckState.Pending).ToList();
                foreach (var tag in toVerify)
                {
                    var res = await tag.UpdateValidLink();
                    if (res != LinkCheckState.Pending)
                    {
                        TagService.Update(Context, tag);
                    }
                }
            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });
            });
        }
    }
}