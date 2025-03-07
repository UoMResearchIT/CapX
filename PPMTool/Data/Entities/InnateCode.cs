// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public class InnateCode : ILoggableClass
    {
        public int InnateCodeId { get; set; }

        /// <summary>
        /// Code of the activity
        /// </summary>
        [Required]
        public string ActivityCode { get; set; }

        /// <summary>
        /// Name of the activity
        /// </summary>
        [Required]
        public string ActivityName { get; set; }

        /// <summary>
        /// Whether this code is active and can be booked to
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The collection of innate code tasks that belong to this code
        /// </summary>
        public ICollection<InnateCodeTask> Tasks { get; set; } = new List<InnateCodeTask>();


        /// <summary>
        /// Joins the activity code and name together with a hyphen.
        /// </summary>
        /// <returns></returns>
        public string GetCodeAsString()
        {
            return $"{ActivityCode} - {ActivityName}";
        }

        /// <summary>
        /// Required implementation to identify this object in the logs
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return GetCodeAsString();
        }
    }
}
