using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a received payment which may or may not be associated with an invoice
    /// </summary>
    public class Payment : FinanceItem, ILoggableObject
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Required]
        public int PaymentId { get; set; }

        /// <summary>
        /// Optional invoice to which the payent is linked
        /// </summary>
        public virtual Invoice? Invoice { get; set; }

        /// <summary>
        /// Mandatory funding source to which payment is attached
        /// </summary>
        [Required]
        public virtual FundingSource Source { get; set; } = null!;

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
