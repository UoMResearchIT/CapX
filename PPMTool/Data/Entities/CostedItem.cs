namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an item that has planned and actual hours and costs
    /// </summary>
    public abstract class CostedItem : ObjectWithStatusMessages
    {
        /// <summary>
        /// The planned effort to be expended on the item
        /// </summary>
        public double PlannedWorkHours { get; set; }

        /// <summary>
        /// The effort expended on this item to date
        /// </summary>
        public double ActualWorkHours { get; set; }

        /// <summary>
        /// The amount of the money this item will cost based on the planned work
        /// </summary>
        public double PlannedCost { get; set; }

        /// <summary>
        /// The actual cost of the item based on effort expended on it
        /// </summary>
        public double ActualCost { get; set; }

        /// <summary>
        /// If applicable, the planned indirects computed as a proportion of the planned cost
        /// </summary>
        public double PlannedIndirectCost { get; set; }

        /// <summary>
        /// If applicable, the actual indirects compute as a proportion of the actual cost
        /// </summary>
        public double ActualIndirectCost { get; set; }

        /// <summary>
        /// Gets the technical part of the total planned costs of the task (difference between total and indirects)
        /// </summary>
        /// <returns></returns>
        public double GetPlannedTechnicalCost()
        {
            return PlannedCost - PlannedIndirectCost;
        }
    }
}
