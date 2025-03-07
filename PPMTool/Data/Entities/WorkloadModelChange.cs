// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents the workload model of a person and the date when it came into effect
    /// </summary>
    public class WorkloadModelChange : PersonProperty
    {
        public int WorkloadModelChangeId { get; set; }

        [Required]
        public int Grade { get; set; }

        [Required]
        public DateTime ChangeDate { get; set; }

        [Required]
        public double ProjectWorkFTE { get; set; }

        [Required]
        public double BusinessAsUsualFTE { get; set; }

        [Required]
        public double PersonalDevelopmentFTE { get; set; }

        [Required]
        public double StaffManagementFTE { get; set; }

        [Required]
        public double ProjectAndServiceManagementFTE { get; set; }

        [Required]
        public double ArchitectureFTE { get; set; }

        private double serviceManagementFTE;
        [Required]
        public double ServiceManagementFTE
        {
            get => serviceManagementFTE;
            set
            {
                if (value != serviceManagementFTE)
                {
                    serviceManagementFTE = value;
                    UpdatePSM();
                }
            }
        }

        private double projectManagementFTE;
        [Required]
        public double ProjectManagementFTE
        {
            get => projectManagementFTE;
            set
            {
                if (value != projectManagementFTE)
                {
                    projectManagementFTE = value;
                    UpdatePSM();
                }
            }
        }

        /// <summary>
        /// Method to update the Project and Service Management FTE when either Project Management FTE or Service Management FTE is updated
        /// </summary>
        private void UpdatePSM()
        {
            ProjectAndServiceManagementFTE = Math.Round(1000 * (ProjectManagementFTE + ServiceManagementFTE)) / 1000d;
        }


        /// <summary>
        /// Optional notes to explain anything about the change
        /// </summary>
        public string Notes { get; set; }

        public override string GetSensibleObjectName()
        {
            return $"WLM Change entry for {Person?.Name}";
        }

        /// <summary>
        /// Method to provide the sum of FTE assigned across the workload model
        /// </summary>
        /// <returns></returns>
        public double Total()
        {
            return ProjectWorkFTE + BusinessAsUsualFTE + PersonalDevelopmentFTE + StaffManagementFTE + ProjectAndServiceManagementFTE + ArchitectureFTE;
        }

        /// <summary>
        /// Method to get the WLM values for each duty as a dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<Duty, float> GetDutyMapping()
        {
            return new Dictionary<Duty, float>
                {
                    { Duty.Other, 0 },
                    { Duty.ProjectWork, (float)ProjectWorkFTE },
                    { Duty.BAU, (float)BusinessAsUsualFTE },
                    { Duty.PersonalDevelopment, (float)PersonalDevelopmentFTE },
                    { Duty.StaffMgmt, (float)StaffManagementFTE },
                    { Duty.ProjectAndServiceMgmt, (float)ProjectAndServiceManagementFTE},
                    { Duty.RSA, (float)ArchitectureFTE },
                };
        }
    }
}
