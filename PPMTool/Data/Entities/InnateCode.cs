using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public class InnateCode : ILoggableClass
    {
        public int InnateCodeId { get; set; }

        [Required]
        public string ActivityCode { get; set; }

        [Required]
        public string ActivityName { get; set; }

        /// <summary>
        /// The collection of innate code tasks that belong to this code
        /// </summary>
        public ICollection<InnateCodeTask> Tasks { get; set; } = new List<InnateCodeTask>();


        /// <summary>
        /// Joins the activity code and name together with a hyphen.
        /// </summary>
        /// <returns></returns>
        public string GetCodeAsString()
        {
            return $"{ActivityCode} - {ActivityName}";
        }

        public string GetSensibleObjectName()
        {
            return GetCodeAsString();
        }
    }
}
