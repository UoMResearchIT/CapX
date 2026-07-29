// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Models.Interfaces;

namespace PPMTool.Models
{
    /// <summary>
    /// Represents a block on the schedule chart
    /// </summary>
    public class GanttBlock : IChartItem
    {
        public GanttBlock(SubTask t, string groupName, bool isFake = false, Duty duty = Duty.ProjectWork)
        {
            Task = t;
            PredecessorGroupName = groupName;
            this.isFake = isFake;
            this.BlockDuty = duty;
        }

        /// <summary>
        /// The subtask which is associated with the Gantt Block
        /// </summary>
        public SubTask Task { get; private set; }

        /// <summary>
        /// When grouping tasks that are linked, this is the name of the group
        /// </summary>
        public string PredecessorGroupName { get; private set; }

        /// <summary>
        /// Whether this task is a fake task which exists in either the provisional or confirmed series so they both match in length.
        /// This is to workaround a bug in Apex Charts where the sorting doesn't work if the series aren't all the same length.
        /// </summary>
        private bool isFake;

        /// <summary>
        /// What duty this task this block aligns to based on the subtask that it relates to.
        /// </summary>
        public Duty BlockDuty { get; private set; }

        public bool IsFake()
        {
            return isFake;
        }

        /// <summary>
        /// Whether any of the assigned resources are marked as provisional
        /// </summary>
        /// <returns></returns>
        public bool IsHatched()
        {
            return Task.AssignedResources.Any(x => x.IsProvisional);
        }
    }
}
