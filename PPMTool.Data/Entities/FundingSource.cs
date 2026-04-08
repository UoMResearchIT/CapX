using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Enums;
using PPMTool.Data.Enums.Attributes;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    public class FundingSource : BaseFinanceItem, ILoggableObject
    {
        public int FundingSourceId { get; set; }

        /// <summary>
        /// Type of funding source based on how it was costed (e.g. cost heading)
        /// </summary>
        [Required]
        public FundingSourceType FundingSourceType { get; set; }

        /// <summary>
        /// Whether the funding source is associated with a UoM account code
        /// </summary>
        [Required]
        public bool HasAccountCode { get; set; }

        /// <summary>
        /// The actual account code e.g. R / P / A code or N/A if not applicable
        /// </summary>
        public string? AccountCode { get; set; }

        /// <summary>
        ///  Some information about what this funding source is if known
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// This is the amount of money available in the funding source for RSE costs
        /// </summary>
        [Required]
        [DataType(DataType.Currency)]
        public double AmountAvailable { get; set; }

        /// <summary>
        /// Set of payments attached to this funding source
        /// </summary>
        public virtual ICollection<Payment> PaymentsFromSource { get; set; } = new List<Payment>();

        /// <summary>
        /// Set of resources funded from this source
        /// </summary>
        public virtual ICollection<Resource> ResourcesFunded { get; set; } = new List<Resource>();

        /// <summary>
        /// Details about the funding source to be posted to a note
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            var text = $"Type = {FundingSourceType.GetDescription()}";
            if (HasAccountCode)
            {
                text += $", Account Code = {AccountCode}";
            }
            text += $", Amount = {AmountAvailable.ToString("C")}";
            text += $", Description = {Description}";
            return text;
        }

        /// <inheritdoc/>
        public string GetSensibleObjectName()
        {
            return $"{(HasAccountCode ? AccountCode : "No Account")} ({FundingSourceType.GetAttribute<ShortDescriptionAttribute>()?.Value}) {AmountAvailable.ToString("C0")}";
        }
    }
}
