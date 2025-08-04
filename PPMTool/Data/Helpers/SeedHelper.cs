using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Data.Helpers
{
    public static class SeedHelper
    {
        /// <summary>
        /// Set up a minimum set of people
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void SeedPeople(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Get the default person in the DB and configure them
                var person = context.People.FirstOrDefault();
                if (person == null)
                {
                    person = new Person();
                }
                person.Name = "Mavis Ledger";
                person.ShortName = "ML";
                person.FTE = 1.0;
                person.StartDate = DateTime.Today.AddYears(-1);
                person.LineManager = person;
                context.People.Add(person);
                context.SaveChanges();

                // FTC not yet with us
                person = new Person();
                person.Name = "Clive Bugworthy";
                person.ShortName = "CB";
                person.FTE = 1.0;
                person.StartDate = DateTime.Today.AddMonths(3);
                person.EndDate = person.StartDate.AddYears(1);
                person.LineManager = context.People.First(x => x.ShortName == "NO");
                context.People.Add(person);
                context.SaveChanges();

                // FTC already left
                person = new Person();
                person.Name = "Janet Nullington";
                person.ShortName = "JN";
                person.FTE = 1.0;
                person.StartDate = DateTime.Today.AddYears(-1);
                person.EndDate = DateTime.Today.AddMonths(-3);
                person.LineManager = context.People.First(x => x.ShortName == "ML");
                context.People.Add(person);
                context.SaveChanges();

                // Perm currently with us
                person = new Person();
                person.Name = "Nigel Overfetch-Nelson";
                person.ShortName = "NO";
                person.FTE = 1.0;
                person.StartDate = DateTime.Today.AddMonths(-6);
                person.LineManager = context.People.First(x => x.ShortName == "ML");
                context.People.Add(person);
                context.SaveChanges();

                // Currently with us but leaving soon
                person = new Person();
                person.Name = "Tina Breakaway";
                person.ShortName = "TB";
                person.FTE = 1.0;
                person.StartDate = DateTime.Today.AddMonths(-6);
                person.EndDate = DateTime.Today.AddMonths(3);
                person.LineManager = context.People.First(x => x.ShortName == "NO");
                context.People.Add(person);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Set up some absences for the people
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void SeedAbsences(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Add an upcoming absence for Nigel Overfetch-Nelson
                var person = context.People.FirstOrDefault(x => x.ShortName == "NO");
                if (person != null)
                {
                    var absence = new Absence
                    {
                        Person = person,
                        StartDate = DateTime.Today.AddDays(1),
                        EndDate = DateTime.Today.AddDays(5)
                    };
                    context.Absence.Add(absence);
                    context.SaveChanges();
                }
                else
                {
                    throw new InvalidOperationException("Person with ShortName 'NO' not found.");
                }

                // Add a past absence for Mavis Ledger
                person = context.People.FirstOrDefault(x => x.ShortName == "ML");
                if (person != null)
                {
                    var pastAbsence = new Absence
                    {
                        Person = person,
                        StartDate = DateTime.Today.AddDays(-10),
                        EndDate = DateTime.Today.AddDays(-5)
                    };
                    context.Absence.Add(pastAbsence);
                    context.SaveChanges();
                }
                else
                {
                    throw new InvalidOperationException("Person with ShortName 'ML' not found.");
                }

                // Add an current absene for Clive Bugworthy
                person = context.People.FirstOrDefault(x => x.ShortName == "CB");
                if (person != null)
                {
                    var currentAbsence = new Absence
                    {
                        Person = person,
                        StartDate = DateTime.Today.AddDays(-2),
                        EndDate = DateTime.Today.AddDays(2)
                    };
                    context.Absence.Add(currentAbsence);
                    context.SaveChanges();
                }
                else
                {
                    throw new InvalidOperationException("Person with ShortName 'CB' not found.");
                }
            }
        }

        /// <summary>
        /// Add some user accounts at least one for each role
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void SeedUsers(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Super user

                // Manager

                // Developer

                // Reader

                // Finance

                // None (leaver)
            }
        }
    }
}
