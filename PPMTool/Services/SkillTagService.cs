using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Services
{
    public class SkillTagService : BaseEntityService<SkillTag>
    {
        /// <summary>
        /// Returns all skill tags in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IEnumerable<SkillTag> GetAll(PPMToolContext context)
        {
            return context.SkillTags;
        }

        /// <summary>
        /// Updates the tag in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        public override int Update(PPMToolContext context, SkillTag tag, bool commitChanges = true)
        {
            if (DuplicateDetected(context, tag))
            {
                return -1;
            }
            context.SkillTags.Update(tag);
            if (commitChanges) context.SaveChanges();
            return tag.SkillTagId;
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
        public override void Delete(PPMToolContext context, SkillTag tag, bool commitChanges = true)
        {
            context.Remove(tag);
            if (commitChanges) context.SaveChanges();
        }

        /// <summary>
        /// Adds a new tag to the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tag"></param>
        public override int Add(PPMToolContext context, SkillTag tag, bool commitChanges = true)
        {
            if (DuplicateDetected(context, tag))
            {
                return -1;
            }
            context.Add(tag);
            if (commitChanges) context.SaveChanges();
            return tag.SkillTagId;
        }

        /// <summary>
        /// Detect a duplicate tag
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, SkillTag entity)
        {
            return GetAll(context).Any(x => (x.Name.Trim().ToLower() == entity.Name.Trim().ToLower() || x.ControlledName.Trim().ToLower() == entity.ControlledName.Trim().ToLower()) && x.SkillTagId != entity.SkillTagId);
        }

        /// <summary>
        /// Get the number of skills tags associated with a given person
        /// </summary>
        /// <param name="context"></param>
        /// <param name="personId"></param>
        /// <returns></returns>
        public int GetCountForPerson(PPMToolContext context, int personId)
        {
            return context.OwnedSkills.Where(x => x.Owner.PersonId == personId).Count();
        }

        /// <summary>
        /// Get the number of owned skills associated with a given tag
        /// </summary>
        /// <param name="context"></param>
        /// <param name="skillTagId"></param>
        /// <returns></returns>
        public int GetCountForTag(PPMToolContext context, int skillTagId)
        {
            return context.OwnedSkills.Where(x => x.SkillTag.SkillTagId == skillTagId).Count();
        }

        /// <summary>
        /// Delete all owned skills associated with a given tag
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        public void DeleteOwnedSkillsAssociatedWithTag(PPMToolContext context, SkillTag entity, bool commitChanges = true)
        {
            var ownedSkillsToRemove = context.OwnedSkills.Where(x => x.SkillTag.SkillTagId == entity.SkillTagId);
            context.OwnedSkills.RemoveRange(ownedSkillsToRemove);
            if (commitChanges) context.SaveChanges();
        }

        /// <summary>
        /// Get the rareness of the provided skill tag based on how many people own instances of it
        /// </summary>
        /// <param name="context"></param>
        /// <param name="skillTagId"></param>
        /// <returns></returns>
        public SkillTagRareness GetRareness(PPMToolContext context, int skillTagId)
        {
            var count = GetCountForTag(context, skillTagId);
            return new SkillTagRareness
            {
                Rareness = GetRareness(count),
                Count = count
            };
        }

        /// <summary>
        /// Represents the rareness information of a skill tag
        /// </summary>
        public class SkillTagRareness
        {
            public SkillRareness Rareness { get; set; }
            public int Count { get; set; }
        }

        /// <summary>
        /// Get the icon name for the emblem for the skill based on how many people have it
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private SkillRareness GetRareness(int count)
        {
            if (count < 3)
            {
                return SkillRareness.Legendary;
            }
            else if (count < 6)
            {
                return SkillRareness.Epic;
            }
            else if (count < 9)
            {
                return SkillRareness.Rare;
            }
            else if (count < 12)
            {
                return SkillRareness.Uncommon;
            }
            else
            {
                return SkillRareness.Common;
            }
        }
    }
}
