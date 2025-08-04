using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

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
                // Clear existing table
                context.People.ExecuteDelete();

                // Get the default person in the DB and configure them
                var person = new Person();
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
                // Clear existing absences
                context.Absence.ExecuteDelete();

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
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Clear existing users
                context.Users.ExecuteDelete();

                // Super user -- attached to no-one
                var superUser = new User
                {
                    Name = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserName"),
                    CASUserName = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserUserName"),
                    EmailAddress = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserEmail"),
                    RoleType = RoleType.Superuser,
                    Person = null // No person associated as assumed not a team member
                };
                context.Users.Add(superUser);
                context.SaveChanges();

                // Manager - Mavis and Nigel are managers
                var manager = new User
                {
                    Name = "Mavis Ledger",
                    CASUserName = "mledger",
                    EmailAddress = "",
                    RoleType = RoleType.Manager,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "ML")
                };
                context.Users.Add(manager);
                context.SaveChanges();

                manager = new User
                {
                    Name = "Nigel Overfetch-Nelson",
                    CASUserName = "noverfetchnelson",
                    EmailAddress = "",
                    RoleType = RoleType.Manager,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "NO")
                };
                context.Users.Add(manager);
                context.SaveChanges();

                // Developer -- Clive and Tina are developers
                var developer = new User
                {
                    Name = "Clive Bugworthy",
                    CASUserName = "cbugworthy",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "CB")
                };
                context.Users.Add(developer);
                context.SaveChanges();

                developer = new User
                {
                    Name = "Tina Breakaway",
                    CASUserName = "tbreakaway",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "TB")
                };
                context.Users.Add(developer);
                context.SaveChanges();

                // Reader -- Sue is an admin and not in the team
                var reader = new User
                {
                    Name = "Sue Permann",
                    CASUserName = "spermann",
                    EmailAddress = "",
                    RoleType = RoleType.Reader,
                    Person = null // No person associated as assumed not a team member
                };
                context.Users.Add(reader);
                context.SaveChanges();

                // Finance - Penny is a finance officer and not in the team
                var finance = new User
                {
                    Name = "Penny Pincher",
                    CASUserName = "ppincher",
                    EmailAddress = "",
                    RoleType = RoleType.Finance,
                    Person = null // No person associated as assumed not a team member
                };
                context.Users.Add(finance);
                context.SaveChanges();

                // None (leaver) -- Janet has left
                var none = new User
                {
                    Name = "Janet Nullington",
                    CASUserName = "jnullington",
                    EmailAddress = "",
                    RoleType = RoleType.None,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "JN")
                };
                context.Users.Add(none);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed some workload model changes for the people
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedWorkloadModelChanges(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Clear existing
                context.WorkloadModelChanges.ExecuteDelete();

                // Add a G7 WLM for Mavis Ledger
                var ml = context.People.FirstOrDefault(x => x.ShortName == "ML");
                var wlm = new WorkloadModelChange
                {
                    Person = ml,
                    ChangeDate = ml.StartDate,
                    Grade = 7,
                    ArchitectureFTE = 0,
                    StaffManagementFTE = 0.2,
                    ProjectManagementFTE = 0.2,
                    ServiceManagementFTE = 0.1,
                    ProjectAndServiceManagementFTE = 0.3, // Total of previous two
                    BusinessAsUsualFTE = 0.1,
                    PersonalDevelopmentFTE = 0,
                    ProjectWorkFTE = 0.4,
                    Notes = "Standard G7 WLM"
                };
                context.WorkloadModelChanges.Add(wlm);
                context.SaveChanges();

                // Add a G6 WLM for Nigel Overfetch-Nelson
                var no = context.People.FirstOrDefault(x => x.ShortName == "NO");
                wlm = new WorkloadModelChange
                {
                    Person = no,
                    ChangeDate = no.StartDate,
                    Grade = 6,
                    BusinessAsUsualFTE = 0.1,
                    PersonalDevelopmentFTE = 0.1,
                    ProjectWorkFTE = 0.8,
                    Notes = "Standard G6 WLM"
                };

                // Promotion for Nigel Overfetch-Nelson
                var newWlm = new WorkloadModelChange
                {
                    Person = no,
                    ChangeDate = no.StartDate.AddMonths(4),
                    Grade = 7,
                    BusinessAsUsualFTE = 0.1,
                    ProjectManagementFTE = 0.4,
                    ArchitectureFTE = 0.1,
                    ProjectWorkFTE = 0.4,
                    Notes = "New G7 WLM"
                };
            }
        }

        /// <summary>
        /// Seed random owned skills for each person in the database.
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedOwnedSkillsForPeople(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Clear existing
                context.OwnedSkills.ExecuteDelete();

                // For each person, add some random owned skills
                var random = new Random();
                foreach (var person in context.People)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        // Randomly choose a proficiency rating
                        var proficiencyRating = random.Next(Enum.GetValues<SkillProficiency>().Count());
                        var ownedSkill = new OwnedSkill
                        {
                            Owner = person,
                            SkillTag = context.SkillTags.ElementAt(random.Next(context.SkillTags.Count())),
                            ProficiencyRating = proficiencyRating,
                            Proficiency = (SkillProficiency)proficiencyRating,
                            LastUsed = proficiencyRating > 0 ? DateTime.Today.AddDays(-random.Next(1, 365)) : default,
                            FavouriteSkill = proficiencyRating == 0
                        };

                        // Add the owned skill to the context
                        context.OwnedSkills.Add(ownedSkill);
                    }
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Seed random competency assessments for each person in the database for half the competencies.
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedCompetencyAssessments(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Clear existing
                context.CompetencyAssessments.ExecuteDelete();

                // Get the count of competencies
                var competencyCount = context.Competencies.Count();

                // For each person, add assessments for half of the competencies
                var random = new Random();
                foreach (var person in context.People)
                {
                    for (int i = 0; i < competencyCount / 2; ++i)
                    {
                        // Get a random competency
                        var competency = context.Competencies.ElementAt(random.Next(competencyCount));

                        // Create a new competency assessment
                        var assessment = new CompetencyAssessment
                        {
                            PersonId = person.PersonId,
                            CompetencyId = competency.CompetencyId,
                            CompetencyDescription = competency.Description,
                            CompetencyObjective = competency.Objective,
                            CompetencyRevision = competency.Revision,
                            DateCreated = DateTime.Now.AddDays(-random.Next(0, 365)).ToString("R"),
                            Status = (AssessmentStatus)random.Next(Enum.GetValues<AssessmentStatus>().Length),
                            Evidence = "<p>Randomly generated evidence for competency assessment.</p>"
                        };

                        // Add the assessment to the context
                        context.CompetencyAssessments.Add(assessment);
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
