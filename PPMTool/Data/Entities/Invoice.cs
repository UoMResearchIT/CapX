using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an invoice or a payment request for a project. May be paid by one or more payments.
    /// </summary>
    public class Invoice : FinanceItem
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Required]
        public int InvoiceId { get; set; }

        /// <summary>
        /// Auto-generated reference based on the project RTP number, the financial year and the preceding invoice reference
        /// </summary>
        [Required]
        public string InvoiceReference { get; set; }

        /// <summary>
        /// Status of the invoice
        /// </summary>
        [Required]
        public InvoiceStatus Status { get; set; }

        /// <summary>
        /// The URL of the invoice document on SharePoint
        /// </summary>
        [Required]
        [DataType(DataType.Url)]
        public string InvoiceUrl { get; set; }

        /// <summary>
        /// An optional list of payments that pay all or part of this invoice
        /// </summary>
        [Required]
        public ICollection<Payment> Payments { get; set; }
    }
}
