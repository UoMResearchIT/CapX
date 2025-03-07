// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System.ComponentModel;
using Radzen;
using static PPMTool.Enums.Extensions;

namespace PPMTool.Enums
{
    /// <summary>
    /// The status of a timesheet
    /// </summary>
    public enum TimesheetStatus
    {
        /// <summary>
        /// Timesheets that have not yet been submitted
        /// </summary>
        [Description("New/Unsubmitted")]
        [BadgeStyle(BadgeStyle.Info)]
        New,

        /// <summary>
        /// Timesheets that have been submitted but not yet approved
        /// </summary>
        [Description("Submitted")]
        [BadgeStyle(BadgeStyle.Primary)]
        Submitted,

        /// <summary>
        /// Timesheets that have been rejected
        /// </summary>
        [Description("Rejected")]
        [BadgeStyle(BadgeStyle.Danger)]
        Rejected,

        /// <summary>
        /// Timesheets that have been approved
        /// </summary>
        [Description("Approved")]
        [BadgeStyle(BadgeStyle.Success)]
        Approved
    }
}
