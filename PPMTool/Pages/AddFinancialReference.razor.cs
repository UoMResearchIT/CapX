// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class AddFinancialReference : DataGridPage<FinancialReferenceValue>
    {
        [Parameter]
        public int FinancialReferenceId { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "copyFromFinancialReferenceId")]
        public int? CopyFromFinancialReferenceId { get; set; }

        [Inject]
        private FinancialReferenceService FinancialReferenceService { get; set; }

        private FinancialReference financialReference;
        private List<FinancialReferenceValueSet> financialReferenceValueSets = new();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            financialReferenceValueSets = FinancialReferenceService.GetAllValueSets(Context).ToList();

            if (FinancialReferenceId > 0)
            {
                financialReference = FinancialReferenceService.GetById(Context, FinancialReferenceId);

                // If the financial reference set is not found, we will create a new one with the provided FinancialReferenceId.
                // This is to handle cases where the user navigates directly to the edit page with an invalid ID.
                dataGridEntities = financialReference?.Values?.Select(x => new FinancialReferenceValue
                {
                    FinancialReferenceValueId = x.FinancialReferenceValueId,
                    FinancialReferenceValueSetId = x.FinancialReferenceValueSetId,
                    FinancialReferenceValueSet = financialReferenceValueSets.FirstOrDefault(y => y.FinancialReferenceValueSetId == x.FinancialReferenceValueSetId) ?? x.FinancialReferenceValueSet,
                    Value = x.Value,
                    FinancialReferenceId = x.FinancialReferenceId,
                    FinancialReference = x.FinancialReference
                }).ToList() ?? new List<FinancialReferenceValue>();
            }

            // If the FinancialReferenceId is not provided, but a CopyFromFinancialReferenceId is provided, we will copy the values from the source financial reference set to create a new one.
            else if (CopyFromFinancialReferenceId.HasValue && CopyFromFinancialReferenceId.Value > 0)
            {
                // Fetch the source financial reference set to copy from
                var sourceReference = FinancialReferenceService.GetById(Context, CopyFromFinancialReferenceId.Value);

                // If the source reference is null then it just produces a blank entry
                financialReference = new FinancialReference
                {
                    FinancialYear = (sourceReference?.FinancialYear ?? DateTime.Today.Year) + 1,
                    Values = (sourceReference?.Values ?? Enumerable.Empty<FinancialReferenceValue>())
                        .Select(x => new FinancialReferenceValue
                        {
                            FinancialReferenceValueSetId = x.FinancialReferenceValueSetId,
                            FinancialReferenceValueSet = financialReferenceValueSets.FirstOrDefault(y => y.FinancialReferenceValueSetId == x.FinancialReferenceValueSetId),
                            Value = x.Value
                        })
                        .ToList()
                };

                dataGridEntities = financialReference.Values.ToList();
            }
            else
            {
                financialReference = new FinancialReference();
                dataGridEntities = new List<FinancialReferenceValue>();
            }

            RefreshValueSetLinks();
            SetDefaultActionBar(HandleValidSubmit, DiscardChanges);
            LogInformation($"Adding / Editing financial reference set {financialReference?.GetSensibleObjectName()}");
        }

        /// <summary>
        /// Discards changes and navigates back to the manage financial references page.
        /// </summary>
        private void DiscardChanges()
        {
            LogInformation("Discarding financial reference set changes");
            Navigation.NavigateTo("managefinref");
        }

        /// <summary>
        /// Handles the valid submission of the financial reference form. Validates the input and either updates or adds the financial reference set.
        /// </summary>
        private void HandleValidSubmit()
        {
            if (financialReference == null)
            {
                return;
            }

            // Clear any previous error messages and reset the financial reference values stored on the model
            ClearErrorMessage();
            financialReference.Values.Clear();

            // Associate each value with the financial reference set
            foreach (var value in dataGridEntities)
            {
                value.FinancialReference = financialReference;
                value.FinancialReferenceValueSet = financialReferenceValueSets.FirstOrDefault(x => x.FinancialReferenceValueSetId == value.FinancialReferenceValueSetId);
                financialReference.Values.Add(value);
            }

            // Validate that all values have a value-set association
            if (financialReference.Values.Any(x => x.FinancialReferenceValueSetId <= 0 || x.FinancialReferenceValueSet == null))
            {
                SetErrorMessage(new StatusMessage("All values must be associated with a value set.", StatusMessage.MessageType.Error));
                return;
            }

            // Validation on duplicate sets by year or duplicate value names within the set happens in the service
            int result;
            if (financialReference.FinancialReferenceId != 0)
            {
                result = FinancialReferenceService.Update(Context, financialReference);
            }
            else
            {
                result = FinancialReferenceService.Add(Context, financialReference);
            }

            // If the result is -1, it indicates a validation failure due to duplicate financial year or duplicate value sets within the set
            if (result == -1)
            {
                SetErrorMessage(new StatusMessage("Financial year must be unique and value sets cannot duplicate in a set.", StatusMessage.MessageType.Error));
                return;
            }

            // If the result is -2, it indicates that one or more selected value sets were invalid
            if (result == -2)
            {
                SetErrorMessage(new StatusMessage("One or more selected value sets are invalid.", StatusMessage.MessageType.Error));
                return;
            }

            // If the result is 0, it indicates a failure to save the financial reference set
            if (result == 0)
            {
                SetErrorMessage(new StatusMessage("Failed to save the financial reference set.", StatusMessage.MessageType.Error));
                return;
            }

            // Success so navigate back to the manage financial references page
            Navigation.NavigateTo("managefinref");
        }

        /// <summary>
        /// Gets the available financial reference value sets for a given row, excluding those that are already selected in other rows.
        /// This ensures that each value set can only be associated with one value in the current financial reference set.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private IEnumerable<FinancialReferenceValueSet> GetAvailableValueSetsForRow(FinancialReferenceValue row)
        {
            var selectedIds = dataGridEntities
                .Where(x => !ReferenceEquals(x, row) && x.FinancialReferenceValueSetId > 0)
                .Select(x => x.FinancialReferenceValueSetId)
                .ToHashSet();

            return financialReferenceValueSets
                .Where(x => !selectedIds.Contains(x.FinancialReferenceValueSetId) || x.FinancialReferenceValueSetId == row.FinancialReferenceValueSetId)
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Refreshes the links between financial reference values and their corresponding value sets.
        /// This method ensures that each financial reference value in the data grid has its FinancialReferenceValueSet property correctly set based on the current list of available value sets.
        /// </summary>
        private void RefreshValueSetLinks()
        {
            foreach (var value in dataGridEntities)
            {
                value.FinancialReferenceValueSet = financialReferenceValueSets.FirstOrDefault(x => x.FinancialReferenceValueSetId == value.FinancialReferenceValueSetId);
            }
        }

        /// <summary>
        /// Cancels the edit operation for a given financial reference value. Restores the original state of the entity and cancels the edit row in the data grid.
        /// </summary>
        /// <param name="entity"></param>
        protected override void CancelEdit(FinancialReferenceValue entity)
        {
            LogInformation($"Cancel edit row for {entity.GetSensibleObjectName()}");
            Reset();
            FinancialReferenceService.RestoreModel(Context, ref entity);
            dataGrid.CancelEditRow(entity);
            RefreshValueSetLinks();
        }

        /// <summary>
        /// Inserts a new row into the data grid for adding a financial reference value.
        /// It selects the first available financial reference value set that is not already associated with an existing value in the current financial reference set and assigns it to the new entity being inserted.
        /// </summary>
        /// <returns></returns>
        protected override async Task InsertRow()
        {
            await base.InsertRow();

            var selectedIds = dataGridEntities
                .Where(x => x.FinancialReferenceValueSetId > 0)
                .Select(x => x.FinancialReferenceValueSetId)
                .ToHashSet();

            var firstAvailable = financialReferenceValueSets
                .FirstOrDefault(x => !selectedIds.Contains(x.FinancialReferenceValueSetId));

            if (firstAvailable != null)
            {
                entityToInsert.FinancialReferenceValueSetId = firstAvailable.FinancialReferenceValueSetId;
                entityToInsert.FinancialReferenceValueSet = firstAvailable;
            }
        }

        /// <summary>
        /// Handles the creation of a new financial reference value. Associates the new value with the current financial reference set and adds it to the data grid entities.
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnCreateRow(FinancialReferenceValue entity)
        {
            entity.FinancialReference = financialReference;
            entity.FinancialReferenceValueSet = financialReferenceValueSets.FirstOrDefault(x => x.FinancialReferenceValueSetId == entity.FinancialReferenceValueSetId);
            dataGridEntities.Add(entity);
            entityToInsert = null;
        }

        /// <summary>
        /// Handles the update of an existing financial reference value. Associates the updated value with the current financial reference set and resets the data grid state.
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnUpdateRow(FinancialReferenceValue entity)
        {
            entity.FinancialReference = financialReference;
            entity.FinancialReferenceValueSet = financialReferenceValueSets.FirstOrDefault(x => x.FinancialReferenceValueSetId == entity.FinancialReferenceValueSetId);
            Reset();
            RefreshValueSetLinks();
        }
    }
}
