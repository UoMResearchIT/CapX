using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    public abstract class PersonProperty : ILoggableClass, IEntity
    {
        public Person Person { get; set; }

        public abstract int GetId();

        public string GetSensibleObjectName()
        {
            return $"Absence entry for {Person?.Name}";
        }
    }
}
