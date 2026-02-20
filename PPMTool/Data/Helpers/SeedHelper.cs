// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System.Linq.Dynamic.Core;
using System.Text;
using System.Text.RegularExpressions;
using LoremNET;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data.Helpers
{
    public static class SeedHelper
    {
        /// <summary>
        /// This is the date that was assumed when the data was created and hence is used to offset the dates
        /// </summary>
        private static readonly DateTime dateAnchor = new DateTime(2025, 4, 1);

        /// <summary>
        /// Creates the dummy data based on the anchor date being used to "shift" the hardcoded dates in each method
        /// </summary>
        /// <param name="y">Year value</param>
        /// <param name="m">Month value</param>
        /// <param name="d">Day value</param>
        /// <param name="hours">Hours value</param>
        /// <param name="minutes">Minutes value</param>
        /// <param name="seconds">Seconds value</param>
        /// <returns>
        /// A DateTime object adjusted to keep its value with respect to
        /// the anchor date the same but with respect to today's date instead
        /// </returns>
        private static DateTime ApplyDateOffset(int y, int m, int d, int hours = 0, int minutes = 0, int seconds = 0)
        {
            var differenceToAnchor = DateTime.Today - dateAnchor;
            return new DateTime(y, m, d, hours, minutes, seconds).Add(differenceToAnchor);
        }

        /// <summary>
        /// Set up a minimum set of people
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void SeedPeople(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding people...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Perm currently with us
                var person = new Person();
                person.Name = "Mavis Ledger";
                person.ShortName = "ML";
                person.FTE = 1.0;
                person.StartDate = ApplyDateOffset(2023, 7, 1);    // Date required by first project
                context.People.Add(person);
                context.SaveChanges();

                // First person manages self so add after save
                person.LineManager = person;
                context.SaveChanges();

                // Perm currently with us
                person = new Person();
                person.Name = "Nigel Overfetch-Nelson";
                person.ShortName = "NO";
                person.FTE = 1.0;
                person.StartDate = DateTime.Today.AddMonths(-12);
                person.LineManager = context.People.First(x => x.ShortName == "ML");
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

                // FTC already left
                person = new Person();
                person.Name = "Janet Nullington";
                person.ShortName = "JN";
                person.FTE = 1.0;
                person.StartDate = DateTime.Today.AddYears(-1);
                person.EndDate = DateTime.Today.AddMonths(-3);
                person.LineManager = context.People.First(x => x.ShortName == "ML");
                context.People.Add(person);

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

                // Ones needed for project import
                var others = new List<Person>
                {
                    new Person
                    {
                        EndDate = null,
                        FTE = 1.0,
                        LineManager = context.People.FirstOrDefault(x => x.ShortName == "ML"),
                        Name = "Bingo McTrousers",
                        ShortName = "BM",
                        StartDate = ApplyDateOffset(2023, 07, 01)
                    },
                    new Person
                    {
                        EndDate = null,
                        FTE = 1.0,
                        LineManager = context.People.FirstOrDefault(x => x.ShortName == "ML"),
                        Name = "Ankle Goblin",
                        ShortName = "AG",
                        StartDate = ApplyDateOffset(2023, 07, 01)
                    },
                    new Person
                    {
                        EndDate = null,
                        FTE = 1.0,
                        LineManager = context.People.FirstOrDefault(x => x.ShortName == "ML"),
                        Name = "Gravy Commander",
                        ShortName = "GC",
                        StartDate = ApplyDateOffset(2023, 07, 01)
                    },
                    new Person
                    {
                        EndDate = null,
                        FTE = 1.0,
                        LineManager = context.People.FirstOrDefault(x => x.ShortName == "ML"),
                        Name = "Lemon Lasso",
                        ShortName = "LL",
                        StartDate = ApplyDateOffset(2023, 07, 01)
                    },
                    new Person
                    {
                        EndDate = null,
                        FTE = 1.0,
                        LineManager = context.People.FirstOrDefault(x => x.ShortName == "ML"),
                        Name = "Soggy Apple Nibbler",
                        ShortName = "SAN",
                        StartDate = ApplyDateOffset(2023, 07, 01)
                    },
                    new Person
                    {
                        EndDate = null,
                        FTE = 1.0,
                        LineManager = context.People.FirstOrDefault(x => x.ShortName == "NO"),
                        Name = "Eggplant Acrobat",
                        ShortName = "EA",
                        StartDate = ApplyDateOffset(2023, 09, 01)
                    },
                    new Person
                    {
                        EndDate = null,
                        FTE = 1.0,
                        LineManager = context.People.FirstOrDefault(x => x.ShortName == "NO"),
                        Name = "Cheddar Swoosh",
                        ShortName = "CS",
                        StartDate = ApplyDateOffset(2024, 08, 20)
                    },
                    new Person
                    {
                        EndDate = ApplyDateOffset(2025, 09, 17),
                        FTE = 1.0,
                        LineManager = context.People.FirstOrDefault(x => x.ShortName == "NO"),
                        Name = "Lumpy Sprinkles",
                        ShortName = "LS",
                        StartDate = ApplyDateOffset(2024, 09, 18)
                    }
                };
                context.People.AddRange(others);
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
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding absences...");
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
                }
                else
                {
                    throw new InvalidOperationException("Person with ShortName 'ML' not found.");
                }

                // Add a current absence for Tina Breakaway
                person = context.People.FirstOrDefault(x => x.ShortName == "TB");
                if (person != null)
                {
                    var currentAbsence = new Absence
                    {
                        Person = person,
                        StartDate = DateTime.Today.AddDays(-2),
                        EndDate = DateTime.Today.AddDays(2)
                    };
                    context.Absence.Add(currentAbsence);
                }
                else
                {
                    throw new InvalidOperationException("Person with ShortName 'CB' not found.");
                }

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Add a superuser with the default values from the configuration if there isn't one
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void SeedSuperUserIfNotExist(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Create a new superuser if there isn't one
                var superUser = context.Users.Include(x => x.Person).FirstOrDefault(x => x.RoleType == RoleType.Superuser);
                if (superUser == null)
                {
                    logger.LogWarning("No superuser found! Adding default...");
                    superUser = new User
                    {
                        RoleType = RoleType.Superuser,
                        Name = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserName"),
                        CASUserName = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserUserName"),
                        EmailAddress = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserEmail"),
                        Person = null
                    };
                    context.Users.Add(superUser);
                    context.SaveChanges();
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
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding users...");
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Update super user if there is one to match the configuration defaults
                var superUser = context.Users.Include(x => x.Person).FirstOrDefault(x => x.RoleType == RoleType.Superuser);
                if (superUser != null)
                {
                    superUser.Name = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserName");
                    superUser.CASUserName = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserUserName");
                    superUser.EmailAddress = configuration.GetValue<string>("DeveloperSettings:DefaultSuperUserEmail");
                    superUser.Person = null;
                    context.SaveChanges();
                }

                // Manager -- Mavis and Nigel are managers
                var manager = new User
                {
                    Name = "Mavis Ledger",
                    CASUserName = "mledger",
                    RoleType = RoleType.Manager,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "ML")
                };
                context.Users.Add(manager);

                manager = new User
                {
                    Name = "Nigel Overfetch-Nelson",
                    CASUserName = "noverfetchnelson",
                    EmailAddress = "",
                    RoleType = RoleType.Manager,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "NO")
                };
                context.Users.Add(manager);

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

                developer = new User
                {
                    Name = "Tina Breakaway",
                    CASUserName = "tbreakaway",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "TB")
                };
                context.Users.Add(developer);

                developer = new User
                {
                    Name = "Ankle Goblin",
                    CASUserName = "agoblin",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "AG")
                };
                context.Users.Add(developer);

                developer = new User
                {
                    Name = "Bingo McTrousers",
                    CASUserName = "bmctrou",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "BM")
                };
                context.Users.Add(developer);

                developer = new User
                {
                    Name = "Cheddar Swoosh",
                    CASUserName = "cswoosh",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "CS")
                };
                context.Users.Add(developer);

                developer = new User
                {
                    Name = "Eggplant Acrobat",
                    CASUserName = "eacrobat",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "EA")
                };
                context.Users.Add(developer);

                developer = new User
                {
                    Name = "Gravy Commander",
                    CASUserName = "gcomm",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "GC")
                };
                context.Users.Add(developer);

                developer = new User
                {
                    Name = "Lumpy Sprinkles",
                    CASUserName = "lspring",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "LS")
                };
                context.Users.Add(developer);

                developer = new User
                {
                    Name = "Lemon Lasso",
                    CASUserName = "llasso",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "LL")
                };
                context.Users.Add(developer);

                developer = new User
                {
                    Name = "Soggy Apple Nibbler",
                    CASUserName = "sanibb",
                    EmailAddress = "",
                    RoleType = RoleType.Developer,
                    Person = context.People.FirstOrDefault(x => x.ShortName == "SAN")
                };
                context.Users.Add(developer);

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
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding WLMs...");
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
                context.WorkloadModelChanges.Add(wlm);

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
                context.WorkloadModelChanges.Add(newWlm);

                var otherWLMs = new List<WorkloadModelChange>
                {
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Non-billable BAU & Personal Dev",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2023, 10, 1),
                        Person = context.People.First(x => x.ShortName == "EA"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.8,
                        StaffManagementFTE = 0.0,
                        Grade = 6,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Part-time 0.8; 0.1 PD",
                        BusinessAsUsualFTE = 0.0,
                        ChangeDate = ApplyDateOffset(2023, 10, 1),
                        Person = context.People.First(x => x.ShortName == "SAN"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.7,
                        StaffManagementFTE = 0.0,
                        Grade = 6,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "PSM 0.3 FTE & Staff 0.4 FTE",
                        BusinessAsUsualFTE = 0.0,
                        ChangeDate = ApplyDateOffset(2023, 10, 1),
                        Person = context.People.First(x => x.ShortName == "AG"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.3,
                        ProjectWorkFTE = 0.3,
                        StaffManagementFTE = 0.4,
                        Grade = 7,
                        ProjectManagementFTE = 0.3,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.4,
                        Notes = "PSM 0.3 FTE, Staff 0.2 FTE & RSA (Web) 0.4 FTE",
                        BusinessAsUsualFTE = 0.0,
                        ChangeDate = ApplyDateOffset(2023, 10, 1),
                        Person = context.People.First(x => x.ShortName == "LL"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.3,
                        ProjectWorkFTE = 0.1,
                        StaffManagementFTE = 0.2,
                        Grade = 7,
                        ProjectManagementFTE = 0.1,
                        ServiceManagementFTE = 0.2
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Part-time 0.8; Personal Dev 0.1 & Training 0.1",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2023, 10, 1),
                        Person = context.People.First(x => x.ShortName == "GC"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.6,
                        StaffManagementFTE = 0.0,
                        Grade = 6,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "PSM 0.2 FTE & Staff 0.3 FTE, BAU 0.1 FTE",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2023, 10, 24),
                        Person = context.People.First(x => x.ShortName == "AG"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.2,
                        ProjectWorkFTE = 0.4,
                        StaffManagementFTE = 0.3,
                        Grade = 7,
                        ProjectManagementFTE = 0.2,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.4,
                        Notes = "PSM 0.2 FTE & RSA (Web) 0.4 FTE, BAU 0.1 FTE",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2024, 1, 15),
                        Person = context.People.First(x => x.ShortName == "LL"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.2,
                        ProjectWorkFTE = 0.3,
                        StaffManagementFTE = 0.0,
                        Grade = 7,
                        ProjectManagementFTE = 0.1,
                        ServiceManagementFTE = 0.1
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "0.1 FTE BAU & 0.1 PD",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2024, 9, 2),
                        Person = context.People.First(x => x.ShortName == "BM"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.8,
                        StaffManagementFTE = 0.0,
                        Grade = 5,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Part-time 0.8; 0.3 PSM, 0.1 Staff, 0.1 BAU",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2024, 4, 1),
                        Person = context.People.First(x => x.ShortName == "SAN"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.3,
                        ProjectWorkFTE = 0.3,
                        StaffManagementFTE = 0.1,
                        Grade = 7,
                        ProjectManagementFTE = 0.2,
                        ServiceManagementFTE = 0.1
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.4,
                        Notes = "BAU 0.1, RSA 0.4 (Django-Wagtail)",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2024, 3, 18),
                        Person = context.People.First(x => x.ShortName == "LL"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.5,
                        StaffManagementFTE = 0.0,
                        Grade = 7,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.1,
                        Notes = "Part-time 0.8; 0.1 PSM, 0.2 Staff, 0.1 RSA (R Shiny Service), 0.1 BAU",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2024, 5, 13),
                        Person = context.People.First(x => x.ShortName == "SAN"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.1,
                        ProjectWorkFTE = 0.3,
                        StaffManagementFTE = 0.2,
                        Grade = 7,
                        ProjectManagementFTE = 0.1,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Standard G6 WLM",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2024, 8, 20),
                        Person = context.People.First(x => x.ShortName == "CS"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.8,
                        StaffManagementFTE = 0.0,
                        Grade = 6,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Standard G6 WLM",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2024, 9, 18),
                        Person = context.People.First(x => x.ShortName == "LS"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.8,
                        StaffManagementFTE = 0.0,
                        Grade = 6,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Extra BAU time for work on DjW stack upgrading",
                        BusinessAsUsualFTE = 0.5,
                        ChangeDate = ApplyDateOffset(2024, 12, 9),
                        Person = context.People.First(x => x.ShortName == "BM"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.4,
                        StaffManagementFTE = 0.0,
                        Grade = 5,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Back to normal G5 WLM, for RTP-311",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2025, 1, 13),
                        Person = context.People.First(x => x.ShortName == "BM"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.8,
                        StaffManagementFTE = 0.0,
                        Grade = 5,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Part-time 0.8; 0.1 PSM, 0.2 Staff, 0.1 BAU",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2025, 1, 20),
                        Person = context.People.First(x => x.ShortName == "SAN"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.2,
                        ProjectWorkFTE = 0.3,
                        StaffManagementFTE = 0.2,
                        Grade = 7,
                        ProjectManagementFTE = 0.2,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Extra BAU time for work on DjW stack upgrading",
                        BusinessAsUsualFTE = 0.5,
                        ChangeDate = ApplyDateOffset(2025, 3, 21),
                        Person = context.People.First(x => x.ShortName == "BM"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.4,
                        StaffManagementFTE = 0.0,
                        Grade = 5,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Back to normal G5",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2025, 5, 7),
                        Person = context.People.First(x => x.ShortName == "BM"),
                        PersonalDevelopmentFTE = 0.1,
                        ProjectAndServiceManagementFTE = 0.0,
                        ProjectWorkFTE = 0.8,
                        StaffManagementFTE = 0.0,
                        Grade = 5,
                        ProjectManagementFTE = 0.0,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Part time 0.9 (for 3 months at present); 0.2 PSM, 0.2 Staff, 0.1 BAU",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2025, 6, 1),
                        Person = context.People.First(x => x.ShortName == "SAN"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.2,
                        ProjectWorkFTE = 0.4,
                        StaffManagementFTE = 0.2,
                        Grade = 7,
                        ProjectManagementFTE = 0.2,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Part-time 0.8; 0.2 PSM, 0.2 Staff, 0.1 BAU",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2025, 9, 1),
                        Person = context.People.First(x => x.ShortName == "SAN"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.2,
                        ProjectWorkFTE = 0.3,
                        StaffManagementFTE = 0.2,
                        Grade = 7,
                        ProjectManagementFTE = 0.2,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "Agile course - 0.5 BAU, 0.2 PSM, 0.3 Staff",
                        BusinessAsUsualFTE = 0.5,
                        ChangeDate = ApplyDateOffset(2025, 4, 26),
                        Person = context.People.First(x => x.ShortName == "AG"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.2,
                        ProjectWorkFTE = 0.0,
                        StaffManagementFTE = 0.3,
                        Grade = 7,
                        ProjectManagementFTE = 0.2,
                        ServiceManagementFTE = 0.0
                    },
                    new WorkloadModelChange
                    {
                        ArchitectureFTE = 0.0,
                        Notes = "PSM 0.2 FTE & Staff 0.3 FTE, BAU 0.1 FTE",
                        BusinessAsUsualFTE = 0.1,
                        ChangeDate = ApplyDateOffset(2025, 5, 3),
                        Person = context.People.First(x => x.ShortName == "AG"),
                        PersonalDevelopmentFTE = 0.0,
                        ProjectAndServiceManagementFTE = 0.2,
                        ProjectWorkFTE = 0.4,
                        StaffManagementFTE = 0.3,
                        Grade = 7,
                        ProjectManagementFTE = 0.2,
                        ServiceManagementFTE = 0.0
                    }
                };
                context.WorkloadModelChanges.AddRange(otherWLMs);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed random owned skills for each person in the database.
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedOwnedSkillsForPeople(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding owned skills...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Clear existing
                context.OwnedSkills.ExecuteDelete();

                // For each person, add some random owned skills
                var random = new Random();
                foreach (var person in context.People)
                {
                    var skillIdsOwned = new List<int>();
                    for (int i = 0; i < 5; ++i)
                    {
                        // Randomly choose a proficiency rating
                        var proficiencyRating = random.Next(Enum.GetValues<SkillProficiency>().Count());

                        // Get random skill tag that is not already owned by this person
                        var skillTag = context.SkillTags.ElementAt(random.Next(context.SkillTags.Count()));
                        while (skillIdsOwned.Contains(skillTag.SkillTagId))
                        {
                            skillTag = context.SkillTags.ElementAt(random.Next(context.SkillTags.Count()));
                        }

                        // Add the skill tag to the list of owned skills
                        var ownedSkill = new OwnedSkill
                        {
                            Owner = person,
                            SkillTag = skillTag,
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

                // Update the rareness of the owned skills
                var totalActivePeople = context.People
                    .Where(x => x.StartDate <= DateTime.Today && (x.EndDate == null || x.EndDate >= DateTime.Today))
                    .Count();
                foreach (var skillTag in context.SkillTags)
                {
                    var totalInstances = context.OwnedSkills.Include(x => x.SkillTag).Where(x => x.SkillTag.SkillTagId == skillTag.SkillTagId).Count();
                    skillTag.UpdateRareness(totalInstances, totalActivePeople);
                }

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed random competency assessments for each person in the database for half the competencies.
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedCompetencyAssessments(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding competency assessments...");
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

        /// <summary>
        /// Cleans up the innate codes and tasks in the database.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal static void SeedInnateCodesAndTasks(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding timesheet codes...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Remove a certain number of the existing InnateCodes
                context.InnateCodes.Where(x =>
                    x.ActivityName.StartsWith("P0") ||
                    x.ActivityName.StartsWith("S-RES-P")
                )
                .ExecuteDelete();

                // Check there are some left
                if (context.InnateCodes.Count() == 0)
                {
                    throw new InvalidOperationException("No InnateCodes left after deletion! There should be a migration that adds them!");
                }

                context.SaveChanges();

                // Remove any brackets which may have Researcher names in them
                foreach (InnateCode code in context.InnateCodes)
                {
                    code.ActivityName = RemoveParenthesesText(code.ActivityName);
                }

                // Add in missing codes
                context.InnateCodes.AddRange(new List<InnateCode>
                {
                    new InnateCode
                    {
                        ActivityCode = "S-RES-RTP-255",
                        ActivityName = "Local Climate",
                        IsActive = true,
                        Tasks = GetDefaultInnateCodeTasks()
                    },
                    new InnateCode
                    {
                        ActivityCode = "S-RES-RTP-323",
                        ActivityName = "Trade-Off Grade",
                        IsActive = true,
                        Tasks = GetDefaultInnateCodeTasks()
                    },
                    new InnateCode
                    {
                        ActivityCode = "S-RES-RTP-324",
                        ActivityName = "PaPrKA",
                        IsActive = true,
                        Tasks = GetDefaultInnateCodeTasks()
                    }
                });
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Generate the default tasks for the Innate codes
        /// </summary>
        /// <returns></returns>
        private static IList<InnateCodeTask> GetDefaultInnateCodeTasks()
        {
            return new List<InnateCodeTask>
                {
                    new InnateCodeTask
                    {
                        TaskName = "Development",
                        Duty = Duty.ProjectWork
                    },
                    new InnateCodeTask
                    {
                        TaskName = "Management",
                        Duty = Duty.ProjectAndServiceMgmt
                    },
                    new InnateCodeTask
                    {
                        TaskName = "Maintenance",
                        Duty = Duty.ProjectWork
                    }
                };
        }

        /// <summary>
        /// Seed the skills tags in the database.
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedSkillTags(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding skills...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Remove existing
                context.SkillTags.ExecuteDelete();

                // Add some skill tags
                var skillTags = new List<SkillTag>
                {
                    new SkillTag { Name = "C#", ControlledName = "C# programming language", },
                    new SkillTag { Name = "JavaScript", ControlledName = "JavaScript programming language" },
                    new SkillTag { Name = "SQL", ControlledName = "SQL database management" },
                    new SkillTag { Name = "Azure", ControlledName = "Microsoft Azure cloud services" },
                    new SkillTag { Name = "AWS", ControlledName = "Amazon Web Services cloud services" },
                    new SkillTag { Name = "Agile", ControlledName = "Agile project management methodology" },
                    new SkillTag { Name = "DevOps", ControlledName = "DevOps practices and tools" },
                    new SkillTag { Name = "Docker", ControlledName = "Docker containerization technology" },
                    new SkillTag { Name = "Kubernetes", ControlledName = "Kubernetes container orchestration" },
                    new SkillTag { Name = "Machine Learning", ControlledName = "Machine learning techniques and algorithms" },
                    new SkillTag { Name = "Data Analysis", ControlledName = "Data analysis and visualization" },
                    new SkillTag { Name = "Cybersecurity", ControlledName = "Cybersecurity practices and tools" },
                    new SkillTag { Name = "Project Management", ControlledName = "Project management methodologies" },
                    new SkillTag { Name = "Business Analysis", ControlledName = "Business analysis techniques" }
                };

                // Update the wiki link status
                foreach (var skillTag in skillTags)
                {
                    skillTag.UpdateValidLinkAsync().GetAwaiter().GetResult();
                }

                context.SkillTags.AddRange(skillTags);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed some financial references around the current date
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedFinancialReferences(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding financial refs...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Clear existing financial references
                context.FinancialReferences.ExecuteDelete();

                // Add some financial references
                var fy = FinancialReference.GetFinancialYear(DateTime.Today);
                var financialReferences = new List<FinancialReference>
                {
                    new FinancialReference
                    {
                        FinancialYear = fy - 1,
                        Grade41Costs = 33333.55f,
                        Grade55Costs = 43172.16f,
                        Grade65Costs = 50935.8f,
                        Grade71Costs = 57458.16f,
                        Grade75Costs = 64797.29f,
                        RecoveryTarget = 1118849f,
                        Grade51Costs = 38011.97f
                    },
                    new FinancialReference
                    {
                        FinancialYear = fy,
                        Grade41Costs = 34510.63f,
                        Grade55Costs = 44349.48f,
                        Grade65Costs = 52095f,
                        Grade71Costs = 58617.36f,
                        Grade75Costs = 65956.38f,
                        RecoveryTarget = 1118849f,
                        Grade51Costs = 39799.01f
                    },
                    new FinancialReference
                    {
                        FinancialYear = fy + 1,
                        Grade41Costs = 35740.07f,
                        Grade55Costs = 45603.10f,
                        Grade65Costs = 53422.28f,
                        Grade71Costs = 60005.48f,
                        Grade75Costs = 67585.78f,
                        RecoveryTarget = 1518718f,
                        Grade51Costs = 41010.82f
                    }
                };
                context.FinancialReferences.AddRange(financialReferences);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed projects -- repurposes some projects from the live DB and changes details.
        /// RTP number is important as it is used to construct the dependent entities so beware of changing it!
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedProjects(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding projects...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Create the projects
                var projects = new List<Project>
                {
                    new Project
                    {
                        ActualCost = 0.0,
                        ActualLeadershipCosts = 0,
                        ActualWorkHours = 0.0,
                        Budget = 12152.0,
                        CostModel = CostModel.DayRate,
                        DayRate = 250,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = ApplyDateOffset(2025, 07, 31),
                        Faculty = Faculty.Internal,
                        LeadershipFTE = 0.05f,
                        Name = "Create CoP for Research Software",
                        PI = "Dr. Waffle McSnort",
                        PlannedCost = 0.0,
                        PlannedLeadershipCosts = 0.0,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, ApplyDateOffset(2023, 07, 03), ApplyDateOffset(2025, 07, 31)),
                        ProjectStatus = ProjectStatus.CancelledBidFailed,
                        RTP = 169,
                        RequestDocLink = "https://www.google.com",
                        School = School.None,
                        StartDate = ApplyDateOffset(2023, 07, 03),
                    },
                    new Project
                    {
                        ActualCost = 39120.05,
                        ActualLeadershipCosts = 0,
                        ActualWorkHours = 1048.5,
                        ActualsLastUpdated = ApplyDateOffset(2025, 06, 02, 13, 12, 0).ToString("R"),
                        Budget = 42269.0,
                        CostModel = CostModel.TechOnly,
                        DayRate = 262,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = ApplyDateOffset(2025, 07, 31),
                        Faculty = Faculty.FBMH,
                        InnateActivity = GetInnateActivityForRTP(context, 180),
                        LeadershipFTE = 0.05f,
                        Name = "Polypharmacy KSS",
                        PI = "Prof. Pickle Pants",
                        PlannedCost = 42123.55,
                        PlannedLeadershipCosts = 0.0,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, ApplyDateOffset(2023, 10, 02), ApplyDateOffset(2025, 07, 31)),
                        ProjectStatus = ProjectStatus.Active,
                        RTP = 180,
                        RequestDocLink = "https://www.google.com",
                        School = School.SHS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/69",
                        StartDate = ApplyDateOffset(2023, 10, 02),
                    },
                    new Project
                    {
                        ActualCost = 0.0,
                        ActualLeadershipCosts = 0,
                        ActualWorkHours = 0.0,
                        Budget = 71848.9,
                        CostModel = CostModel.TechAndLeadership,
                        DayRate = 250,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = ApplyDateOffset(2028, 06, 30),
                        Faculty = Faculty.FSE,
                        InnateActivity = GetInnateActivityForRTP(context, 255),
                        LeadershipFTE = 0.05f,
                        Name = "Local Climate Zone Modelling",
                        PI = "Sir Gigglesworth",
                        PlannedCost = 64074.39,
                        PlannedLeadershipCosts = 10140.21,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, ApplyDateOffset(2025, 07, 01), ApplyDateOffset(2028, 06, 30)),
                        ProjectStatus = ProjectStatus.Funded,
                        RTP = 255,
                        RequestDocLink = "https://",
                        School = School.SBS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/193",
                        StartDate = ApplyDateOffset(2025, 07, 01),
                    },
                    new Project
                    {
                        ActualCost = 0.0,
                        ActualLeadershipCosts = 0.0,
                        ActualWorkHours = 0.0,
                        Budget = 7425.0,
                        CostModel = 0,
                        DayRate = 297,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = ApplyDateOffset(2025, 10, 12),
                        Faculty = Faculty.FHUMS,
                        LeadershipFTE = 0.05f,
                        Name = "Political Research Transparency Web App",
                        PI = "Ms. Bubbles McGee",
                        PlannedCost = 7425.0,
                        PlannedLeadershipCosts = 0.0,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, ApplyDateOffset(2025, 10, 12), ApplyDateOffset(2025, 07, 01)),
                        ProjectStatus = ProjectStatus.AwaitingOutcome,
                        RTP = 265,
                        RequestDocLink = "https://www.google.com",
                        School = School.SSS,
                        StartDate = ApplyDateOffset(2025, 07, 01),
                    },
                    new Project
                    {
                        ActualCost = 2202.18,
                        ActualLeadershipCosts = 302.68,
                        ActualWorkHours = 73.5,
                        ActualsLastUpdated = ApplyDateOffset(2025, 04, 28, 14, 06, 0).ToString("R"),
                        Budget = 3035.0,
                        CostModel = CostModel.TechAndLeadership,
                        DayRate = 262,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = ApplyDateOffset(2025, 03, 20),
                        Faculty = Faculty.FBMH,
                        InnateActivity = GetInnateActivityForRTP(context, 311),
                        LeadershipFTE = 0.025f,
                        Name = "BMBaseDB Update",
                        PI = "Captain Quirk",
                        PlannedCost = 3197.15,
                        PlannedLeadershipCosts = 302.68,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, ApplyDateOffset(2025, 03, 20), ApplyDateOffset(2025, 01, 13)),
                        ProjectStatus = ProjectStatus.Finished,
                        RTP = 311,
                        RequestDocLink = "https://www.google.com",
                        School = School.SBS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/146",
                        StartDate = ApplyDateOffset(2025, 01, 13),
                    },
                    new Project
                    {
                        ActualCost = 3874.95,
                        ActualLeadershipCosts = 280.09,
                        ActualWorkHours = 114.8,
                        ActualsLastUpdated = ApplyDateOffset(2025, 06, 11, 09, 09, 0).ToString("R"),
                        Budget = 4963.0,
                        CostModel = CostModel.TechAndLeadership,
                        DayRate = 297,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = ApplyDateOffset(2028, 04, 08),
                        Faculty = Faculty.FHUMS,
                        InnateActivity = GetInnateActivityForRTP(context, 323),
                        LeadershipFTE = 0.025f,
                        Name = "Sustainability Trade-off Game Website",
                        PI = "Major Chuckles",
                        PlannedCost = 4633.86,
                        PlannedLeadershipCosts = 280.09,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, ApplyDateOffset(2028, 04, 08), ApplyDateOffset(2025, 02, 27)),
                        ProjectStatus =ProjectStatus.Paused,
                        RTP = 323,
                        RequestDocLink = "https://www.google.com",
                        School = School.AMBS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/154/views/1?custom_template=33",
                        StartDate = ApplyDateOffset(2025, 02, 27),
                    },
                    new Project
                    {
                        ActualCost = 7532.77,
                        ActualLeadershipCosts = 786.06,
                        ActualWorkHours = 177.2,
                        ActualsLastUpdated = ApplyDateOffset(2025, 06, 05, 14, 22, 0).ToString("R"),
                        Budget = 12937.81,
                        CostModel = CostModel.TechAndLeadership,
                        DayRate = 297,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = ApplyDateOffset(2026, 01, 29),
                        Faculty = Faculty.FBMH,
                        InnateActivity = GetInnateActivityForRTP(context, 324),
                        LeadershipFTE = 0.05f,
                        Name = "PAPrKA",
                        PI = "Lady Lollipop",
                        PlannedCost = 12852.1,
                        PlannedLeadershipCosts = 786.06,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, ApplyDateOffset(2026, 01, 29), ApplyDateOffset(2025, 03, 05)),
                        ProjectStatus = ProjectStatus.Maintenance,
                        RTP = 324,
                        RequestDocLink = "https://www.google.com",
                        School = School.SMS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/158",
                        StartDate = ApplyDateOffset(2025, 03, 05),
                    }
                };
                context.Projects.AddRange(projects);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed some dummy funding sources and attach to the projects in the DB already
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedFundingSources(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding funding sources...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Create funding sources
                var fundingSources = new List<FundingSource>
                {
                    new FundingSource
                    {
                        FundingSourceType = FundingSourceType.DA,
                        HasAccountCode = true,
                        AccountCode = "R1234",
                        Description = "Research and Teaching Project funding source",
                        AmountAvailable = 12152,
                        Project = GetProjectByRTP(context, 169)
                    },
                    new FundingSource
                    {
                        FundingSourceType = FundingSourceType.DI,
                        HasAccountCode = true,
                        AccountCode = "P5678",
                        Description = "Project Code funding source",
                        AmountAvailable = 42269,
                        Project = GetProjectByRTP(context, 180)
                    },
                    new FundingSource
                    {
                        FundingSourceType = FundingSourceType.Other,
                        HasAccountCode = false,
                        AccountCode = "N/A",
                        Description = "External Research Grant funding source",
                        AmountAvailable = 71848.9,
                        Project = GetProjectByRTP(context, 255)
                    },
                    new FundingSource
                    {
                        FundingSourceType = FundingSourceType.Other,
                        HasAccountCode = true,
                        AccountCode = "R9876",
                        Description = "Departmental Allocation for strategic initiatives",
                        AmountAvailable = 7425,
                        Project = GetProjectByRTP(context, 265)
                    },
                    new FundingSource
                    {
                        FundingSourceType = FundingSourceType.Other,
                        HasAccountCode = true,
                        AccountCode = "P4321",
                        Description = "Direct Investment for infrastructure upgrade",
                        AmountAvailable = 3035,
                        Project = GetProjectByRTP(context, 311)
                    },
                    new FundingSource
                    {
                        FundingSourceType = FundingSourceType.Other,
                        HasAccountCode = false,
                        AccountCode = "N/A",
                        Description = "Private donation for research excellence",
                        AmountAvailable = 966,
                        Project = GetProjectByRTP(context, 323)
                    },
                    new FundingSource
                    {
                        FundingSourceType = FundingSourceType.Other,
                        HasAccountCode = true,
                        AccountCode = "R2468",
                        Description = "Annual departmental budget allocation",
                        AmountAvailable = 3997,
                        Project = GetProjectByRTP(context, 324)
                    },
                    new FundingSource
                    {
                        FundingSourceType = FundingSourceType.DI,
                        HasAccountCode = true,
                        AccountCode = "P1357",
                        Description = "Project-specific funding from external partner",
                        AmountAvailable = 12937.81,
                        Project = GetProjectByRTP(context, 324)
                    }
                };

                // Set as leadership funding source where cost model requires it
                foreach (var fs in fundingSources)
                {
                    // If requires leadership funding source and there isn't one already then assign
                    if (fs.Project.CostModel == CostModel.TechAndLeadership && fs.ProjectLeadershipSource == null)
                    {
                        fs.ProjectLeadershipSource = fs.Project;
                    }
                }

                context.FundingSources.AddRange(fundingSources);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Adds subtasks to the projects in the DB
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedSubTasks(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding subtasks...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Create subtasks
                var subTasks = new List<SubTask>
                {
                    new SubTask
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        RequiresLeadership = true,
                        Demand = 0.1,
                        DurationBillableDays = 458,
                        DurationDays = 760,
                        EndDate = ApplyDateOffset(2025, 07, 31),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "Run CoP",
                        OriginalDemand = 0.1,
                        PlannedCost = 0,
                        PlannedWorkHours = 320.6,
                        StartDate = ApplyDateOffset(2023, 07, 03),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0.1
                    },
                    new SubTask
                    {
                        ActualCost = 39120.05,
                        ActualWorkHours = 1048.5,
                        RequiresLeadership = true,
                        Demand = 0.4,
                        DurationBillableDays = 404,
                        DurationDays = 669,
                        EndDate = ApplyDateOffset(2025, 07, 31),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE",
                        OriginalDemand = 0.4,
                        PlannedCost = 42123.54,
                        PlannedWorkHours = 1129,
                        StartDate = ApplyDateOffset(2023, 10, 02),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        RequiresLeadership = true,
                        Demand = 0.3,
                        DurationBillableDays = 38,
                        DurationDays = 63,
                        EndDate = ApplyDateOffset(2025, 09, 01),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE Support",
                        OriginalDemand = 0.3,
                        PlannedCost = 2987.96,
                        PlannedWorkHours = 78.5,
                        StartDate = ApplyDateOffset(2025, 07, 01),
                        TaskType = TaskType.FixedWork,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        RequiresLeadership = true,
                        Demand = 0.3,
                        DurationBillableDays = 623,
                        DurationDays = 1033,
                        EndDate = ApplyDateOffset(2028, 06, 30),
                        HasFixedEndDate = false,
                        HasFixedStart = false,
                        Lag = 0,
                        Name = "RSE Support (Copy)",
                        OriginalDemand = 0.3,
                        PlannedCost = 50946.21,
                        PlannedWorkHours = 1307.5,
                        StartDate = ApplyDateOffset(2025, 09, 02),
                        TaskType = TaskType.FixedWork,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        RequiresLeadership = true,
                        Demand = 0.38,
                        DurationBillableDays = 63,
                        DurationDays = 104,
                        EndDate = ApplyDateOffset(2025, 10, 12),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE Build and Deploy Website",
                        OriginalDemand = 0.4,
                        PlannedCost = 7425,
                        PlannedWorkHours = 175,
                        StartDate = ApplyDateOffset(2025, 07, 01),
                        TaskType = TaskType.FixedWork,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 1899.49,
                        ActualWorkHours = 73.5,
                        RequiresLeadership = true,
                        Demand = 0.4,
                        DurationBillableDays = 40,
                        DurationDays = 67,
                        EndDate = ApplyDateOffset(2025, 03, 20),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE",
                        OriginalDemand = 0.4,
                        PlannedCost = 2894.47,
                        PlannedWorkHours = 112,
                        StartDate = ApplyDateOffset(2025, 01, 13),
                        TaskType = TaskType.FixedWork,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 1960.25,
                        ActualWorkHours = 51.5,
                        RequiresLeadership = true,
                        Demand = 0.3,
                        DurationBillableDays = 25,
                        DurationDays = 41,
                        EndDate = ApplyDateOffset(2025, 04, 08),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE",
                        OriginalDemand = 0.3,
                        PlannedCost = 1960.25,
                        PlannedWorkHours = 51.5,
                        StartDate = ApplyDateOffset(2025, 02, 27),
                        TaskType = TaskType.FixedWork,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        RequiresLeadership = false,
                        Demand = 0.005,
                        DurationBillableDays = 441,
                        DurationDays = 731,
                        EndDate = ApplyDateOffset(2028, 04, 08),
                        HasFixedEndDate = true,
                        HasFixedStart = false,
                        Lag = 365,
                        Name = "Maintenance",
                        OriginalDemand = 0.005,
                        PlannedCost = 584.46,
                        PlannedWorkHours = 15,
                        StartDate = ApplyDateOffset(2026, 04, 09),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 1634.60,
                        ActualWorkHours = 63.25,
                        RequiresLeadership = true,
                        Demand = 0.8,
                        DurationBillableDays = 13,
                        DurationDays = 21,
                        EndDate = ApplyDateOffset(2025, 06, 08),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 30,
                        Name = "RSE 2",
                        OriginalDemand = 0.8,
                        PlannedCost = 1809.04,
                        PlannedWorkHours = 70,
                        StartDate = ApplyDateOffset(2025, 05, 19),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        RequiresLeadership = false,
                        Demand = 0.1,
                        DurationBillableDays = 150,
                        DurationDays = 249,
                        EndDate = ApplyDateOffset(2026, 01, 29),
                        HasFixedEndDate = false,
                        HasFixedStart = false,
                        Lag = 0,
                        Name = "Support",
                        OriginalDemand = 0.1,
                        PlannedCost = 3996.63,
                        PlannedWorkHours = 105,
                        StartDate = ApplyDateOffset(2025, 05, 26),
                        TaskType = TaskType.FixedWork,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 142.73,
                        ActualWorkHours = 3.75,
                        RequiresLeadership = true,
                        Demand = 0.4,
                        DurationBillableDays = 5,
                        DurationDays = 7,
                        EndDate = ApplyDateOffset(2025, 05, 11),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 2 after break for Agile training",
                        OriginalDemand = 0.4,
                        PlannedCost = 304.50,
                        PlannedWorkHours = 8,
                        StartDate = ApplyDateOffset(2025, 05, 05),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0.1
                    },
                    new SubTask
                    {
                        ActualCost = 3596.97,
                        ActualWorkHours = 94.5,
                        RequiresLeadership = true,
                        Demand = 0.4,
                        DurationBillableDays = 33,
                        DurationDays = 54,
                        EndDate = ApplyDateOffset(2025, 04, 27),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 1",
                        OriginalDemand = 0.4,
                        PlannedCost = 3463.75,
                        PlannedWorkHours = 91,
                        StartDate = ApplyDateOffset(2025, 03, 05),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 1808.00,
                        ActualWorkHours = 47.5,
                        RequiresLeadership = true,
                        Demand = 0.4,
                        DurationBillableDays = 32,
                        DurationDays = 53,
                        EndDate = ApplyDateOffset(2025, 04, 26),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 2",
                        OriginalDemand = 0.4,
                        PlannedCost = 2550.23,
                        PlannedWorkHours = 67,
                        StartDate = ApplyDateOffset(2025, 03, 05),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0.1
                    },
                    new SubTask
                    {
                        ActualCost = 551.91,
                        ActualWorkHours = 14.5,
                        RequiresLeadership = true,
                        Demand = 0.4,
                        DurationBillableDays = 11,
                        DurationDays = 17,
                        EndDate = ApplyDateOffset(2025, 05, 31),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 2 Overrun",
                        OriginalDemand = 0.4,
                        PlannedCost = 1065.77,
                        PlannedWorkHours = 28,
                        StartDate = ApplyDateOffset(2025, 05, 15),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 647.07,
                        ActualWorkHours = 17,
                        RequiresLeadership = true,
                        Demand = 0.4,
                        DurationBillableDays = 5,
                        DurationDays = 7,
                        EndDate = ApplyDateOffset(2025, 05, 25),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 1 postponed final week",
                        OriginalDemand = 0.4,
                        PlannedCost = 418.69,
                        PlannedWorkHours = 11,
                        StartDate = ApplyDateOffset(2025, 05, 19),
                        TaskType = TaskType.FixedDuration,
                        UnmetDemand = 0
                    },
                    new SubTask
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        RequiresLeadership = true,
                        Demand = 0.2,
                        DurationBillableDays = 5,
                        DurationDays = 9,
                        EndDate = ApplyDateOffset(2025, 06, 09),
                        HasFixedEndDate = false,
                        HasFixedStart = false,
                        Lag = 0,
                        Name = "RSE 2 Extension 2",
                        OriginalDemand = 0.2,
                        PlannedCost = 266.44,
                        PlannedWorkHours = 7,
                        StartDate = ApplyDateOffset(2025, 06, 01),
                        TaskType = TaskType.FixedWork,
                        UnmetDemand = 0
                    }
                };

                // Assign to the correct projects
                var project = GetProjectByRTP(context, 169);
                project.SubTasks = new List<SubTask>
                {
                    subTasks[0]
                };
                context.SaveChanges();

                project = GetProjectByRTP(context, 180);
                project.SubTasks = new List<SubTask>
                {
                    subTasks[1]
                };
                context.SaveChanges();

                project = GetProjectByRTP(context, 255);
                subTasks[3].Predecessor = subTasks[2];
                project.SubTasks = new List<SubTask>
                {
                    subTasks[2],
                    subTasks[3]
                };
                context.SaveChanges();

                project = GetProjectByRTP(context, 265);
                project.SubTasks = new List<SubTask>
                {
                    subTasks[4]
                };
                context.SaveChanges();

                project = GetProjectByRTP(context, 311);
                project.SubTasks = new List<SubTask>
                {
                    subTasks[5]
                };
                context.SaveChanges();

                project = GetProjectByRTP(context, 323);
                subTasks[7].Predecessor = subTasks[6];
                project.SubTasks = new List<SubTask>
                {
                    subTasks[6],
                    subTasks[7],
                    subTasks[8]
                };
                context.SaveChanges();

                project = GetProjectByRTP(context, 324);
                subTasks[9].Predecessor = subTasks[14];
                subTasks[15].Predecessor = subTasks[13];
                project.SubTasks = new List<SubTask>
                {
                    subTasks[9],
                    subTasks[10],
                    subTasks[11],
                    subTasks[12],
                    subTasks[13],
                    subTasks[14],
                    subTasks[15]
                };
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Adds resources to the subtasks in the DB
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedResources(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding resources...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                var project = GetProjectWithSubTaskAndFundingByRTP(context, 180);
                project.SubTasks[0].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 39120.05,
                        ActualWorkHours = 1048.5,
                        AssignmentFTE = 0.4,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "EA"),
                        PlannedCost = 42123.54,
                        PlannedWorkHours = 1129,
                        UseProjectDayRate = true
                    }
                };
                context.SaveChanges();

                project = GetProjectWithSubTaskAndFundingByRTP(context, 255);
                project.SubTasks[0].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        AssignmentFTE = 0.3,
                        DayRate = 250,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "GC"),
                        PlannedCost = 2987.96,
                        PlannedWorkHours = 78.5,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[1].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        AssignmentFTE = 0.3,
                        DayRate = 250,
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "SAN"),
                        PlannedCost = 50946.21,
                        PlannedWorkHours = 1307.5,
                        UseProjectDayRate = true
                    }
                };
                context.SaveChanges();

                project = GetProjectWithSubTaskAndFundingByRTP(context, 265);
                project.SubTasks[0].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        AssignmentFTE = 0.4,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "CS"),
                        PlannedCost = 7425,
                        PlannedWorkHours = 175,
                        UseProjectDayRate = true
                    }
                };
                context.SaveChanges();

                project = GetProjectWithSubTaskAndFundingByRTP(context, 311);
                project.SubTasks[0].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 1899.49,
                        ActualWorkHours = 73.5,
                        AssignmentFTE = 0.4,
                        DayRate = 262,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "BM"),
                        PlannedCost = 2894.47,
                        PlannedWorkHours = 112,
                        UseProjectDayRate = true
                    }
                };
                context.SaveChanges();

                project = GetProjectWithSubTaskAndFundingByRTP(context, 323);
                project.SubTasks[0].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 1960.25,
                        ActualWorkHours = 51.5,
                        AssignmentFTE = 0.3,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "LL"),
                        PlannedCost = 1960.25,
                        PlannedWorkHours = 51.5,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[1].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        AssignmentFTE = 0.005,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.Last(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "LL"),
                        PlannedCost = 584.46,
                        PlannedWorkHours = 15,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[2].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 1634.60,
                        ActualWorkHours = 63.25,
                        AssignmentFTE = 0.8,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "BM"),
                        PlannedCost = 1809.04,
                        PlannedWorkHours = 70,
                        UseProjectDayRate = true
                    }
                };
                context.SaveChanges();

                project = GetProjectWithSubTaskAndFundingByRTP(context, 324);
                project.SubTasks[0].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        AssignmentFTE = 0.1,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "LS"),
                        PlannedCost = 3996.63,
                        PlannedWorkHours = 105,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[1].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 142.73,
                        ActualWorkHours = 3.75,
                        AssignmentFTE = 0.3,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "AG"),
                        PlannedCost = 304.50,
                        PlannedWorkHours = 8,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[2].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 3596.97,
                        ActualWorkHours = 94.5,
                        AssignmentFTE = 0.4,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "LS"),
                        PlannedCost = 3463.75,
                        PlannedWorkHours = 91,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[3].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 1808.00,
                        ActualWorkHours = 47.5,
                        AssignmentFTE = 0.3,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "AG"),
                        PlannedCost = 2550.23,
                        PlannedWorkHours = 67,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[4].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 551.91,
                        ActualWorkHours = 14.5,
                        AssignmentFTE = 0.4,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "AG"),
                        PlannedCost = 1065.77,
                        PlannedWorkHours = 28,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[5].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 647.07,
                        ActualWorkHours = 17,
                        AssignmentFTE = 0.4,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "LS"),
                        PlannedCost = 418.69,
                        PlannedWorkHours = 11,
                        UseProjectDayRate = true
                    }
                };
                project.SubTasks[6].AssignedResources = new List<Resource>
                {
                    new Resource
                    {
                        ActualCost = 0,
                        ActualWorkHours = 0,
                        AssignmentFTE = 0.2,
                        DayRate = 297,
                        FundedFrom = project.FundingSources.First(),
                        IsProvisional = false,
                        Person = context.People.First(x => x.ShortName == "AG"),
                        PlannedCost = 266.44,
                        PlannedWorkHours = 7,
                        UseProjectDayRate = true
                    }
                };
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seeds some simple, random notes against each project in the DB
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedNotes(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding notes...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Add a note to each project
                var projects = context.Projects.ToList();
                var users = context.Users.ToList();
                Random rnd = new Random();
                foreach (var project in projects)
                {
                    for (int i = 0; i < rnd.Next(1, 5); ++i)
                    {
                        context.Notes.Add(new Note
                        {
                            HtmlContent = GetDummyParagraphsAsHtml(),
                            Author = users[rnd.Next(0, context.Users.Count())],
                            CreatedDate = DateTime.Now.AddDays(-rnd.Next(1, 100)),
                            Project = project
                        });
                    }
                }
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed some dummy invoices and payments against projects with other funding sources
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedInvoicesAndPayments(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding invoices and payments...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Add a payment and invoice to each project with other funding sources
                var projects = context.Projects
                    .Include(x => x.FundingSources)
                    .Where(x => x.FundingSources
                        .Any(x => x.FundingSourceType == FundingSourceType.Other)
                    )
                    .ToList();
                var users = context.Users.ToList();
                foreach (var project in projects)
                {
                    var funding = project.FundingSources.First(x => x.FundingSourceType == FundingSourceType.Other);
                    var invoice = new Invoice
                    {
                        Value = funding.AmountAvailable,
                        InvoiceUrl = "https://example.com/invoice.pdf",
                        KeyDate = DateTime.Now.AddDays(-15),
                        InvoiceReference = $"INV-{project.RTP}-{DateTime.Now.Year}-{funding.FundingSourceId}",
                        Description = "This is a dummy invoice created as part of database seeding.",
                        Project = project,
                        Status = InvoiceStatus.Paid
                    };
                    context.Invoices.Add(invoice);
                    context.SaveChanges();

                    var payment = new Payment
                    {
                        Source = funding,
                        KeyDate = invoice.KeyDate.AddDays(10),
                        Invoice = invoice,
                        Value = invoice.Value,
                        Description = "This is a dummy payment created as part of database seeding.",
                        Project = project,
                    };
                    context.Payments.Add(payment);
                }
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Generate some dummy timesheets for people in the DB
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedTimesheets(IServiceProvider serviceProvider)
        {

            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInformation("Seeding timesheets...");
            using (var context = dbContextFactory.CreateDbContext())
            {
                foreach (var person in context.People.Include(x => x.WorkloadModelChanges))
                {
                    // Set start and end dates to the nearest Monday
                    var startDate = person.StartDate;
                    if (startDate.DayOfWeek != DayOfWeek.Monday)
                    {
                        // Change to next Monday
                        startDate = startDate.AddDays(7 - (int)startDate.DayOfWeek + (int)DayOfWeek.Monday);
                    }

                    // Use the person's end date (if they have one and it's not in the future) or use today by default
                    var endDate = person.EndDate is null || person.EndDate > DateTime.Today ? DateTime.Today : person.EndDate;

                    if (endDate?.DayOfWeek != DayOfWeek.Monday)
                    {
                        // Change to nearest previous Monday
                        endDate = endDate?.AddDays(-(int)endDate?.DayOfWeek + (int)DayOfWeek.Monday);
                    }

                    // Add timesheets for each week in the range
                    var currentDate = startDate;
                    while (currentDate <= endDate)
                    {
                        logger.LogInformation($"Seeding timesheet for {person.ShortName} for week starting {currentDate.ToShortDateString()}");
                        var timesheet = new Timesheet
                        {
                            Owner = person,
                            StartDate = currentDate,
                            CreatedDate = startDate,
                            DateStatusChanged = startDate,
                            StatusChangedBy = person,
                            Info = Lorem.Paragraph(5, 2),
                            Status = GenerateDummyTimesheetStatus(currentDate),
                            TimesheetEntries = GenerateDummyTimesheetEntries(context, person, currentDate)
                        };

                        // Add timesheet
                        context.Timesheets.Add(timesheet);

                        // Advance to the next week
                        currentDate = currentDate.AddDays(7);
                    }
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Returns a random but logic list of timesheet entries for a person based on standard WLMs and variability in of 0.05 increments
        /// </summary>
        /// <param name="context"></param>
        /// <param name="person"></param>
        /// <param name="currentDate"></param>
        /// <returns></returns>
        private static IList<TimesheetEntry> GenerateDummyTimesheetEntries(PPMToolContext context, Person person, DateTime currentDate)
        {
            var list = new List<TimesheetEntry>();
            var rnd = new Random();

            // Get WLM of the person at the start of the week
            var wlm = person.GetWorkloadModelOnDateOrDefault(currentDate);

            // Project work structure where they are resource and project is running on this week
            var projects = context.Projects
                .Include(x => x.InnateActivity)
                    .ThenInclude(x => x.Tasks)
                .Include(x => x.SubTasks)
                    .ThenInclude(x => x.AssignedResources)
                        .ThenInclude(x => x.Person)
                .Where(x => x.SubTasks
                    .Any(x => x.AssignedResources
                        .Any(x => x.Person.PersonId == person.PersonId)
                    )
                )
                .ToList()
                .Where(x => x.IsWithin(currentDate));

            // Assume 5% chance of them being on leave that week
            var onLeave = rnd.Next(100) < 20;
            if (onLeave)
            {
                var value = wlm.Total() * 35 / 5f;
                var entry = new TimesheetEntry
                {
                    InnateCodeTask = context.InnateCodeTasks.First(x => x.TaskName == "Annual Leave (Holidays)"),
                    MondayHours = value,
                    TuesdayHours = value,
                    WednesdayHours = value,
                    ThursdayHours = value,
                    FridayHours = value
                };
                entry.UpdateTotalHours();
                list.Add(entry);
                return list;
            }

            // Get random targets for this timesheet
            var psmFte = RandomlyVaryPositiveDouble(wlm.ProjectAndServiceManagementFTE);
            var bauFte = RandomlyVaryPositiveDouble(wlm.BusinessAsUsualFTE);
            var staffFte = RandomlyVaryPositiveDouble(wlm.StaffManagementFTE);
            var rsaFte = RandomlyVaryPositiveDouble(wlm.ArchitectureFTE);
            var pdFte = RandomlyVaryPositiveDouble(wlm.PersonalDevelopmentFTE);
            var projectFte = wlm.Total() - (psmFte + bauFte + staffFte + rsaFte + pdFte);
            if (projectFte < 0) projectFte = 0;

            // Get all active tasks
            var activeTasks = context.InnateCodeTasks
                .Include(x => x.InnateCode)
                .Where(x => x.InnateCode.IsActive);

            // For each area, add in a sutiable timesheet entry if non-zero
            if (psmFte > 0)
            {
                var value = psmFte * wlm.Total() * 35 / 5f;
                value = RoundUpToQuarterHour(value); // Round up the value.

                var entry = new TimesheetEntry
                {
                    InnateCodeTask = activeTasks.GetRandomTask(Duty.ProjectAndServiceMgmt),
                    MondayHours = value,
                    TuesdayHours = value,
                    WednesdayHours = value,
                    ThursdayHours = value,
                    FridayHours = value
                };
                entry.UpdateTotalHours();
                list.Add(entry);
            }

            if (bauFte > 0)
            {
                var value = bauFte * wlm.Total() * 35 / 5f;
                value = RoundUpToQuarterHour(value); // Round up the value.

                var entry = new TimesheetEntry
                {
                    InnateCodeTask = activeTasks.GetRandomTask(Duty.BAU),
                    MondayHours = value,
                    TuesdayHours = value,
                    WednesdayHours = value,
                    ThursdayHours = value,
                    FridayHours = value
                };
                entry.UpdateTotalHours();
                list.Add(entry);
            }

            if (staffFte > 0)
            {
                var value = staffFte * wlm.Total() * 35 / 5f;
                value = RoundUpToQuarterHour(value); // Round up the value.

                var entry = new TimesheetEntry
                {
                    InnateCodeTask = activeTasks.GetRandomTask(Duty.StaffMgmt),
                    MondayHours = value,
                    TuesdayHours = value,
                    WednesdayHours = value,
                    ThursdayHours = value,
                    FridayHours = value
                };
                entry.UpdateTotalHours();
                list.Add(entry);
            }

            if (rsaFte > 0)
            {
                var value = rsaFte * wlm.Total() * 35 / 5f;
                value = RoundUpToQuarterHour(value); // Round up the value.

                var entry = new TimesheetEntry
                {
                    InnateCodeTask = activeTasks.GetRandomTask(Duty.RSA),
                    MondayHours = value,
                    TuesdayHours = value,
                    WednesdayHours = value,
                    ThursdayHours = value,
                    FridayHours = value
                };
                entry.UpdateTotalHours();
                list.Add(entry);
            }

            if (pdFte > 0)
            {
                var value = pdFte * wlm.Total() * 35 / 5f;
                value = RoundUpToQuarterHour(value); // Round up the value.

                var entry = new TimesheetEntry
                {
                    InnateCodeTask = activeTasks.GetRandomTask(Duty.ProjectAndServiceMgmt),
                    MondayHours = value,
                    TuesdayHours = value,
                    WednesdayHours = value,
                    ThursdayHours = value,
                    FridayHours = value
                };
                entry.UpdateTotalHours();
                list.Add(entry);
            }

            // Project work codes selected from the projects they are working on
            if (projectFte > 0)
            {
                // Extract development tasks for the projects assigned to this week and store the assignment FTE
                IDictionary<InnateCodeTask, double> projectTasks = new Dictionary<InnateCodeTask, double>();
                foreach (var project in projects)
                {
                    // Get the innate activity for the project
                    var innateActivity = project.InnateActivity;
                    if (innateActivity == null) continue;

                    // Get the tasks for the innate activity
                    var task = innateActivity.Tasks.First(x => x.Duty == Duty.ProjectWork);

                    // For each resource assigned to the project this week that is this person, get their assignment FTE
                    var assignmentFte = project.SubTasks
                        .Where(x => x.IsWithin(currentDate))
                        .SelectMany(x => x.AssignedResources)
                        .Where(x => x.Person.PersonId == person.PersonId)
                        .Sum(x => x.AssignmentFTE);

                    // Add to dictionary
                    if (assignmentFte > 0)
                    {
                        projectTasks[task] = assignmentFte;
                    }
                }

                // If not assignments than assign a BAU activity
                if (projectTasks.Count == 0)
                {
                    var value = projectFte * wlm.Total() * 35 / 5f;
                    value = RoundUpToQuarterHour(value); // Round up the value.

                    var entry = new TimesheetEntry
                    {
                        InnateCodeTask = activeTasks.GetRandomTask(Duty.BAU),
                        MondayHours = value,
                        TuesdayHours = value,
                        WednesdayHours = value,
                        ThursdayHours = value,
                        FridayHours = value
                    };
                    entry.UpdateTotalHours();
                    list.Add(entry);
                }
                else
                {
                    // Create timesheet entries for each project task with hours based on the assignment FTE
                    var totalAssignmentFte = projectTasks.Values.Sum();
                    foreach (var kvp in projectTasks)
                    {
                        var task = kvp.Key;
                        var assignmentFte = kvp.Value;
                        var value = (assignmentFte / totalAssignmentFte) * projectFte * wlm.Total() * 35 / 5f;
                        value = RoundUpToQuarterHour(value); // Round up the value.

                        var entry = new TimesheetEntry
                        {
                            InnateCodeTask = task,
                            MondayHours = value,
                            TuesdayHours = value,
                            WednesdayHours = value,
                            ThursdayHours = value,
                            FridayHours = value
                        };
                        entry.UpdateTotalHours();
                        list.Add(entry);
                    }
                }
            }
            return list;
        }

        private static InnateCodeTask GetRandomTask(this IQueryable<InnateCodeTask> allTasks, Duty duty)
        {
            var rnd = new Random();
            var tasks = allTasks.Where(x => x.Duty == duty).ToList();
            if (tasks.Count == 0) throw new InvalidOperationException($"No innate tasks for duty {duty}");
            return tasks[rnd.Next(tasks.Count)];
        }

        /// <summary>
        /// Vary a double value by a random percentage between -20% and +20% in steps of 0.01
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static double RandomlyVaryPositiveDouble(double value)
        {
            var rnd = new Random();

            // Do nothing to it if not positive
            if (value <= 0) return value;

            // Generate a random percentage between -20% and +20% in steps of 0.01
            int percent = rnd.Next(-20, 21);
            double variation = percent * value / 100d;
            var newValue = value + variation;

            // Ensure to nearest 0.01
            newValue = (int)Math.Round(newValue * 100) / 100d;

            // Check always positive
            if (newValue < 0) newValue = 0;

            return newValue;
        }

        /// <summary>
        /// Generate a timesheet status that is random in the last couple of weeks
        /// </summary>
        /// <param name="currentDate"></param>
        /// <returns></returns>
        private static TimesheetStatus GenerateDummyTimesheetStatus(DateTime currentDate)
        {
            // If within the last couple of weeks then random status
            if (currentDate >= DateTime.Today.AddDays(-14))
            {
                var rnd = new Random();
                var values = Enum.GetValues<TimesheetStatus>();
                return values[rnd.Next(values.Length)];
            }
            return TimesheetStatus.Approved;
        }

        /// <summary>
        /// Get a project with its subtasks and funding sources by RTP
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rtp"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">There are no matching projects</exception>
        private static Project GetProjectWithSubTaskAndFundingByRTP(PPMToolContext context, int rtp)
        {
            // Get a project
            var project = context.Projects
                .Include(x => x.SubTasks)
                .Include(x => x.FundingSources)
                .FirstOrDefault(x => x.RTP == rtp);

            // If no projects
            if (project == null)
            {
                throw new InvalidOperationException("No matching project!");
            }

            return project;
        }

        /// <summary>
        /// Return the first project in DB that matches the RTP given.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rtp"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If there are no projects in the DB that match</exception>
        private static Project GetProjectByRTP(PPMToolContext context, int rtp)
        {
            // Get a project
            var project = context.Projects
                .FirstOrDefault(x => x.RTP == rtp);

            // If no projects
            if (project == null)
            {
                throw new InvalidOperationException("No matching project!");
            }

            return project;
        }

        /// <summary>
        /// Returns the InnateActivity which matches the RTP code. If it doesn't exist, it returns null.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rtp"></param>
        /// <returns></returns>
        private static InnateCode GetInnateActivityForRTP(PPMToolContext context, int rtp)
        {
            return context.InnateCodes.FirstOrDefault(x => x.ActivityCode.EndsWith($"RTP-{rtp}"));
        }

        /// <summary>
        /// Get a random manager from the available people who is employed during the whole of the window given
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rangeStart"></param>
        /// <param name="rangeEnd"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">No eligible managers</exception>
        private static Person GetRandomManagerActiveDuringDateRange(PPMToolContext context, DateTime rangeStart, DateTime rangeEnd)
        {
            // Get selection of managers who are active during the window given
            var managers = context.Users
                .Where(x => x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                .Include(x => x.Person)
                .Select(x => x.Person)
                .Where(x => x.StartDate <= rangeStart && (x.EndDate == null || x.EndDate >= rangeEnd))
                .ToList();

            // Bail if no-one eligible
            if (managers.Count == 0)
            {
                throw new InvalidOperationException("Cannot find a manager who is active during the project period! Modify the seeding code.");
            }

            // Return a random one from the list
            var rnd = new Random();
            return managers[rnd.Next(managers.Count - 1)];
        }

        /// <summary>
        /// Method to return a few paragraphs of text
        /// </summary>
        /// <returns></returns>
        private static string GetDummyParagraphsAsHtml()
        {
            var paragraphs = Lorem.Paragraphs(3, 9, 5, 8, 1);
            var sb = new StringBuilder();
            foreach (var paragraph in paragraphs)
            {
                sb.Append($"<p>{paragraph.ToString()}</p>");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Rounds up a timesheet value to the nearest quarter of an hour
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static double RoundUpToQuarterHour(double hours)
        {
            return Math.Ceiling(hours * 4) / 4.0;
        }

        /// <summary>
        /// Replace all the text in the parentheses and the parentheses themselves with nothing
        /// </summary>
        /// <param name="stringWithParentheses"></param>
        /// <returns></returns>
        public static string RemoveParenthesesText(string stringWithParentheses)
        {
            return Regex.Replace(stringWithParentheses, @"\s*\(.*?\)\s*", "");
        }
    }
}
