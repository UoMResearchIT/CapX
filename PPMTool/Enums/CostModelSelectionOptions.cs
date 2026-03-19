using System.Reflection;
using PPMTool.Enums.Attributes;

namespace PPMTool.Enums
{
    /// <summary>
    /// Derives the display order for cost model selection controls from the
    /// <see cref="DisplayOrderAttribute"/> on each <see cref="CostModel"/> value.
    /// The same ordering is used for dropdown lists and data grid sorting.
    /// </summary>
    internal static class CostModelSelectionOptions
    {
        internal static IReadOnlyList<CostModel> DisplayOrder { get; } =
            Enum.GetValues<CostModel>()
                .Select(model => (
                    model,
                    order: typeof(CostModel)
                        .GetField(model.ToString())!
                        .GetCustomAttribute<DisplayOrderAttribute>()?.Order ?? int.MaxValue))
                .OrderBy(x => x.order)
                .Select(x => x.model)
                .ToList()
                .AsReadOnly();

        /// <summary>
        /// Lookup from <see cref="CostModel"/> to its sort position, derived from <see cref="DisplayOrderAttribute"/>.
        /// Values without the attribute sort last.
        /// </summary>
        private static readonly IReadOnlyDictionary<CostModel, int> sortIndexMap =
            DisplayOrder
                .Select((model, index) => (model, index))
                .ToDictionary(x => x.model, x => x.index);

        /// <summary>
        /// Returns the sort index for a given <see cref="CostModel"/> based on <see cref="DisplayOrderAttribute"/>.
        /// Values without the attribute sort last.
        /// </summary>
        internal static int GetSortIndex(CostModel model)
        {
            return sortIndexMap.TryGetValue(model, out var index) ? index : DisplayOrder.Count;
        }
    }
}
