// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageFinancialReferenceValueSets : DataGridPage<FinancialReferenceValueSet>
    {
        [Inject]
        public FinancialReferenceService FinancialReferenceService { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntities = FinancialReferenceService.GetAllValueSets(Context).ToList();

            // Only superusers can edit value sets
            EditAuthorised = ActiveUserRoleType == RoleType.Superuser;
            LogInformation("Viewing financial reference value set grid");
        }

        /// <summary>
        /// Navigates back to the financial references management page.
        /// </summary>
        private void BackToReferences()
        {
            Navigation.NavigateTo("managefinref");
        }

        /// <summary>
        /// Handles the results of the creation of a new financial reference value set.
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnCreateRow(FinancialReferenceValueSet entity)
        {
            entity.Name = entity.Name?.Trim() ?? string.Empty;

            // Re-uses the FinRefService so can't call the base method.
            var result = FinancialReferenceService.AddValueSet(Context, entity);
            if (!CheckSuccessAndSetError(result))
            {
                return;
            }

            entityToInsert = null;
        }

        /// <summary>
        /// Handles the results of the update of an existing financial reference value set.
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnUpdateRow(FinancialReferenceValueSet entity)
        {
            entity.Name = entity.Name?.Trim() ?? string.Empty;
            var result = FinancialReferenceService.UpdateValueSet(Context, entity);
            if (!CheckSuccessAndSetError(result))
            {
                return;
            }

            entityToUpdate = null;
        }

        /// <summary>
        /// Checks the result of an operation and sets an error message if it was not successful.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool CheckSuccessAndSetError(int result)
        {
            if (result == -1)
            {
                MarkRowSaveFailed();

                SetErrorMessage(new StatusMessage(
                    "A value set with that name already exists.",
                    StatusMessage.MessageType.Error));

                return false;
            }

            if (result == -3)
            {
                MarkRowSaveFailed();

                SetErrorMessage(new StatusMessage(
                    "Value set name is required.",
                    StatusMessage.MessageType.Error));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles the cancellation of editing a financial reference value set, restoring its original state.
        /// </summary>
        /// <param name="entity"></param>
        protected override void CancelEdit(FinancialReferenceValueSet entity)
        {
            // Unsaved row being inserted - just remove it completely
            if (ReferenceEquals(entityToInsert, entity))
            {
                dataGrid.CancelEditRow(entity);

                if (dataGridEntities.Contains(entity))
                {
                    dataGridEntities.Remove(entity);
                }

                Reset();
                dataGrid.Reload();
                return;
            }

            // Existing row being edited
            Reset();
            FinancialReferenceService.RestoreModel(Context, ref entity);
            dataGrid.CancelEditRow(entity);
        }

        /// <summary>
        /// Handles the deletion of a financial reference value set, confirming with the user before proceeding and checking for dependencies that would prevent deletion.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override async Task DeleteRow(FinancialReferenceValueSet entity)
        {
            // Unsaved rows can't be deleted from the DB as they were never saved
            if (ReferenceEquals(entityToInsert, entity))
            {
                await base.DeleteRow(entity);
                return;
            }

            if (await DialogService.Confirm($"You are about to delete value set {entity.GetSensibleObjectName()}.", "Delete") ?? false)
            {
                var result = FinancialReferenceService.DeleteValueSet(Context, entity);
                if (result == -2)
                {
                    SetErrorMessage(new StatusMessage("Cannot delete value set while it is linked to financial references or workload model changes.", StatusMessage.MessageType.Error));
                    return;
                }

                await base.DeleteRow(entity);
            }
        }
    }
}
