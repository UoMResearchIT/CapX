using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// RAG status for whether a task is in budget or not
    /// </summary>
    public enum BudgetStatus
    {
        [Description("Fully Rechargable")]
        FullyInBudget,

        [Description("Partially Rechargable")]
        PartiallyInBudget,

        [Description("Not Rechargable")]
        NotInBudget,
    }
}
