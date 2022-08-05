using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PPMTool.Data;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class TagService
    {
        /// <summary>
        /// Returns all skill tags in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal IEnumerable<SkillTag> GetAllTags(PPMToolContext context)
        {
            return context.SkillTags;
        }

        /// <summary>
        /// Updates the tag in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        internal void Update(PPMToolContext context, SkillTag tag)
        {
            context.SkillTags.Update(tag);
            context.SaveChanges();
        }

        /// <summary>
        /// Gets the tracking entry for the tag
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        internal EntityEntry<SkillTag> GetEntry(PPMToolContext context, SkillTag tag)
        {
            return context.Entry(tag);
        }

        /// <summary>
        /// Deletes a tag from the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        internal void Delete(PPMToolContext context, SkillTag tag)
        {
            context.Remove(tag);
            context.SaveChanges();
        }

        /// <summary>
        /// Adds a new tag to the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        internal void Add(PPMToolContext context, SkillTag tag)
        {
            context.Add(tag);
            context.SaveChanges();
        }
    }
}
