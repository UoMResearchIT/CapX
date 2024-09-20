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
