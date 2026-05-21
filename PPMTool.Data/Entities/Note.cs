// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public class Note
    {
        public int NoteId { get; set; }

        [Required]
        public string HtmlContent { get; set; } = null!;

        [Required]
        public virtual User Author { get; set; } = null!;

        [Required]
        public virtual Project Project { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public DateTime EditedDate { get; set; }

        public virtual User? Editor { get; set; }

        public bool IsFinanceInfo { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public string? GetNoteEditorText()
        {
            return Editor != null ? $"Last edited by {Editor?.Name ?? "[User Not Found]"} on {EditedDate.ToString("dd/MM/yyyy HH:mm:ss")}" : null;
        }

        public string GetNoteAuthorText()
        {
            return $"{Author?.Name ?? "[User Not Found]"} posted on {CreatedDate.ToString("dd/MM/yyyy HH:mm:ss")}";
        }

        public bool IsCompleted()
        {
            return CompletedDate.HasValue;
        }

        public bool IsDue()
        {
            return DueDate.HasValue && DueDate.Value.AddDays(-7) <= DateTime.Now && !IsCompleted() && !IsOverDue();
        }

        public bool IsOverDue()
        {
            return DueDate.HasValue && DueDate.Value <= DateTime.Now && !IsCompleted();
        }
    }
}
