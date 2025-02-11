using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a received payment which may or may not be associated with an invoice
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Required]
        public int PaymentId { get; set; }

        /// <summary>
        /// Date the payment was received
        /// </summary>
        [Required]
        public DateTime ReceivedDate { get; set; }

        /// <summary>
        /// Project associated with this payment
        /// </summary>
        [Required]
        public Project Project { get; set; }

        /// <summary>
        /// Optional invoice to which the payent is linked
        /// </summary>
        public Invoice Invoice { get; set; }
    }
}
