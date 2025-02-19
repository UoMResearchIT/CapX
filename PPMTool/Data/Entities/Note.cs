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
        public User Author { get; set; }

        [Required]
        public Project Project { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime EditedDate { get; set; }

        public User Editor { get; set; }

        public bool IsFinanceInfo { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        internal string GetNoteEditorText()
        {
            return Editor != null ? $"Last edited by {Editor?.Name ?? "[User Not Found]"} on {EditedDate.ToString("dd/MM/yyyy HH:mm:ss")}" : null;
        }

        internal string GetNoteAuthorText()
        {
            return $"{Author?.Name ?? "[User Not Found]"} posted on {CreatedDate.ToString("dd/MM/yyyy HH:mm:ss")}";
        }

        internal bool IsCompleted()
        {
            return CompletedDate.HasValue;
        }

        internal bool IsDue()
        {
            return DueDate.HasValue && DueDate.Value.AddDays(-7) <= DateTime.Now && !IsCompleted() && !IsOverDue();
        }

        internal bool IsOverDue()
        {
            return DueDate.HasValue && DueDate.Value <= DateTime.Now && !IsCompleted();
        }
    }
}
