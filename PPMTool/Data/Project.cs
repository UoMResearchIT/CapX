using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaMoney;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a group of subtask that form a project
    /// </summary>
    public class Project : BaseTask
    {
        public int ProjectId { get; set; }

        public Portfolio Portfolio { get; set; }

        public IList<SubTask> Tasks { get; set; }

        public Money Budget { get; set; }

        public Money FundsReceived { get; set; }

    }
}
