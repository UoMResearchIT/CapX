using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class InnateCode : ILoggableClass
    {
        public int InnateCodeId { get; set; }

        [Required]
        public string ActivityCode { get; set; }

        [Required]
        public string ActivityName { get; set; }

        [Required]
        public string TaskName { get; set; }

        /// <summary>
        /// This is the category of work that this timesheet code is calssified as when doing WLM analysis
        /// </summary>
        [Required]
        public Duty Duty { get; set; }


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
