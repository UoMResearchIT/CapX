// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class InnateCodeTask : ILoggableClass
    {
        public int InnateCodeTaskId { get; set; }

        [Required]
        public string TaskName { get; set; }

        /// <summary>
        /// This is the category of work that this timesheet code is classified as when doing WLM analysis
        /// </summary>
        [Required]
        public Duty Duty { get; set; }

        /// <summary>
        /// The code which owns this task
        /// </summary>
        [Required]
        public virtual InnateCode InnateCode { get; set; }

        public string GetSensibleObjectName()
        {
            return $"{InnateCode?.GetSensibleObjectName()} | {TaskName}";
        }
    }
}
