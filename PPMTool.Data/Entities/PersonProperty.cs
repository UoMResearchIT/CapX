using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    public abstract class PersonProperty : ILoggableObject
    {
        [Required]
        public virtual Person Person { get; set; } = null!;

        public abstract string GetSensibleObjectName();
    }
}
