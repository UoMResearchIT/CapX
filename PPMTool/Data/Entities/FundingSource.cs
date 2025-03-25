using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class FundingSource
    {
        public int FundingSourceId { get; set; }

        [Required]
        public string AccountCode { get; set; }

        [Required]
        public FundingSourceType FundingSourceType { get; set; }
    }
}
