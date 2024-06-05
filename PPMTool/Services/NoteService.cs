using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class NoteService : BaseService<Note>
    {
        public override int Add(PPMToolContext context, Note entity)
        {
            context.Notes.Add(entity);
            context.SaveChanges();
            return entity.NoteId;
        }

        public override void Delete(PPMToolContext context, Note entity)
        {
            context.Notes.Remove(entity);
            context.SaveChanges();
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
                .Include(x => x.Author);
        }

        public override int Update(PPMToolContext context, Note entity)
        {
            context.Notes.Update(entity);
            context.SaveChanges();
            return entity.NoteId;
        }
    }
}
