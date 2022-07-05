using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    public class PersonService
    {
        /// <summary>
        /// Adds a person to the DB.
        /// </summary>
        /// <param name="personModel"></param>
        /// <returns>False if an entry with the same name exists already.</returns>
        internal bool AddPerson(PPMToolContext context, Person personModel)
        {
            if (context.People.Any(p=> p.Name.ToLower().Trim() == personModel.Name.ToLower().Trim()))
            {
                // Duplicate found
                return false;
            }

            context.People.Add(personModel);
            context.SaveChanges();
            return true;
        }

        /// <summary>
        /// Get all the people
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Person> GetAll(PPMToolContext context)
        {
            return context.People
                .Include(p => p.SkillTags)
                .ToList();
        }
        /// <summary>
        /// Update an exist person in the DB
        /// </summary>
        /// <param name="context"></param>
        /// <param name="person"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void Update(PPMToolContext context, Person person)
        {
            context.People.Update(person);
            context.SaveChanges();
        }
    }
}
