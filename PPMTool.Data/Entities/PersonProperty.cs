using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    public abstract class PersonProperty : ILoggableClass
    {
        public virtual Person Person { get; set; }

        public abstract string GetSensibleObjectName();
    }
}
