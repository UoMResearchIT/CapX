namespace PPMTool.Data.Interfaces
{
    /// <summary>
    /// A class that entities should extend where the log needs to show a particular property to make it useful to identify the entity
    /// </summary>
    public interface ILoggableObject
    {
        /// <summary>
        /// Method to obtian a sensible identifier of an object for the log records
        /// </summary>
        /// <returns></returns>
        public abstract string GetSensibleObjectName();
    }
}
