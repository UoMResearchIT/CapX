using System.Diagnostics;
using System.Linq.Dynamic.Core;
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
        private SkillTagService TagService { get; set; }

        private int count;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = TagService;
            Loading = true;
            EnqueueLoadData(GetLoadTask);
            LogInformation($"Viewing skills tags");
        }

        protected override async Task SaveRow(SkillTag entity)
        {
            if (IsDuplicatedSkill(entity)) return;
            await base.SaveRow(entity);
        }

        protected override void OnCreateRow(SkillTag entity)
        {
            // Override as need to set the value of rareness and rareness count
            entity.Rareness = SkillRareness.Epic;
            entity.RarenessCount = 0;

            // Now call the base method which adds it to the DB
            base.OnCreateRow(entity);
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
                await dataGrid.Reload();
            }
        }

        /// <summary>
        /// Returns a standard task to get the data for the grid
        /// </summary>
        /// <returns></returns>
        private Task GetLoadTask()
        {
            return Task.Run(() =>
            {
                LoadDataGrid(new LoadDataArgs());
            })
                .ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });
            });
        }


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

        /// <summary>
        /// Method fired when a column is filtered or sorted to allow us to custom filter or sort
        /// </summary>
        /// <param name="args"></param>
        private void LoadDataGrid(LoadDataArgs args)
        {
            // Order by name by default
            IQueryable<SkillTag> query = TagService.GetAll(Context).OrderBy(x => x.Name).AsQueryable();

            // Filtering
            if (!string.IsNullOrEmpty(args.Filter))
            {
                if (args.Filter.StartsWith("Rareness"))
                {
                    var filter = args.Filters.FirstOrDefault(x => x.Property == "Rareness");
                    var filterValue = filter?.FilterValue as int?;
                    if (filterValue != null)
                    {
                        query = query.Where(x => (int)x.Rareness == filterValue);
                    }
                }
                else
                {
                    query = query.Where(args.Filter);
                }
            }

            // Sorting
            if (!string.IsNullOrEmpty(args.OrderBy))
            {
                if (args.OrderBy.StartsWith("Rareness"))
                {
                    var order = args.Sorts.FirstOrDefault(x => x.Property == "Rareness");
                    if (order.SortOrder == SortOrder.Ascending)
                    {
                        query = query.OrderBy(x => (int)x.Rareness).ThenByDescending(x => x.RarenessCount);
                    }
                    else
                    {
                        query = query.OrderByDescending(x => (int)x.Rareness).ThenByDescending(x => x.RarenessCount);
                    }
                }
                else
                {
                    query = query.OrderBy(args.OrderBy);
                }
            }

            // Assign to grid source
            var data = query.ToList();
            count = data.Count;
            dataGridEntities = data.ToList();

            Debug.WriteLine($"** {data.Count()} skills loaded. {dataGridEntities.Count()} displayed.");
        }

        /// <summary>
        /// Verify the controlled names for those pending and save to DB
        /// </summary>
        private void VerifyControlledNames()
        {
            Loading = true;
            Task.Run(async () =>
            {
                var toVerify = await TagService.GetAllPendingAsync(Context);
                Debug.WriteLine($"** {toVerify.Count} tags to verify...");
                foreach (var tag in toVerify)
                {
                    var res = await tag.UpdateValidLinkAsync();
                    if (res != LinkCheckState.Pending)
                    {
                        Logger.LogInformation($"Updating the wiki link status for {tag.ControlledName}");
                        TagService.Update(Context, tag);
                    }
                }
                LoadDataGrid(new LoadDataArgs());
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