// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager")]
    public partial class ManageFinancialReferences : DataGridPage<FinancialReference>
    {
        [Inject]
        public FinancialReferenceService FinancialReferenceService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = FinancialReferenceService;
            dataGridEntities = FinancialReferenceService.GetAll(Context).ToList();

            // Only superusers can edit financial references
            EditAuthorised = ActiveUserRoleType == RoleType.Superuser;
            LogInformation("Viewing financial reference set grid");
        }

        /// <summary>
        /// Deletes a financial reference after confirming with the user.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private async Task DeleteReference(FinancialReference entity)
        {
            if (await DialogService.Confirm($"You are about to delete reference {entity.GetSensibleObjectName()}.", "Delete") ?? false)
            {
                await base.DeleteRow(entity);
                dataGridEntityService.Delete(Context, entity);
                LogInformation($"Deleted financial reference set {entity.GetSensibleObjectName()}");
            }
        }

        /// <summary>
        /// Navigates to the page for managing value sets.
        /// </summary>
        private void ManageValueSets()
        {
            Navigation.NavigateTo("managefinref/valuesets");
        }

        /// <summary>
        /// Navigates to the edit page for a financial reference.
        /// </summary>
        /// <param name="entity"></param>
        private void EditReference(FinancialReference entity)
        {
            Navigation.NavigateTo($"managefinref/addfinancialreference/{entity.FinancialReferenceId}");
        }

        /// <summary>
        /// Navigates to the add page for a new financial reference.
        /// </summary>
        private void AddReference()
        {
            Navigation.NavigateTo("managefinref/addfinancialreference/-1");
        }

        /// <summary>
        /// Navigates to the add page and pre-populates it by copying an existing reference set.
        /// </summary>
        /// <param name="entity"></param>
        private void CopyReference(FinancialReference entity)
        {
            Navigation.NavigateTo($"managefinref/addfinancialreference/-1?copyFromFinancialReferenceId={entity.FinancialReferenceId}");
        }
    }
}
