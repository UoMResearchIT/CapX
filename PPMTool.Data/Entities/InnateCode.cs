// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public class InnateCode : BaseBookableItem
    {
        public int InnateCodeId { get; set; }

        /// <summary>
        /// Code of the activity
        /// </summary>
        [Required]
        public string ActivityCode { get; set; } = null!;

        /// <summary>
        /// Name of the activity
        /// </summary>
        [Required]
        public string ActivityName { get; set; } = null!;

        /// <summary>
        /// Whether this code contains sensitive information that should be restricted to line manager and the person
        /// </summary>
        public bool IsSensitive { get; set; }

        /// <summary>
        /// The collection of innate code tasks that belong to this code
        /// </summary>
        public virtual ICollection<InnateCodeTask> Tasks { get; set; } = new List<InnateCodeTask>();

        /// <summary>
        /// Method to deactivate all tasks too -- assuming they are deep loaded
        /// </summary>
        public void DeactivateAllTasks()
        {
            foreach (var task in Tasks)
            {
                task.IsActive = false;
            }
        }

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
        public override string GetSensibleObjectName()
        {
            return GetCodeAsString();
        }
    }
}
