// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Radzen;

namespace PPMTool.Helpers
{
    /// <summary>
    /// Provides helper methods for applying filters to IQueryable collections.
    /// </summary>
    public static class FilterHelper
    {
        /// <summary>
        /// Applies a string filter to the given IQueryable collection based on the specified FilterDescriptor and value selector.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        /// <param name="valueSelector"></param>
        /// <returns></returns>
        public static IQueryable<T> ApplyStringFilter<T>(
            IQueryable<T> query,
            FilterDescriptor filter,
            Func<T, string> valueSelector)
        {
            return filter.FilterOperator switch
            {
                FilterOperator.IsNull => query.Where(item => valueSelector(item) == null),
                FilterOperator.IsNotNull => query.Where(item => valueSelector(item) != null),
                FilterOperator.IsEmpty => query.Where(item => string.IsNullOrEmpty(valueSelector(item))),
                FilterOperator.IsNotEmpty => query.Where(item => !string.IsNullOrEmpty(valueSelector(item))),
                _ => ApplyStringValueFilter(query, filter, valueSelector)
            };
        }

        /// <summary>
        /// Applies a nullable integer filter to the given IQueryable collection based on the specified FilterDescriptor and value selector.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        /// <param name="valueSelector"></param>
        /// <returns></returns>
        public static IQueryable<T> ApplyNullableIntFilter<T>(
            IQueryable<T> query,
            FilterDescriptor filter,
            Func<T, int?> valueSelector)
        {
            if (filter.FilterOperator == FilterOperator.IsNull)
            {
                return query.Where(item => valueSelector(item) == null);
            }

            if (filter.FilterOperator == FilterOperator.IsNotNull)
            {
                return query.Where(item => valueSelector(item) != null);
            }

            var filterValue = filter.FilterValue switch
            {
                int intValue => intValue,
                string stringValue when int.TryParse(stringValue, out var parsedValue) => parsedValue,
                _ => (int?)null
            };

            if (filterValue == null)
            {
                return query;
            }

            return filter.FilterOperator switch
            {
                FilterOperator.Equals => query.Where(item => valueSelector(item) == filterValue),
                FilterOperator.NotEquals => query.Where(item => valueSelector(item) != filterValue),
                FilterOperator.GreaterThan => query.Where(item => valueSelector(item) > filterValue),
                FilterOperator.GreaterThanOrEquals => query.Where(item => valueSelector(item) >= filterValue),
                FilterOperator.LessThan => query.Where(item => valueSelector(item) < filterValue),
                FilterOperator.LessThanOrEquals => query.Where(item => valueSelector(item) <= filterValue),
                _ => query
            };
        }

        /// <summary>
        /// Applies a string value filter to the given IQueryable collection based on the specified FilterDescriptor and value selector.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        /// <param name="valueSelector"></param>
        /// <returns></returns>
        private static IQueryable<T> ApplyStringValueFilter<T>(
            IQueryable<T> query,
            FilterDescriptor filter,
            Func<T, string> valueSelector)
        {
            // Do nothing if not filter value
            var filterValue = (filter.FilterValue as string)?.Trim();
            if (string.IsNullOrWhiteSpace(filterValue))
            {
                return query;
            }

            var filterValueLower = filterValue.ToLowerInvariant();

            // Manually implement the string filter operators to avoid issues with Dynamic LINQ and null values
            return filter.FilterOperator switch
            {
                FilterOperator.Contains => query.Where(item => (valueSelector(item) ?? string.Empty).Trim().ToLowerInvariant().Contains(filterValueLower)),
                FilterOperator.DoesNotContain => query.Where(item => !(valueSelector(item) ?? string.Empty).Trim().ToLowerInvariant().Contains(filterValueLower)),
                FilterOperator.StartsWith => query.Where(item => (valueSelector(item) ?? string.Empty).Trim().ToLowerInvariant().StartsWith(filterValueLower)),
                FilterOperator.EndsWith => query.Where(item => (valueSelector(item) ?? string.Empty).Trim().ToLowerInvariant().EndsWith(filterValueLower)),
                FilterOperator.Equals => query.Where(item => (valueSelector(item) ?? string.Empty).Trim().ToLowerInvariant() == filterValueLower),
                FilterOperator.NotEquals => query.Where(item => (valueSelector(item) ?? string.Empty).Trim().ToLowerInvariant() != filterValueLower),
                _ => query
            };
        }
    }
}
