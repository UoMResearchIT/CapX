using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaMoney;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents an individual activity or phase of a project
    /// </summary>
    public class SubTask : BaseTask
    {
        public int SubTaskId { get; set; }

        public TaskType TaskType { get; set; }

        public IList<Resource> AssignedResources { get; set; }

        public IList<SubTask> Predecessors { get; set; }

        public void Schedule()
        {
            // TODO: Update the duration, work or units given the known constraints
        }
    }
}
