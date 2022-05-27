using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a group of subtask that form a project
    /// </summary>
    public class Project : BaseTask
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; }

        public string PI { get; set; }

        public Portfolio Portfolio { get; set; }

        public IList<SubTask> Tasks { get; set; }

        public double Budget { get; set; }

        public double FundsReceived { get; set; }

        public FundingStatus FundingStatus { get; set; }

    }
}
