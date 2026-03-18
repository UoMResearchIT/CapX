using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public class ApiKey
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int ApiKeyId { get; set; }

        /// <summary>
        /// The user who owns this API key
        /// </summary>
        [Required]
        public virtual User Owner { get; set; }

        /// <summary>
        /// The key itself
        /// </summary>
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// Description of the key
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Whether the key is active or expired
        /// </summary>
        [Required]
        public bool Active { get; set; } = true;

        /// <summary>
        /// When the key expires
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }
    }
}
