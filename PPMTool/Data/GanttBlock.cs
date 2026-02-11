// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a block on the schedule chart
    /// </summary>
    internal class GanttBlock : IChartItem
    {
        public GanttBlock(SubTask t, string groupName, bool isFake = false, bool isLeadershipTask = false)
        {
            Task = t;
            PredecessorGroupName = groupName;
            this.isFake = isFake;
            this.IsLeadershipTask = isLeadershipTask;
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
        /// This is to workaround a bug in Apex Charts where the sorting doesn't work if the series aren't all the same length
        /// </summary>
        private bool isFake;

        /// <summary>
        /// Whether this task is a leaderhsip task and hence doesn't have a proper subtask object associated with it in the DB.
        /// </summary>
        public bool IsLeadershipTask { get; private set; }

        public bool IsFake()
        {
            return isFake;
        }

        public bool IsHatched()
        {
            return Task.AssignedResources.Any(x => x.IsProvisional);
        }
    }
}
