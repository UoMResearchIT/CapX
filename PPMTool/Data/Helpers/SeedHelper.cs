using System.Text;
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

                // Perm currently with us
                person = new Person();
                person.Name = "Nigel Overfetch-Nelson";
                person.ShortName = "NO";
                person.FTE = 1.0;
                person.StartDate = DateTime.Today.AddMonths(-6);
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
                }
                else
                {
                    throw new InvalidOperationException("Person with ShortName 'CB' not found.");
                }

                context.SaveChanges();
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
            using (var context = dbContextFactory.CreateDbContext())
            {
                // Remove a certain number of the existing InnateCodes
                context.InnateCodes.Where(x =>
                    x.ActivityName.StartsWith("P0") ||
                    x.ActivityName.StartsWith("S-RES-RTP") ||
                    x.ActivityName.StartsWith("S-RES-P")
                )
                .ExecuteDelete();

                // Check there are some left
                if (context.InnateCodes.Count() == 0)
                {
                    throw new InvalidOperationException("No InnateCodes left after deletion! There should be a migration that adds them!");
                }

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Seed the skills tags in the database.
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedSkillTags(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
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
        /// RTP number is important as it is used to construct the depdendent entities so beware of changing it!
        /// </summary>
        /// <param name="serviceProvider"></param>
        internal static void SeedProjects(IServiceProvider serviceProvider)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
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
                        EndDate = new DateTime(2025, 07, 31),
                        Faculty = Faculty.Internal,
                        LeadershipFTE = 0.05f,
                        Name = "Create CoP for Research Software",
                        PI = "Dr. Waffle McSnort",
                        PlannedCost = 0.0,
                        PlannedLeadershipCosts = 0.0,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, new DateTime(2023, 07, 03), new DateTime(2025, 07, 31)),
                        ProjectStatus = ProjectStatus.CancelledBidFailed,
                        RTP = 169,
                        RequestDocLink = "https://www.google.com",
                        School = School.None,
                        StartDate = new DateTime(2023, 07, 03),
                    },
                    new Project
                    {
                        ActualCost = 39120.05,
                        ActualLeadershipCosts = 0,
                        ActualWorkHours = 1048.5,
                        ActualsLastUpdated = new DateTime(2025, 06, 02, 13, 12, 0).ToString("R"),
                        Budget = 42269.0,
                        CostModel = CostModel.TechOnly,
                        DayRate = 262,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = new DateTime(2025, 07, 31),
                        Faculty = Faculty.FBMH,
                        InnateActivity = GetInnateActivityForRTP(context, 180),
                        LeadershipFTE = 0.05f,
                        Name = "Polypharmacy KSS",
                        PI = "Prof. Pickle Pants",
                        PlannedCost = 42123.55,
                        PlannedLeadershipCosts = 0.0,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, new DateTime(2023, 10, 02), new DateTime(2025, 07, 31)),
                        ProjectStatus = ProjectStatus.Active,
                        RTP = 180,
                        RequestDocLink = "https://www.google.com",
                        School = School.SHS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/69",
                        StartDate = new DateTime(2023, 10, 02),
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
                        EndDate = new DateTime(2028, 06, 30),
                        Faculty = Faculty.FSE,
                        InnateActivity = GetInnateActivityForRTP(context, 255),
                        LeadershipFTE = 0.05f,
                        Name = "Local Climate Zone Modelling",
                        PI = "Sir Gigglesworth",
                        PlannedCost = 64074.39,
                        PlannedLeadershipCosts = 10140.21,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, new DateTime(2025, 07, 01), new DateTime(2028, 06, 30)),
                        ProjectStatus = ProjectStatus.Funded,
                        RTP = 255,
                        School = School.SBS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/193",
                        StartDate = new DateTime(2025, 07, 01),
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
                        EndDate = new DateTime(2025, 10, 12),
                        Faculty = Faculty.FHUMS,
                        LeadershipFTE = 0.05f,
                        Name = "Political Research Transparency Web App",
                        PI = "Ms. Bubbles McGee",
                        PlannedCost = 7425.0,
                        PlannedLeadershipCosts = 0.0,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, new DateTime(2025, 10, 12), new DateTime(2025, 07, 01)),
                        ProjectStatus = ProjectStatus.AwaitingOutcome,
                        RTP = 265,
                        RequestDocLink = "https://www.google.com",
                        School = School.SSS,
                        StartDate = new DateTime(2025, 07, 01),
                    },
                    new Project
                    {
                        ActualCost = 2202.18,
                        ActualLeadershipCosts = 302.68,
                        ActualWorkHours = 73.5,
                        ActualsLastUpdated = new DateTime(2025, 04, 28, 14, 06, 0).ToString("R"),
                        Budget = 3035.0,
                        CostModel = CostModel.TechAndLeadership,
                        DayRate = 262,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = new DateTime(2025, 03, 20),
                        Faculty = Faculty.FBMH,
                        InnateActivity = GetInnateActivityForRTP(context, 311),
                        LeadershipFTE = 0.025f,
                        Name = "BMBaseDB Update",
                        PI = "Captain Quirk",
                        PlannedCost = 3197.15,
                        PlannedLeadershipCosts = 302.68,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, new DateTime(2025, 03, 20), new DateTime(2025, 01, 13)),
                        ProjectStatus = ProjectStatus.Finished,
                        RTP = 311,
                        RequestDocLink = "https://www.google.com",
                        School = School.SBS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/146",
                        StartDate = new DateTime(2025, 01, 13),
                    },
                    new Project
                    {
                        ActualCost = 3874.95,
                        ActualLeadershipCosts = 280.09,
                        ActualWorkHours = 114.8,
                        ActualsLastUpdated = new DateTime(2025, 06, 11, 09, 09, 0).ToString("R"),
                        Budget = 4963.0,
                        CostModel = CostModel.TechAndLeadership,
                        DayRate = 297,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = new DateTime(2028, 04, 08),
                        Faculty = Faculty.FHUMS,
                        InnateActivity = GetInnateActivityForRTP(context, 323),
                        LeadershipFTE = 0.025f,
                        Name = "Sustainability Trade-off Game Website",
                        PI = "Major Chuckles",
                        PlannedCost = 4633.86,
                        PlannedLeadershipCosts = 280.09,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, new DateTime(2028, 04, 08), new DateTime(2025, 02, 27)),
                        ProjectStatus =ProjectStatus.Paused,
                        RTP = 323,
                        RequestDocLink = "https://www.google.com",
                        School = School.AMBS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/154/views/1?custom_template=33",
                        StartDate = new DateTime(2025, 02, 27),
                    },
                    new Project
                    {
                        ActualCost = 7532.77,
                        ActualLeadershipCosts = 786.06,
                        ActualWorkHours = 177.2,
                        ActualsLastUpdated = new DateTime(2025, 06, 05, 14, 22, 0).ToString("R"),
                        Budget = 12937.81,
                        CostModel = CostModel.TechAndLeadership,
                        DayRate = 297,
                        Description = GetDummyParagraphsAsHtml(),
                        EndDate = new DateTime(2026, 01, 29),
                        Faculty = Faculty.FBMH,
                        InnateActivity = GetInnateActivityForRTP(context, 324),
                        LeadershipFTE = 0.05f,
                        Name = "PAPrKA",
                        PI = "Lady Lollipop",
                        PlannedCost = 12852.1,
                        PlannedLeadershipCosts = 786.06,
                        PlannedWorkHours = 0,
                        ProjectManager = GetRandomManagerActiveDuringDateRange(context, new DateTime(2026, 01, 29), new DateTime(2025, 03, 05)),
                        ProjectStatus = ProjectStatus.Maintenance,
                        RTP = 324,
                        RequestDocLink = "https://www.google.com",
                        School = School.SMS,
                        ScrumProjectLink = "https://github.com/orgs/UoMResearchIT/projects/158",
                        StartDate = new DateTime(2025, 03, 05),
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
                        EndDate = new DateTime(2025, 07, 31),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "Run CoP",
                        OriginalDemand = 0.1,
                        PlannedCost = 0,
                        PlannedWorkHours = 320.6,
                        StartDate = new DateTime(2023, 07, 03),
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
                        EndDate = new DateTime(2025, 07, 31),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE",
                        OriginalDemand = 0.4,
                        PlannedCost = 42123.54,
                        PlannedWorkHours = 1129,
                        StartDate = new DateTime(2023, 10, 02),
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
                        EndDate = new DateTime(2025, 09, 01),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE Support",
                        OriginalDemand = 0.3,
                        PlannedCost = 2987.96,
                        PlannedWorkHours = 78.5,
                        StartDate = new DateTime(2025, 07, 01),
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
                        EndDate = new DateTime(2028, 06, 30),
                        HasFixedEndDate = false,
                        HasFixedStart = false,
                        Lag = 0,
                        Name = "RSE Support (Copy)",
                        OriginalDemand = 0.3,
                        PlannedCost = 50946.21,
                        PlannedWorkHours = 1307.5,
                        StartDate = new DateTime(2025, 09, 02),
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
                        EndDate = new DateTime(2025, 10, 12),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE Build and Deploy Website",
                        OriginalDemand = 0.4,
                        PlannedCost = 7425,
                        PlannedWorkHours = 175,
                        StartDate = new DateTime(2025, 07, 01),
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
                        EndDate = new DateTime(2025, 03, 20),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE",
                        OriginalDemand = 0.4,
                        PlannedCost = 2894.47,
                        PlannedWorkHours = 112,
                        StartDate = new DateTime(2025, 01, 13),
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
                        EndDate = new DateTime(2025, 04, 08),
                        HasFixedEndDate = false,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE",
                        OriginalDemand = 0.3,
                        PlannedCost = 1960.25,
                        PlannedWorkHours = 51.5,
                        StartDate = new DateTime(2025, 02, 27),
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
                        EndDate = new DateTime(2028, 04, 08),
                        HasFixedEndDate = true,
                        HasFixedStart = false,
                        Lag = 365,
                        Name = "Maintenance",
                        OriginalDemand = 0.005,
                        PlannedCost = 584.46,
                        PlannedWorkHours = 15,
                        StartDate = new DateTime(2026, 04, 09),
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
                        EndDate = new DateTime(2025, 06, 08),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 30,
                        Name = "RSE 2",
                        OriginalDemand = 0.8,
                        PlannedCost = 1809.04,
                        PlannedWorkHours = 70,
                        StartDate = new DateTime(2025, 05, 19),
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
                        EndDate = new DateTime(2026, 01, 29),
                        HasFixedEndDate = false,
                        HasFixedStart = false,
                        Lag = 0,
                        Name = "Support",
                        OriginalDemand = 0.1,
                        PlannedCost = 3996.63,
                        PlannedWorkHours = 105,
                        StartDate = new DateTime(2025, 05, 26),
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
                        EndDate = new DateTime(2025, 05, 11),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 2 after break for Agile training",
                        OriginalDemand = 0.4,
                        PlannedCost = 304.50,
                        PlannedWorkHours = 8,
                        StartDate = new DateTime(2025, 05, 05),
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
                        EndDate = new DateTime(2025, 04, 27),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 1",
                        OriginalDemand = 0.4,
                        PlannedCost = 3463.75,
                        PlannedWorkHours = 91,
                        StartDate = new DateTime(2025, 03, 05),
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
                        EndDate = new DateTime(2025, 04, 26),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 2",
                        OriginalDemand = 0.4,
                        PlannedCost = 2550.23,
                        PlannedWorkHours = 67,
                        StartDate = new DateTime(2025, 03, 05),
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
                        EndDate = new DateTime(2025, 05, 31),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 2 Overrun",
                        OriginalDemand = 0.4,
                        PlannedCost = 1065.77,
                        PlannedWorkHours = 28,
                        StartDate = new DateTime(2025, 05, 15),
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
                        EndDate = new DateTime(2025, 05, 25),
                        HasFixedEndDate = true,
                        HasFixedStart = true,
                        Lag = 0,
                        Name = "RSE 1 postponed final week",
                        OriginalDemand = 0.4,
                        PlannedCost = 418.69,
                        PlannedWorkHours = 11,
                        StartDate = new DateTime(2025, 05, 19),
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
                        EndDate = new DateTime(2025, 06, 09),
                        HasFixedEndDate = false,
                        HasFixedStart = false,
                        Lag = 0,
                        Name = "RSE 2 Extension 2",
                        OriginalDemand = 0.2,
                        PlannedCost = 266.44,
                        PlannedWorkHours = 7,
                        StartDate = new DateTime(2025, 06, 01),
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
            using (var context = dbContextFactory.CreateDbContext())
            {

            }
        }

        /// <summary>
        /// Return the first project in DB that matches the RTP given.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rtp"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If there are not projects in the DB that match</exception>
        private static Project GetProjectByRTP(PPMToolContext context, int rtp)
        {
            // Get a project with no funding sources
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
    }
}
