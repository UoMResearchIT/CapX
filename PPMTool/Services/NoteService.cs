using System.Collections;
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

        public override IEnumerable GetAll(PPMToolContext context)
        {
            return context.Notes
                .Include(x => x.Author);
        }

        public override void Update(PPMToolContext context, Note entity)
        {
            context.Notes.Update(entity);
            context.SaveChanges();
        }
    }
}
