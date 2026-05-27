// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Models
{
    public class DemandChartItem : BaseDemandChartItem
    {

        // Workload Model //

        /// <summary>
        /// FTE available to do project work
        /// </summary>
        public float ProjectFTE { get; set; }

        /// <summary>
        /// FTE available to do BAU
        /// </summary>
        public float BAUFTE { get; set; }

        /// <summary>
        /// FTE available to do Personal Development
        /// </summary>
        public float PersonalDevFTE { get; set; }

        /// <summary>
        /// FTE available to do PSM
        /// </summary>
        public float PSMFTE { get; set; }

        /// <summary>
        /// FTE available to do Line Management
        /// </summary>
        public float StaffManFTE { get; set; }

        /// <summary>
        /// FTE available to do RSA work
        /// </summary>
        public float RSAFTE { get; set; }

        /// <summary>
        /// Total FTE that is available for project work that is not assigned this week
        /// </summary>
        public float UnassignedFTE { get; set; }

        /// <summary>
        /// The amount of FTE that represents allocation of staff to project work beyond their workload model limits
        /// </summary>
        public float OverallocationFTE { get; set; }

        /// <summary>
        /// The amount of FTE that represents underallocation of staff to project work based on their workload model
        /// </summary>
        public float UnderallocationFTE { get; set; }


        // Demand Data //

        /// <summary>
        /// Number of staff active in this week
        /// </summary>
        public int NumberOfStaff { get; set; }

        /// <summary>
        /// Essentially the number of active staff minus the number of staff the Head of RSE is managing
        /// </summary>
        public int NumberStaffRequiringLineManagement { get; set; }

        /// <summary>
        /// The number of projects that are confirmed
        /// </summary>
        public int NumberOfConfirmedProjects { get; set; }

        /// <summary>
        /// The number of projects that are unconfirmed
        /// </summary>
        public int NumberOfUnconfirmedProjects { get; set; }

        /// <summary>
        /// Demand for projects with the current week that is not being met by an assignment
        /// </summary>
        public float UnmetDemandFTE { get; set; }

        /// <summary>
        /// Total FTE in assignments within the current week
        /// </summary>
        public float MetDemandFTE { get; set; }

        /// <summary>
        /// The difference between the unassiged FTE and the unmet demand FTE
        /// (i.e. even if we were to somehow assign all free project work FTE, how much spare project FTE would we have)
        /// </summary>
        public float BenchProjectFTE { get; set; }

        /// <summary>
        /// The sum of the met and unmet demand this week
        /// </summary>
        public float TotalDemandFTE { get; set; }

        /// <summary>
        /// Amount of FTE of confirmed projects this week
        /// </summary>
        public float ConfirmedDemandFTE { get; set; }

        /// <summary>
        /// Amount of FTE of projects that are not yet confirmed this week
        /// </summary>
        public float UnconfirmedDemandFTE { get; set; }

        /// <summary>
        /// Amount of unmet demand on confirmed projects this week
        /// </summary>
        public float ConfirmedUnmetDemandFTE { get; set; }

        /// <summary>
        /// Amount of met demand on unconfirmed projects this week
        /// </summary>
        public float UnconfirmedMetDemandFTE { get; set; }

        /// <summary>
        /// Amount of met demand on confirmed projects this week
        /// </summary>
        public float ConfirmedMetDemandFTE { get; set; }

        /// <summary>
        /// Amount of unmet demand on unconfirmed projects this week
        /// </summary>
        public float UnconfirmedUnmetDemandFTE { get; set; }

        /// <summary>
        /// Amount of unmet demand on cancelled projects this week
        /// </summary>
        public float CancelledDemand { get; set; }

        /// <summary>
        /// Amount of met demand on finished projects this week
        /// </summary>
        public float FinishedMetDemand { get; set; }

        /// <summary>
        /// Amount of unmet demand on finished projects this week
        /// </summary>
        public float FinishedUnmetDemand { get; set; }

        /// <summary>
        /// Amount of demand for leadership tasks
        /// </summary>
        public float LeadershipDemand { get; set; }


        // Cost Data //

        /// <summary>
        /// What is the YTD value of the recovery target
        /// </summary>
        public float RecoveryTargetYTD { get; set; }

        /// <summary>
        /// What is the YTD value of the budgets of all the projects on the books
        /// </summary>
        public float BudgetYTD { get; set; }

        /// <summary>
        /// What is the YTD value of the planned costs of all the projects on the books
        /// </summary>
        public float PlannedCostYTD { get; set; }

        /// <summary>
        /// What is the YTD value of the actual costs of all the projects on the books
        /// </summary>
        public float ActualCostsYTD { get; set; }

        /// <summary>
        /// What is the YTD value of the received funds of all the projects on the books
        /// </summary>
        public float ReceivedFundsYTD { get; set; }

        /// <summary>
        /// What is the YTD value of the requested funds of all the projects on the books
        /// </summary>
        public float RequestedFundsYTD { get; set; }

        /// <summary>
        /// This is the weekly amount of staff costs that should be recoverable based on WLMs active that week and the grade of the person (assuming middle of the grade)
        /// </summary>
        public float RecoverableStaffCostsYTD { get; set; }
    }
}
