using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    public abstract class PersonProperty : ILoggableClass
    {
        public Person Person { get; set; }

        public string GetSensibleObjectName()
        {
            return $"Absence entry for {Person?.Name}";
        }
    }
}
