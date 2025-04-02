using System;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    /// <summary>
    /// Class to represent a window when someone has availability
    /// </summary>
    public class CapacityQueryItem
    {
        /// <summary>
        /// Person associated with this query result
        /// </summary>
        public Person Person { get; }

        /// <summary>
        /// Date from which they have availability
        /// </summary>
        public DateTime StartDate { get; }

        /// <summary>
        /// Date to which they have availability
        /// </summary>
        public DateTime EndDate { get; }

        /// <summary>
        /// What FTE are they available during the period
        /// </summary>
        public double AvailableFTE { get; }

        /// <summary>
        /// Whether this query item is a match to the requested query FTE
        /// </summary>
        public bool FteMatch { get; }

        /// <summary>
        /// Whether this query item is a match to the requested query duration
        /// </summary>
        public bool DurationMatch { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="person"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="availableFTE"></param>
        public CapacityQueryItem(
            Person person,
            DateTime startDate,
            DateTime endDate,
            double availableFTE,
            DateTime queryStart,
            DateTime queryEnd,
            double queryFTE)
        {
            Person = person;
            StartDate = startDate;
            EndDate = endDate;
            AvailableFTE = availableFTE;

            // Set flags
            FteMatch = availableFTE == queryFTE;
            DurationMatch = startDate.Date == queryStart.Date && endDate.Date == queryEnd.Date;
        }
    }
}
