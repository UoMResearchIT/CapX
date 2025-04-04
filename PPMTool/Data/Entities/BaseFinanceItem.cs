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

        /// <summary>
        /// Method to get a suitable description of the item for posting to a note
        /// </summary>
        /// <returns></returns>
        public abstract string GetDescription();
    }
}
