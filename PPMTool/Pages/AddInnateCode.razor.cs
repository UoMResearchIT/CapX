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
        private StatusMessage errorMessage;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (InnateCodeId > 0)
            {
                innateCode = InnateCodeService.GetById(context, InnateCodeId);
                dataGridEntities = innateCode.Tasks.ToList();
            }
            else
            {
                dataGridEntities = new List<InnateCodeTask>();
            }

            LogInformation($"Editing innate code {innateCode?.GetCodeAsString()}");
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
                // TODO: Validate and exit early

                // TODO: Reset error message
                //errorMessage = null;

                // Assign tasks to code
                innateCode.Tasks.Clear();
                foreach (var task in dataGridEntities)
                {
                    innateCode.Tasks.Add(task);
                }

                // Write to the database
                LogInformation($"Saving innate code {innateCode?.GetCodeAsString()} with tasks {string.Join(",", innateCode?.Tasks)}.");

                if (innateCode?.InnateCodeId != 0)
                {
                    InnateCodeService.Update(context, innateCode);
                }
                else
                {
                    InnateCodeService.Add(context, innateCode);
                }

                // Navigate back
                Navigation.NavigateTo($"managecodes");
            }
        }

        protected override void CancelEdit(InnateCodeTask entity)
        {
            LogInformation($"Cancel edit row for {entity.GetSensibleObjectName()}");
            Reset();
            InnateCodeService.RestoreModel(context, ref entity);
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
    }
}
