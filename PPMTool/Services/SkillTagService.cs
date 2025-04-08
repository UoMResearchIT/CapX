using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

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
        public int GetOwnedSkillCountForTag(PPMToolContext context, int skillTagId)
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
        public void UpdateSkillTagRareness(PPMToolContext context, SkillTag entity, bool commitChanges = true)
        {
            // Get owned skill count
            var count = GetOwnedSkillCountForTag(context, entity.SkillTagId);
            var totalActivePeople = context.People
                .Where(x => x.StartDate <= DateTime.Today && (x.EndDate == null || x.EndDate >= DateTime.Today))
                .Count();

            // Update the rarness of the skill tag
            entity.UpdateRareness(count, totalActivePeople);
            if (commitChanges)
            {
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Given a project ID, returns the unique list of skill tags aggregate from its subtasks
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public IEnumerable<SkillTag> GetSkillsForProject(PPMToolContext context, int projectId)
        {
            // Find all the subtasks for the project
            var subtasks = context.SubTasks
                .Include(x => x.OwningProject)
                .Where(x => x.OwningProject.ProjectId == projectId);

            var skills = new List<SkillTag>();
            foreach (var subtask in subtasks)
            {
                skills.AddRange(
                    context.SkillTags
                        .Include(x => x.TasksNeedingThisSkill)
                        .Where(x => x.TasksNeedingThisSkill
                            .Any(x => x.SubTaskId == subtask.SubTaskId)
                        )
                    );
            }

            return skills.DistinctBy(x => x.SkillTagId);
        }

        /// <summary>
        /// Given a subtask ID, returns the unique list of skill tags associated with it
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subtaskId"></param>
        /// <returns></returns>
        public IEnumerable<SkillTag> GetSkillsForSubTask(PPMToolContext context, int subtaskId)
        {
            return context.SkillTags
                .Include(x => x.TasksNeedingThisSkill)
                .Where(x => x.TasksNeedingThisSkill
                    .Any(x => x.SubTaskId == subtaskId)
                );
        }

        /// <summary>
        /// Returns the count of incomplete skills records for the given user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activeUserId"></param>
        /// <returns></returns>
        public int GetIncompleteRecordCount(PPMToolContext context, int activeUserId)
        {
            var recordsForUser = context.OwnedSkills
                .Include(x => x.Owner)
                .Where(x => x.Owner.PersonId == activeUserId)
                .ToList();
            return recordsForUser
                .Where(x => !x.RecordComplete())
                .Count();
        }
    }
}
