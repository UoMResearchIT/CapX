using System.ComponentModel.DataAnnotations;
using DotNetExtensions;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class FundingSource : BaseFinanceItem
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
    }
}
