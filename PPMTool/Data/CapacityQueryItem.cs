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

        public int AvailabilityPercent { get; }

        public CapacityQueryItem(Person person, DateTime startDate, DateTime endDate, int availabilityPercent)
        {
            Person = person;
            StartDate = startDate;
            EndDate = endDate;
            AvailabilityPercent = availabilityPercent;
        }
    }
}
