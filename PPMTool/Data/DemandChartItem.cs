using System;

namespace PPMTool.Data
{
    public class DemandChartItem
    {
        /// <summary>
        /// Start of the week to which this data corresponds
        /// </summary>
        private DateTime weekStart;
        public DateTime WeekStart
        {
            get => weekStart;
            set
            {
                if (value != weekStart)
                {
                    weekStart = value;
                    Period = (int)Math.Ceiling(weekStart.Month / 3f);
                }
            }
        }

        /// <summary>
        /// Set automatically when the week is set and represents the period of the year
        /// </summary>
        public int? Period { get; private set; }


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


        // Cost Data //

        /// <summary>
        /// Based on the standard cost model what is the value of the confrimed projects
        /// </summary>
        public float ConfirmedValue { get; set; }

        /// <summary>
        /// Based on the standard cost model what is the value of the unconfrimed projects
        /// </summary>
        public float UnconfirmedValue { get; set; }

    }
}
