// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class FinancialReferenceService : BaseEntityService<FinancialReference>
    {
        public FinancialReferenceService(ILogger<FinancialReferenceService> logger) : base(logger)
        {
        }

        public override int Add(PPMToolContext context, FinancialReference entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }

            if (!CheckValueSetLinks(context, entity))
            {
                return -2;
            }

            context.FinancialReferences.Add(entity);
            if (commitChanges) CommitChanges(context);
            return entity.FinancialReferenceId;
        }

        public override void Delete(PPMToolContext context, FinancialReference entity, bool commitChanges = true)
        {
            // Delete all associated FinancialReferenceValues before deleting the FinancialReference entity
            var values = context.FinancialReferenceValues.Where(x => x.FinancialReferenceId == entity.FinancialReferenceId);
            context.FinancialReferenceValues.RemoveRange(values);

            // Now delete the FinancialReference entity
            context.FinancialReferences.Remove(entity);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Retrieves all financial reference entities from the specified database context. Will return an empty list if no references exist.
        /// </summary>
        /// <param name="context">The database context used to access financial reference entities. Cannot be null.</param>
        /// <returns>An enumerable collection of all financial reference entities in the context.</returns>
        public override IEnumerable<FinancialReference> GetAll(PPMToolContext context)
        {
            // Retrieve all financial references and include their associated values
            return context.FinancialReferences
                .OrderBy(x => x.FinancialYear)
                .Include(x => x.Values)
                .ThenInclude(x => x.FinancialReferenceValueSet);
        }

        /// <summary>
        /// Returns the Financial References from the db. If none have been added then the app will
        /// crash in certain places if the Finance Feature is not enabled. The check in this method
        /// helps avoid this exception by passing back a non-null, non-zero IEnumerable to satisfy
        /// the requesting call. This was added to allow bypassing of the crash when a new Project
        /// was added without the Finance Feature being enabled (so no Financial References exist).
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<FinancialReference> GetAllOrDefault(PPMToolContext context)
        {
            // If DB table is empty return a list of one default item to avoid a financial ref exception
            if (!context.FinancialReferences.Any())
            {
                return new List<FinancialReference> { new FinancialReference() };
            }

            // Default to the standard call if there are references in the list
            return GetAll(context);
        }

        public override int Update(PPMToolContext context, FinancialReference entity, bool commitChanges = true)
        {
            if (DuplicateDetected(context, entity))
            {
                return -1;
            }

            if (!CheckValueSetLinks(context, entity))
            {
                return -2;
            }

            context.FinancialReferences.Update(entity);
            if (commitChanges) CommitChanges(context);
            return entity.FinancialReferenceId;
        }

        /// <summary>
        /// Checks for duplicate financial references based on the financial year and value names. Returns true if a duplicate is detected, otherwise false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, FinancialReference entity)
        {
            var duplicateYear = context.FinancialReferences.Any(x => x.FinancialYear == entity.FinancialYear && x.FinancialReferenceId != entity.FinancialReferenceId);
            var values = entity.Values ?? new List<FinancialReferenceValue>();

            // Considered a duplicate reference set if any value-set IDs repeat within this financial year set
            var duplicateValueSetIds = values
                .Where(x => x.FinancialReferenceValueSetId > 0)
                .GroupBy(x => x.FinancialReferenceValueSetId)
                .Any(x => x.Count() > 1);

            return duplicateYear || duplicateValueSetIds;
        }

        /// <summary>
        /// Checks the links between financial reference values and their corresponding value sets.
        /// Returns true if all links are valid, otherwise false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool CheckValueSetLinks(PPMToolContext context, FinancialReference entity)
        {
            var values = entity.Values ?? new List<FinancialReferenceValue>();
            foreach (var value in values)
            {
                if (value.FinancialReferenceValueSetId <= 0)
                {
                    return false;
                }

                var valueSet = context.FinancialReferenceValueSets.FirstOrDefault(x => x.FinancialReferenceValueSetId == value.FinancialReferenceValueSetId);
                if (valueSet == null)
                {
                    return false;
                }

                value.FinancialReferenceValueSet = valueSet;
            }

            return true;
        }

        /// <summary>
        /// Retrieves a financial reference entity by its primary key from the specified database context. Returns null if no matching entity is found.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="financialReferenceId"></param>
        /// <returns></returns>
        internal FinancialReference GetById(PPMToolContext context, int financialReferenceId)
        {
            return GetAll(context).FirstOrDefault(x => x.FinancialReferenceId == financialReferenceId);
        }

        /// <summary>
        /// Gets available financial reference values from the suitable financial reference set for the provided date.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public IEnumerable<FinancialReferenceValue> GetCostValueOptionsForDate(PPMToolContext context, DateTime date)
        {
            if (context == null || !context.FinancialReferences.Any())
            {
                return Enumerable.Empty<FinancialReferenceValue>();
            }

            var finRef = GetFinancialReferenceForDate(context, date);
            return GetCostValueOptions(finRef);
        }

        /// <summary>
        /// Gets available financial reference values from the provided financial reference set.
        /// </summary>
        /// <param name="finRef"></param>
        /// <returns></returns>
        public IEnumerable<FinancialReferenceValue> GetCostValueOptions(FinancialReference finRef)
        {
            // No values in the financial reference set
            if (finRef?.Values == null)
            {
                return Enumerable.Empty<FinancialReferenceValue>();
            }

            // Subset of valid values
            var values = finRef.Values
                .Where(x => !string.IsNullOrWhiteSpace(x.FinancialReferenceValueSet?.Name));

            // Returns distinct values ordered by key name
            return values
                .GroupBy(x => x.FinancialReferenceValueSet.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.FinancialReferenceValueSet.Name)
                .ToList();
        }

        /// <summary>
        /// Retrieves all financial reference value sets from the specified database context, ordered by name. Returns an empty list if no value sets exist.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<FinancialReferenceValueSet> GetAllValueSets(PPMToolContext context)
        {
            return context.FinancialReferenceValueSets
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Adds a new financial reference value set to the database context after validating its name for uniqueness and non-emptiness.
        /// Returns the ID of the newly added value set, or an error code if validation fails.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public int AddValueSet(PPMToolContext context, FinancialReferenceValueSet entity, bool commitChanges = true)
        {
            entity.Name = entity.Name?.Trim() ?? string.Empty;
            var validationResult = ValidateValueSet(context, entity);
            if (validationResult != 0) return validationResult;

            // Add the new value set to the context and commit changes if specified
            context.FinancialReferenceValueSets.Add(entity);
            if (commitChanges) CommitChanges(context);
            return entity.FinancialReferenceValueSetId;
        }

        /// <summary>
        /// Validates the provided financial reference value set for uniqueness and non-emptiness of its name.
        /// Returns 0 if validation passes, -1 if a duplicate name is found, or -3 if the name is empty or whitespace.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private int ValidateValueSet(PPMToolContext context, FinancialReferenceValueSet entity)
        {
            entity.Name = entity.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                return -3; // Name is empty or whitespace
            }
            var duplicateName = context.FinancialReferenceValueSets.Any(x =>
                x.FinancialReferenceValueSetId != entity.FinancialReferenceValueSetId
                && x.Name.ToLower() == entity.Name.ToLower());
            if (duplicateName)
            {
                return -1; // Duplicate name found
            }
            return 0; // Validation passed
        }

        /// <summary>
        /// Updates an existing financial reference value set in the database context after validating its name for uniqueness and non-emptiness.
        /// Returns the ID of the updated value set, or an error code if validation fails.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public int UpdateValueSet(PPMToolContext context, FinancialReferenceValueSet entity, bool commitChanges = true)
        {
            entity.Name = entity.Name?.Trim() ?? string.Empty;
            var validationResult = ValidateValueSet(context, entity);
            if (validationResult != 0) return validationResult;

            context.FinancialReferenceValueSets.Update(entity);
            if (commitChanges) CommitChanges(context);
            return entity.FinancialReferenceValueSetId;
        }

        /// <summary>
        /// Deletes a financial reference value set from the database context after checking if it is in use by any financial reference values or workload model changes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="commitChanges"></param>
        /// <returns></returns>
        public int DeleteValueSet(PPMToolContext context, FinancialReferenceValueSet entity, bool commitChanges = true)
        {
            var inUse = context.FinancialReferenceValues.Any(x => x.FinancialReferenceValueSetId == entity.FinancialReferenceValueSetId)
                || context.WorkloadModelChanges.Any(x => x.CostValueSetId == entity.FinancialReferenceValueSetId);

            if (inUse)
            {
                return -2;
            }

            context.FinancialReferenceValueSets.Remove(entity);
            if (commitChanges) CommitChanges(context);
            return 1;
        }

        /// <summary>
        /// Method to return a suitable financial reference following set logic given a date in a certain financial year
        /// </summary>
        /// <param name="context"></param>
        /// <param name="startDate"></param>
        /// <returns></returns>
        public FinancialReference GetFinancialReferenceForDate(PPMToolContext context, DateTime startDate)
        {
            return GetAll(context).GetSuitableFinancialReference(startDate);
        }
    }
}
