using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a received payment which may or may not be associated with an invoice
    /// </summary>
    public class Payment : FinanceItem, ILoggableClass
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

        /// <summary>
        /// Mandatory funding source to which payment is attached
        /// </summary>
        [Required]
        public FundingSource Source { get; set; }

        /// <summary>
        /// To identify the Invoice in the logs and on exports
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return $"Payment {PaymentId}";
        }
    }
}
