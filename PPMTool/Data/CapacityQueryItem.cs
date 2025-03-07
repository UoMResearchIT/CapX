using System;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    /// <summary>
    /// Class to represent a window when someone has availability
    /// </summary>
    public class CapacityQueryItem
    {
        public Person Person { get; }

        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public double AvailabilityPercent { get; }

        public CapacityQueryItem(Person person, DateTime startDate, DateTime endDate, double availabilityPercent)
        {
            Person = person;
            StartDate = startDate;
            EndDate = endDate;
            AvailabilityPercent = availabilityPercent;
        }
    }
}
