using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
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

        private int count;
        private int pageCount = 15;

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

            Debug.WriteLine($"** {query.Count()} tags loaded!");

            // Filtering
            if (!string.IsNullOrEmpty(args.Filter))
            {
                query = query.Where(args.Filter);
            }

            // Now apply the skills tag filter
            if (args.Filters != null && args.Filters.Count() > 0)
            {
                // Filter on rareness if necessary
                var filter = args.Filters.FirstOrDefault(x => x.Property == "Rareness");
                var filterValue = filter?.FilterValue as string;
                if (filter != null && filterValue != null)
                {
                    query = query.Where(x => x.Rareness.ToString().ToLower() == filterValue.ToLower());
                }
            }

            // Sorting
            if (!string.IsNullOrEmpty(args.OrderBy))
            {
                var order = args.OrderBy.Split(" ");
                if (order.Length > 0 && order[0] == "Rareness")
                {
                    if (order.Length > 1 && order[1] == "asc")
                    {
                        query = query.OrderBy(x => x.Rareness);
                    }
                    else
                    {
                        query = query.OrderByDescending(x => x.Rareness);
                    }
                }
                else
                {
                    // Sort via the OrderBy method
                    query = query.OrderBy(args.OrderBy);
                }
            }

            // Assign to grid source
            count = query.Count();
            if (args.Skip == null)
            {
                dataGridEntities = query.Take(pageCount).ToList();
            }
            else
            {
                dataGridEntities = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
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