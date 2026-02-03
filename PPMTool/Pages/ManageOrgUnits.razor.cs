using System.Diagnostics;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using Radzen;

namespace PPMTool.Pages
{
    /*
    [Authorize(Roles = "Superuser")]
    public partial class ManageSkills : DataGridPage<Faculty>
    {
        private int count;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            EditAuthorised = ActiveUserRoleType == RoleType.Superuser;
            LogInformation($"Viewing skills tags");
        }

        protected override async Task SaveRow(Faculty entity)
        {
            if (IsDuplicatedSkill(entity)) return;
            await base.SaveRow(entity);
        }

        protected override async Task DeleteRow(Faculty entity)
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
    }
    */
}