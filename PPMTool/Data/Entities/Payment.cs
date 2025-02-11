using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a received payment which may or may not be associated with an invoice
    /// </summary>
    public class Payment : FinanceItem
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Required]
        public int PaymentId { get; set; }

        /// <summary>
        /// Optional invoice to which the payent is linked
        /// </summary>
        public Invoice Invoice { get; set; }
    }
}
