using System.ComponentModel.DataAnnotations;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    public class InnateCode : ILoggableClass, IEntity
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

        public int GetId()
        {
            return InnateCodeId;
        }

        public string GetSensibleObjectName()
        {
            return GetCodeAsString();
        }
    }
}
