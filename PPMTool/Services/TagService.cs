using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    }
}
