// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class NoteService : BaseEntityService<Note>
    {
        private readonly HtmlContentSanitizerService htmlContentSanitizer;

        public NoteService(HtmlContentSanitizerService htmlContentSanitizer, ILogger<NoteService> logger) : base(logger)
        {
            this.htmlContentSanitizer = htmlContentSanitizer;
        }

        public override int Add(PPMToolContext context, Note entity, bool commitChanges = true)
        {
            SanitizeNoteHtml(entity);
            context.Notes.Add(entity);
            if (commitChanges) CommitChanges(context);
            return entity.NoteId;
        }

        public override void Delete(PPMToolContext context, Note entity, bool commitChanges = true)
        {
            context.Notes.Remove(entity);
            if (commitChanges) CommitChanges(context);
        }

        /// <summary>
        /// Returns all notes in the DB ordered with the most recently created first.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<Note> GetAll(PPMToolContext context)
        {
            return context.Notes
                .OrderByDescending(x => x.CreatedDate)
                .Include(x => x.Author)
                .Include(x => x.Project)
                .ThenInclude(x => x.Followers);
        }

        public override int Update(PPMToolContext context, Note entity, bool commitChanges = true)
        {
            SanitizeNoteHtml(entity);
            context.Notes.Update(entity);
            if (commitChanges) CommitChanges(context);
            return entity.NoteId;
        }

        internal IEnumerable<Note> GetDueNotesForProject(PPMToolContext context, int projectId)
        {
            return context.Notes.Where(x => x.Project.ProjectId == projectId);
        }

        /// <summary>
        /// Method to sanitise the HTML content in the model
        /// </summary>
        /// <param name="note"></param>
        private void SanitizeNoteHtml(Note note)
        {
            note.HtmlContent = htmlContentSanitizer.Sanitize(note.HtmlContent);
        }
    }
}
