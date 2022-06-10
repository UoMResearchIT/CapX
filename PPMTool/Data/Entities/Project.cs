using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a group of subtask that form a project
    /// </summary>
    public class Project : BaseTask
    {
        public int ProjectId { get; set; }

        [Required]
        public string PI { get; set; }

        [Required]
        public Portfolio Portfolio { get; set; }

        public IList<SubTask> SubTasks { get; set; }

        [Required]
        public double Budget { get; set; }

        [Required]
        public double FundsReceived { get; set; }

        [Required]
        public FundingStatus FundingStatus { get; set; }

        /// <summary>
        /// Updates the project summary based on the current state of subtasks and resources then updates the database
        /// </summary>
        public void UpdateProjectSummary()
        {
            // Set initial values
            DateTime startDate = DateTime.MaxValue;
            DateTime endDate = DateTime.MinValue;
            double cost = 0d;

            // Loop over all the subtasks
            foreach (var task in SubTasks)
            {
                // Check start date
                if (task.StartDate < startDate) startDate = task.StartDate;
                if (task.EndDate > endDate) endDate = task.EndDate;
                cost += task.ActualCost;
            }

            // Update project
            StartDate = startDate;
            EndDate = endDate;
            ActualCost = cost;
        }

    }
}
