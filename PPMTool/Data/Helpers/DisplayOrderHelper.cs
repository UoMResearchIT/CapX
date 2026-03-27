using System.Linq.Expressions;
using System.Reflection;
using PPMTool.Enums;
using PPMTool.Enums.Attributes;

namespace PPMTool.Data.Helpers
{
    /// <summary>
    /// Provides helper methods for ordering entities and enumeration values based on display order attributes.
    /// </summary>
    /// <remarks>Use the methods in this class to generate sorting expressions or retrieve ordered lists
    /// according to custom display order logic defined by DisplayOrderAttribute. These utilities are useful for
    /// scenarios where a specific, user-defined order is required, such as displaying options in a UI or processing
    /// items in a particular sequence.</remarks>
    public static class DisplayOrderHelper
    {
        /// <summary>
        /// Creates a sorting expression that orders entities based on the display order specified by the
        /// DisplayOrderAttribute of an enum property.
        /// </summary>
        /// <remarks>This method enables sorting entities according to a custom order defined by
        /// DisplayOrderAttribute on enum members. Enum values without the attribute are sorted last. The returned
        /// expression can be used in LINQ queries for ordering.</remarks>
        /// <typeparam name="TEntity">The type of the entity containing the enum property.</typeparam>
        /// <typeparam name="TEnum">The enum type that defines the possible values for the property and is decorated with DisplayOrderAttribute.</typeparam>
        /// <param name="enumSelector">An expression that selects the enum property from the entity to be used for sorting.</param>
        /// <returns>An expression that evaluates to the display order for each entity, as defined by the DisplayOrderAttribute
        /// on the enum values. If the attribute is not present, int.MaxValue is used.</returns>
        public static Expression<Func<TEntity, int>> CreateOrderAttributeSortingExpression<TEntity, TEnum>(
           Expression<Func<TEntity, TEnum>> enumSelector)
           where TEnum : struct, Enum
        {
            var param = enumSelector.Parameters[0];
            var enumExpr = enumSelector.Body;

            Expression current = Expression.Constant(int.MaxValue);
            var enumType = typeof(TEnum);

            foreach (var value in Enum.GetValues<TEnum>().Reverse())
            {
                var member = enumType.GetMember(value.ToString()).Single();
                var attr = member.GetCustomAttribute<DisplayOrderAttribute>();

                var order = attr?.Order ?? int.MaxValue;

                current = Expression.Condition(
                    Expression.Equal(enumExpr, Expression.Constant(value)),
                    Expression.Constant(order),
                    current);
            }

            return Expression.Lambda<Func<TEntity, int>>(current, param);
        }

        /// <summary>
        /// Returns a collection of all values of the CostModel enumeration, ordered by their associated display order.
        /// </summary>
        /// <remarks>Use this method to obtain a list of CostModel values in a user-friendly or logical
        /// display order, as defined by the DisplayOrderAttribute. This is useful for populating UI elements or
        /// processing cost models in a specific sequence.</remarks>
        /// <returns>An IEnumerable of CostModel containing all CostModel values, sorted by the value of their
        /// DisplayOrderAttribute. Values without the attribute appear last.</returns>
        public static IEnumerable<CostModel> GetOrderListOfCostModels()
        {
            return Enum.GetValues<CostModel>()
                .OrderBy(x => x.GetAttribute<DisplayOrderAttribute>()?.Order ?? int.MaxValue)
                .ToList();
        }
    }
}
