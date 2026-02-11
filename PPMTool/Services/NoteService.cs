// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class NoteService : BaseEntityService<Note>
    {
        public override int Add(PPMToolContext context, Note entity, bool commitChanges = true)
        {
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
            context.Notes.Update(entity);
            if (commitChanges) CommitChanges(context);
            return entity.NoteId;
        }

        internal IEnumerable<Note> GetDueNotesForProject(PPMToolContext context, int projectId)
        {
            return context.Notes.Where(x => x.Project.ProjectId == projectId);
        }
    }
}
