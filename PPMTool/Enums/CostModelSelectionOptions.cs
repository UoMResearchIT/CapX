namespace PPMTool.Enums
{
    /// <summary>
    /// Defines the display order for cost model selection controls.
    /// The same ordering is used for dropdown lists and data grid sorting.
    /// </summary>
    internal static class CostModelSelectionOptions
    {
        internal static IReadOnlyList<CostModel> DisplayOrder { get; } = new[]
        {
            CostModel.TechAndLeadershipWithIndirects,
            CostModel.TechAndLeadership,
            CostModel.TechOnlyWithIndirects,
            CostModel.TechOnly,
            CostModel.DayRate
        };

        /// <summary>
        /// Lookup from <see cref="CostModel"/> to its sort position, derived from <see cref="DisplayOrder"/>.
        /// Values not in <see cref="DisplayOrder"/> sort last.
        /// </summary>
        private static readonly IReadOnlyDictionary<CostModel, int> sortIndexMap =
            DisplayOrder
                .Select((model, index) => (model, index))
                .ToDictionary(x => x.model, x => x.index);

        /// <summary>
        /// Returns the sort index for a given <see cref="CostModel"/> based on <see cref="DisplayOrder"/>.
        /// Values not in <see cref="DisplayOrder"/> sort last.
        /// </summary>
        internal static int GetSortIndex(CostModel model)
        {
            return sortIndexMap.TryGetValue(model, out var index) ? index : DisplayOrder.Count;
        }
    }
}
