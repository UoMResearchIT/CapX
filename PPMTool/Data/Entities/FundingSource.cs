using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class FundingSource
    {
        public int FundingSourceId { get; set; }

        /// <summary>
        /// The actual account code e.g. R / P / A code or N/A if not applicable
        /// </summary>
        public string AccountCode { get; set; }

        /// <summary>
        /// Whether the funding source is associated with a UoM account code
        /// </summary>
        [Required]
        public bool HasAccountCode { get; set; }

        /// <summary>
        /// Type of funding source based on how it was costed (e.g. cost heading)
        /// </summary>
        [Required]
        public FundingSourceType FundingSourceType { get; set; }

        /// <summary>
        /// Project which uses this funding source
        /// </summary>
        [Required]
        public Project Project { get; set; }
    }
}
