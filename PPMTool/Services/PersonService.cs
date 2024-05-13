using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class PersonService : BaseService<Person>
    {
        /// <summary>
        /// Adds a person to the DB.
        /// </summary>
        /// <param name="personModel"></param>
        /// <returns>False if an entry with the same name exists already.</returns>
        public override int Add(PPMToolContext context, Person personModel)
        {
            if (context.People.Any(p => p.Name.ToLower().Trim() == personModel.Name.ToLower().Trim()))
            {
                // Duplicate found
                return -1;
            }

            context.People.Add(personModel);
            context.SaveChanges();
            return personModel.PersonId;
        }

        /// <summary>
        /// Get all the people
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Person> GetAll(PPMToolContext context)
        {
            return context.People
                .Include(p => p.SkillTags)
                .Include(p => p.AvailabilityChanges)
                .Include(p => p.Absences)
                .ToList();
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
        /// <param name="person"></param>
        public override void Update(PPMToolContext context, Person person)
        {
            context.People.Update(person);
            context.SaveChanges();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Delete(PPMToolContext context, Person entity)
        {
            Debug.Write("** Delete Person not implemented!");
            throw new System.NotImplementedException();
        }
    }
}
