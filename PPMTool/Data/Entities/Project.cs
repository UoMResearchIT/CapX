using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a group of subtask that form a project
    /// </summary>
    public class Project : BaseTask
    {
        public int ProjectId { get; set; }

        [Required]
        public string PI { get; set; }

        [Required]
        public Portfolio Portfolio { get; set; }

        public IList<SubTask> Tasks { get; set; } = new List<SubTask>();

        [Required]
        public double Budget { get; set; }

        [Required]
        public double FundsReceived { get; set; }

        [Required]
        public FundingStatus FundingStatus { get; set; }

    }
}
