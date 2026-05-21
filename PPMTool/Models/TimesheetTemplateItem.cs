// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Entities;
using PPMTool.Data.Interfaces;

namespace PPMTool.Models
{
    /// <summary>
    /// Represents timesheet template item
    /// </summary>
    public class TimesheetTemplateItem : ILoggableObject
    {
        /// <summary>
        /// Represents the ID of the timesheet entry record
        /// </summary>
        public int TimesheetTemplateItemId { get; set; }

        /// <summary>
        /// Represents the innate code
        /// </summary>
        [Required]
        public InnateCode InnateCode { get; set; } = null!;

        /// <summary>
        /// Represents the innate code task
        /// </summary>
        [Required]
        public InnateCodeTask InnateCodeTask { get; set; } = null!;

        /// <summary>
        /// Returns a useful string to identify the entity
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return $"{InnateCode.GetSensibleObjectName} : {InnateCodeTask?.GetSensibleObjectName()}";
        }
    }
}