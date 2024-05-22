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

        internal string GetNoteEditorText()
        {
            return Editor != null ? $"Last edited by {Editor.Name} on {EditedDate.ToString("dd/MM/yyyy HH:mm:ss")}" : null;
        }

        internal string GetNoteAuthorText()
        {
            return $"{Author.Name} posted on {CreatedDate.ToString("dd/MM/yyyy HH:mm:ss")}";
        }
    }
}
