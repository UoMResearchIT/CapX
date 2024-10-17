using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class Competency
    {
        public int CompetencyId { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Objective { get; set; }

        [Required]
        public int Grade { get; set; }

        [Required]
        public CompetencyCategory Category { get; set; }
    }
}
