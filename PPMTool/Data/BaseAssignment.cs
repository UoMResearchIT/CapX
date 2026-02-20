// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Some kind of assignment used to plot a Gantt chart.
    /// </summary>
    public abstract class BaseAssignment : IWithin
    {
        public abstract bool IsWithin(DateTime testDate);
        public abstract bool IsWithin(DateTime startDate, DateTime endDate);

        public ProjectStatus ProjectStatus { get; private set; }

        public BaseAssignment(ProjectStatus projectStatus)
        {
            ProjectStatus = projectStatus;
        }
    }
}
