using System;

namespace PPMTool.Enums
{
    /// <summary>
    /// Converts an expiration DateTime to a value in days
    /// </summary>
    public class ExpirationInDaysAttribute : Attribute
    {
        public double Days { get; }

        public ExpirationInDaysAttribute(double days)
        {
            Days = days;
        }
    }
}
