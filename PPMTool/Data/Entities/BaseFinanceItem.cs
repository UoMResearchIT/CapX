using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public abstract class BaseFinanceItem
    {
        /// <summary>
        /// Project associated with this item
        /// </summary>
        [Required]
        public Project Project { get; set; }
    }
}
