using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// State an invoice can be in
    /// </summary>
    public enum InvoiceStatus
    {
        Unpaid,
        [Description("Partially Paid")]
        PartiallyPaid,
        Paid,
        Cancelled
    }
}
