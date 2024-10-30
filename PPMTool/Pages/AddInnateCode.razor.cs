using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddInnateCode : DataGridPage<InnateCodeTask>
    {
        [Parameter]
        public int InnateCodeId { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        private InnateCode innateCode;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (InnateCodeId > 0)
            {
                innateCode = InnateCodeService.GetById(Context, InnateCodeId);
                dataGridEntities = innateCode.Tasks.ToList();
            }
            else
            {
                innateCode = new InnateCode();
                dataGridEntities = new List<InnateCodeTask>();
            }

            LogInformation($"Adding / Editing innate code {innateCode?.GetCodeAsString()}");
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding innate code changes!");
            Navigation.NavigateTo($"managecodes");
        }

        private void HandleValidSubmit()
        {
            if (innateCode != null)
            {
                // Reset error message
                ErrorMessage = null;

                // Write to the database
                LogInformation($"Saving innate code {innateCode?.GetCodeAsString()} with tasks {string.Join(",", innateCode?.Tasks)}.");

                // Assign tasks to code
                innateCode.Tasks.Clear();
                foreach (var task in dataGridEntities)
                {
                    innateCode.Tasks.Add(task);
                }

                // Try to add or update
                int result = -1;
                if (innateCode?.InnateCodeId != 0)
                {
                    result = InnateCodeService.Update(Context, innateCode);
                }
                else
                {
                    result = InnateCodeService.Add(Context, innateCode);
                }

                if (result == -1)
                {
                    ErrorMessage = new StatusMessage("Either the name or code duplicates another already in the database or multiple tasks have the same name", StatusMessage.MessageType.Error);
                    return;
                }

                // Navigate back
                Navigation.NavigateTo($"managecodes");
            }
        }

        protected override void CancelEdit(InnateCodeTask entity)
        {
            LogInformation($"Cancel edit row for {entity.GetSensibleObjectName()}");
            Reset();
            InnateCodeService.RestoreModel(Context, ref entity);
            dataGrid.CancelEditRow(entity);
        }

        protected override void OnCreateRow(InnateCodeTask entity)
        {
            LogInformation($"Created new row for {entity.GetSensibleObjectName()}");
            dataGridEntities.Add(entity);
            entityToInsert = null;
        }

        protected override void OnUpdateRow(InnateCodeTask entity)
        {
            LogInformation($"Updated row for {entity.GetSensibleObjectName()}");
            Reset();
        }

        private async void AddDefaults()
        {
            LogInformation($"Adding default tasks");
            dataGridEntities.Add(new InnateCodeTask
            {
                TaskName = "Management",
                InnateCode = innateCode,
                Duty = Enums.Duty.ProjectAndServiceMgmt
            });
            dataGridEntities.Add(new InnateCodeTask
            {
                TaskName = "Development",
                InnateCode = innateCode,
                Duty = Enums.Duty.ProjectWork
            });
            await dataGrid.Reload();
        }
    }
}
