using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PPMTool.Data;

namespace PPMTool.Services
{
    public class PersonService
    {
        /// <summary>
        /// Adds a person to the DB.
        /// </summary>
        /// <param name="personModel"></param>
        /// <returns>False if an entry with the same name exists already.</returns>
        internal bool AddPerson(Person personModel)
        {
            using var context = new PPMToolContext();
            if (context.People.Any(p=> p.Name.ToLower().Trim() == personModel.Name.ToLower().Trim()))
            {
                // Duplicate found
                return false;
            }

            context.People.Add(personModel);
            context.SaveChanges();
            return true;
        }
    }
}
