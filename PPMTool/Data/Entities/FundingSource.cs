using System.ComponentModel.DataAnnotations;
using DotNetExtensions;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class FundingSource : BaseFinanceItem, ILoggableClass
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
        public string AccountCode { get; set; }

        /// <summary>
        ///  Some information about what this funding source is if known
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Set of payments attached to this funding source
        /// </summary>
        public ICollection<Payment> PaymentsFromSource { get; set; }

        /// <summary>
        /// Set of resources funded from this source
        /// </summary>
        public ICollection<Resource> ResourcesFunded { get; set; }

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
            return text;
        }

        /// <inheritdoc/>
        public string GetSensibleObjectName()
        {
            return $"[{FundingSourceId}] {(HasAccountCode ? AccountCode : "No Account")} ({FundingSourceType.GetAttribute<ShortDescriptionAttribute>().Value})";
        }
    }
}
