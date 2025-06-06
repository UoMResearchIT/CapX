using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class PersonService : BaseEntityService<Person>
    {
        /// <summary>
        /// Adds a person to the DB.
        /// </summary>
        /// <param name="personModel"></param>
        /// <returns>False if an entry with the same name exists already.</returns>
        public override int Add(PPMToolContext context, Person personModel, bool commitChanges = true)
        {
            if (DuplicateDetected(context, personModel))
            {
                // Duplicate found
                return -1;
            }
            if (DuplicateInitialsDetected(context, personModel))
            {
                // Duplicate found
                return -2;
            }

            context.People.Add(personModel);
            if (commitChanges) CommitChanges(context);
            return personModel.PersonId;
        }

        /// <summary>
        /// Duplicate determined by name
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool DuplicateDetected(PPMToolContext context, Person entity)
        {
            return context.People.Any(p => p.Name.ToLower().Trim() == entity.Name.ToLower().Trim() && p.PersonId != entity.PersonId);
        }

        /// <summary>
        /// Duplicate determined by initials
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool DuplicateInitialsDetected(PPMToolContext context, Person entity)
        {
            return context.People.Any(p => p.ShortName.ToLower().Trim() == entity.ShortName.ToLower().Trim() && p.PersonId != entity.PersonId);
        }

        /// <summary>
        /// Get all the people
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Person> GetAll(PPMToolContext context)
        {
            return context.People
                .Include(p => p.OwnedSkills)
                .ThenInclude(x => x.SkillTag)
                .Include(p => p.WorkloadModelChanges)
                .Include(p => p.Absences)
                .ToList();
        }

        /// <summary>
        /// Gets people table entities without any includes
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<Person> GetAllShallow(PPMToolContext context)
        {
            return context.People;
        }

        /// <summary>
        /// Get a person based on their ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="personId"></param>
        /// <returns></returns>
        internal Person GetById(PPMToolContext context, int personId)
        {
            return GetAll(context).FirstOrDefault(x => x.PersonId == personId);
        }

        /// <summary>
        /// Update an exist person in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="personModel"></param>
        public override int Update(PPMToolContext context, Person personModel, bool commitChanges = true)
        {
            if (DuplicateDetected(context, personModel))
            {
                // Duplicate found
                return -1;
            }
            if (DuplicateInitialsDetected(context, personModel))
            {
                // Duplicate found
                return -2;
            }
            context.People.Update(personModel);
            if (commitChanges) CommitChanges(context);
            return personModel.PersonId;
        }

        /// <summary>
        /// Deletes the person and everything associated with them including notes they have authored or edited.
        /// Maintains projects they owned but unsets the PM.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        public override void Delete(PPMToolContext context, Person entity, bool commitChanges = true)
        {
            // Set project manager on all projects owned by person to null
            foreach (var project in context.Projects.Where(x => x.ProjectManager.PersonId == entity.PersonId))
            {
                project.ProjectManager = null;
            }

            // Delete all the resources that have been created against this person
            context.Resources.RemoveRange(context.Resources.Where(x => x.Person.PersonId == entity.PersonId));

            // Delete all absences created against the person
            context.Absence.RemoveRange(context.Absence.Where(x => x.Person.PersonId == entity.PersonId));

            // Delete all workload models created against the person
            context.WorkloadModelChanges.RemoveRange(context.WorkloadModelChanges.Where(x => x.Person.PersonId == entity.PersonId));

            // Delete all users created against the person
            context.Users.RemoveRange(context.Users.Where(x => x.Person.PersonId == entity.PersonId));

            // Delete all notes created or edited by the person
            context.Notes.RemoveRange(context.Notes.Where(x => x.Author.Person.PersonId == entity.PersonId || x.Editor.Person.PersonId == entity.PersonId));

            // Remove entity from the owned skills
            context.OwnedSkills.RemoveRange(context.OwnedSkills.Where(x => x.Owner.PersonId == entity.PersonId));

            // Remove the person from the table
            context.People.Remove(entity);

            // Save changes as required
            if (commitChanges)
            {
                CommitChanges(context);
            }
        }

        /// <summary>
        /// Returns a list of absences in the DB for the people provided
        /// </summary>
        /// <param name="context"></param>
        /// <param name="people"></param>
        /// <returns></returns>
        internal IEnumerable<Absence> GetAbsencesForPeople(PPMToolContext context, IEnumerable<Person> people)
        {
            return context.Absence.Where(x => people.Select(x => x.PersonId).Contains(x.Person.PersonId));
        }

        /// <summary>
        /// Get a person based on their name
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Person GetByName(PPMToolContext context, string name)
        {
            return context.People.Where(x => x.Name == name).Include(x => x.WorkloadModelChanges).FirstOrDefault();
        }

        /// <summary>
        /// Gets all people managed by the supplied person
        /// </summary>
        /// <param name="context"></param>
        /// <param name="activeUser"></param>
        /// <returns></returns>
        internal IEnumerable<Person> GetManagedStaff(PPMToolContext context, Person activeUser)
        {
            return context.People.Where(x => activeUser == null ? false : x.LineManager.PersonId == activeUser.PersonId);
        }

        /// <summary>
        /// Gets all the workload model changes for the person
        /// </summary>
        /// <param name="context"></param>
        /// <param name="personId"></param>
        /// <returns></returns>
        internal IEnumerable<WorkloadModelChange> GetWorkloadModelChanges(PPMToolContext context, int personId)
        {
            return context.WorkloadModelChanges
                .Include(x => x.Person)
                .Where(x => x.Person.PersonId == personId);
        }
    }
}
