using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// An abstract base class for timesheet codes or tasks
    /// </summary>
    public abstract class BaseBookableItem : ILoggableObject
    {
        /// <summary>
        /// Whether this item is active and can be booked to
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Pass the interface on to the concrete classes
        /// </summary>
        /// <returns></returns>
        public abstract string GetSensibleObjectName();
    }
}
