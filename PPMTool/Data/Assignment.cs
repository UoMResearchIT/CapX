// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a sub task but with additional information about the project to which it belongs.
    /// </summary>
    public class Assignment : BaseAssignment
    {
        public SubTask SubTask { get; private set; }

        public Assignment(SubTask subTask, ProjectStatus projectStatus) : base(projectStatus)
        {
            SubTask = subTask;
        }

        public override bool IsWithin(DateTime testDate)
        {
            return SubTask.IsWithin(testDate);
        }

        public override bool IsWithin(DateTime startDate, DateTime endDate)
        {
            return SubTask.IsWithin(startDate, endDate);
        }
    }
}
