using System;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an invoice or a payment request for a project. May be paid by one or more payments.
    /// </summary>
    public class Invoice : ObjectWithStatusMessages
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Required]
        public int InvoiceId { get; set; }

        /// <summary>
        /// Date the invoice was raised
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Auto-generated reference based on the project RTP number, the financial year and the preceding invoice reference
        /// </summary>
        [Required]
        public string InvoiceReference { get; set; }

        /// <summary>
        /// Details of the invoice
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Value of the invoice
        /// </summary>
        [Required]
        public double Value { get; set; }

        /// <summary>
        /// Status of the invoice
        /// </summary>
        [Required]
        public InvoiceStatus Status { get; set; }

        /// <summary>
        /// Project to which this invoice is attached
        /// </summary>
        [Required]
        public Project Project { get; set; }
    }
}
