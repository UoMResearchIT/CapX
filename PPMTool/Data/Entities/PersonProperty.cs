namespace PPMTool.Data.Entities
{
    public abstract class PersonProperty : ILoggableClass
    {
        public Person Person { get; set; }

        public abstract string GetSensibleObjectName();
    }
}
