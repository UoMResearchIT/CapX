using System;

namespace PPMTool.Data
{
    public class DemandChartItem
    {
        /// <summary>
        /// Start of the week to which this data corresponds
        /// </summary>
        public DateTime WeekStart { get; }

        /// <summary>
        /// Set automatically when the week is set and represents the period of the year as a string
        /// </summary>
        public string PeriodAsString { get; }


        // Workload Model //

        /// <summary>
        /// Number of staff active in this week
        /// </summary>
        public int NumberOfStaff { get; }

        /// <summary>
        /// FTE available to do project work
        /// </summary>
        public float ProjectFTE { get; }

        /// <summary>
        /// FTE available to do BAU
        /// </summary>
        public float BAUFTE { get; }

        /// <summary>
        /// FTE available to do Personal Development
        /// </summary>
        public float PersonalDevFTE { get; }

        /// <summary>
        /// FTE available to do PSM
        /// </summary>
        public float PSMFTE { get; }

        /// <summary>
        /// FTE available to do Line Management
        /// </summary>
        public float StaffManFTE { get; }

        /// <summary>
        /// FTE available to do RSA work
        /// </summary>
        public float RSAFTE { get; }

        /// <summary>
        /// Total amount of FTE assgined to projects
        /// </summary>
        public float AssignedFTE { get; }

        /// <summary>
        /// Total FTE that is available for project work that is not assigned this week
        /// </summary>
        public float UnassignedFTE { get; }


        // Demand Data //

        /// <summary>
        /// Essentially the number of active staff minus the number of staff the Head of RSE is managing
        /// </summary>
        public int NumberStaffRequiringLineManagement { get; }

        /// <summary>
        /// Demand for projects with the current week that is not being met by an assignment
        /// </summary>
        public float UnmetDemandFTE { get; }

        /// <summary>
        /// Total FTE in assignments within the current week
        /// </summary>
        public float MetDemandFTE { get; }

        /// <summary>
        /// The difference between the unassiged FTE and the unmet demand FTE
        /// (i.e. even if we were to somehow assign all free project work FTE, how much would we still be short by)
        /// </summary>
        public float TechnicalEffortShortfall { get; }

        /// <summary>
        /// The sum of the met and unmet demand this week
        /// </summary>
        public float TotalDemand { get; }

        /// <summary>
        /// Amount of FTE of confirmed projects this week
        /// </summary>
        public float ConfirmedProjectFTE { get; }

        /// <summary>
        /// Amount of FTE of projects that are not yet confirmed this week
        /// </summary>
        public float UnconfirmedProjectFTE { get; }

        /// <summary>
        /// Amount of unmet demand on confirmed projects this week
        /// </summary>
        public float ConfirmedUnallocatedProjectFTE { get; }

        /// <summary>
        /// Amount of met demand on unconfirmed projects this week
        /// </summary>
        public float UnconfirmedAllocatedProjectFTE { get; }

        /// <summary>
        /// Amount of met demand on confirmed projects this week
        /// </summary>
        public float ConfirmedAllocatedProjectFTE { get; }

        /// <summary>
        /// Amount of unmet demand on unconfirmed projects this week
        /// </summary>
        public float UnconfirmedUnllocatedProjectFTE { get; }


        // Cost Data //

        /// <summary>
        /// Based on the standard cost model what is the value of the confrimed projects
        /// </summary>
        public float ConfirmedValue { get; }

        /// <summary>
        /// Based on the standard cost model what is the value of the unconfrimed projects
        /// </summary>
        public float UnconfirmedValue { get; }

    }
}
