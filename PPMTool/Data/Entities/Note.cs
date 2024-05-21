using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public class Note
    {
        public int NoteId { get; set; }

        [Required]
        public string HtmlContent { get; set; }

        [Required]
        public Person Author { get; set; }

        [Required]
        public Project Project { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime EditedDate { get; set; }

        public Person Editor { get; set; }
    }
}
