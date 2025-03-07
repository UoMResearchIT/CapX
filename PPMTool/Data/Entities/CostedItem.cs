// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿namespace PPMTool.Data.Entities
{
    public class CostedItem : ObjectWithStatusMessages
    {
        /// <summary>
        /// The planned effort to be expended on the item
        /// </summary>
        public double PlannedWorkHours { get; set; }

        /// <summary>
        /// The effort expended on this item to date
        /// </summary>
        public double ActualWorkHours { get; set; }

        /// <summary>
        /// The amount of the money this item will cost based on the planned work
        /// </summary>
        public double PlannedCost { get; set; }

        /// <summary>
        /// The actual cost of the item based on effort expended on it
        /// </summary>
        public double ActualCost { get; set; }
    }
}
