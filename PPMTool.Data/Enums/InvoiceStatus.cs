using System.ComponentModel;

namespace PPMTool.Data.Enums
{
    /// <summary>
    /// State an invoice can be in
    /// </summary>
    public enum InvoiceStatus
    {
        [BadgeStyle(BadgeStyle.Danger)]
        Unpaid,
        [BadgeStyle(BadgeStyle.Warning)]
        [Description("Partially Paid")]
        PartiallyPaid,
        [BadgeStyle(BadgeStyle.Success)]
        Paid,
        [BadgeStyle(BadgeStyle.Light)]
        Cancelled
    }
}
