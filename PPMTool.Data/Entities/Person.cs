using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an RSE available for project work
    /// </summary>
    public class Person : ObjectWithStatusMessages, IComparable
    {
        public int PersonId { get; set; }

        private string name;
        /// <summary>
        /// Name of the person
        /// </summary>
        [Required]
        public string Name
        {
            get => name;
            set { name = value; ShortName = GetInitials(value); }
        }

        /// <summary>
        /// Initials of the person -- auto populated but can be edited
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// When they started in post
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; } = DateTime.Today;

        /// <summary>
        /// When they left. Null if still in post.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// FTE of the post
        /// </summary>
        [Required]
        public double FTE { get; set; } = 1.0;

        /// <summary>
        /// Line manager of this person
        /// </summary>
        [JsonIgnore]
        public virtual Person LineManager { get; set; }

        /// <summary>
        /// Pipe-separated list of timesheet tasks that represent the person's timesheet template
        /// </summary>
        public string TimesheetTemplateData { get; set; }

        /// <summary>
        /// Any changes to their WLMs
        /// </summary>
        public virtual ICollection<WorkloadModelChange> WorkloadModelChanges { get; set; } = new List<WorkloadModelChange>();

        /// <summary>
        /// Collection of skill tag instances owned by this person
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<OwnedSkill> OwnedSkills { get; set; } = new List<OwnedSkill>();

        /// <summary>
        /// Collection of absences
        /// </summary>
        public virtual ICollection<Absence> Absences { get; set; } = new List<Absence>();

        /// <summary>
        /// List of projects this person is following
        /// </summary>
        [InverseProperty("Followers")]
        public virtual ICollection<Project> FollowedProjects { get; set; } = new List<Project>();

        /// <summary>
        /// List of projects this person manages
        /// </summary>
        [InverseProperty("ProjectManager")]
        public virtual ICollection<Project> ManagedProjects { get; set; } = new List<Project>();

        /// <summary>
        /// List of the competency assessments this person has performed
        /// </summary>
        public virtual ICollection<CompetencyAssessment> Assessments { get; set; } = new List<CompetencyAssessment>();

        /// <summary>
        /// The collection of Timesheets this person owns
        /// </summary>
        [InverseProperty("Owner")]
        public virtual ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();

        /// <summary>
        /// The collection of Timesheets this person was last to change the status of
        /// </summary>
        [InverseProperty("StatusChangedBy")]
        public virtual ICollection<Timesheet> TimesheetsChanged { get; set; } = new List<Timesheet>();

        /// <summary>
        /// List of people that this person manages as their line manager
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Person> PeopleManaged { get; set; } = new List<Person>();

        public Person()
        {
            // Generate status messages to be maintained against a person
            statusMessages = new List<StatusMessage>
            {
                new StatusMessage("This person is currently absent.", StatusMessage.MessageType.Info, IsCurrentlyAbsent)
            };
        }

        /// <summary>
        /// Checks whether this person is currently absent.
        /// </summary>
        /// <returns></returns>
        public bool IsCurrentlyAbsent()
        {
            return Absences.Any(x => x.IsCurrentAbsence());
        }

        /// <summary>
        /// Updates the initials of the person.
        /// </summary>
        /// <param name="name">Full name</param>
        /// <returns></returns>
        static string GetInitials(string name)
        {
            string[] nameSplit = name.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            string initials = "";
            foreach (string item in nameSplit)
            {
                initials += item.Substring(0, 1).ToUpper();
            }
            return initials;
        }

        /// <summary>
        /// Get the availability of the person on the current date from their workload model changes profile
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal double GetProjectWorkAvailabilityOnDate(DateTime date)
        {
            // If person hasn't started on day then return zero
            if (StartDate > date) return 0;

            // If the person has left before this day then return zero
            if (EndDate != null && EndDate < date) return 0;

            // If the person has no workload model changes then assume default G6 model
            var wlm = GetWorkloadModelOnDateOrDefault(date);
            return wlm.ProjectWorkFTE;
        }

        /// <summary>
        /// Method to get the current workload model of a person in force at the date given or defaults to a G6 model
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal WorkloadModelChange GetWorkloadModelOnDateOrDefault(DateTime date)
        {
            // Get the workload model that is active at the beginning of the week
            var activeModel = WorkloadModelChanges
                .Where(x => x.ChangeDate <= date)
                .OrderBy(x => x.ChangeDate)
                .LastOrDefault();

            // If no workload model active then default to the standard 100% project work model
            if (activeModel == null)
            {
                activeModel = new WorkloadModelChange()
                {
                    ChangeDate = date,
                    Person = this,
                    ProjectWorkFTE = 0.8,
                    BusinessAsUsualFTE = 0.1,
                    PersonalDevelopmentFTE = 0.1,
                    Notes = "Default G6 Model",
                    Grade = 6
                };
            }

            return activeModel;
        }

        /// <summary>
        /// Is the current staff member a current staff member at the moment
        /// </summary>
        /// <returns></returns>
        public bool IsCurrentStaff()
        {
            return StartDate <= DateTime.Today && (EndDate == null || EndDate > DateTime.Today);
        }

        /// <summary>
        /// Default comparer for person objects compares name
        /// </summary>
        /// <param name="obj">Other person</param>
        /// <returns></returns>
        public int CompareTo(object? obj)
        {
            return Name.CompareTo((obj as Person)?.Name);
        }

        /// <summary>
        /// Method to return PM capacity for a person based on the WLM active on the provided date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal double GetProjectManagementCapacityOnDate(DateTime date)
        {
            // If person hasn't started on day then return zero
            if (StartDate > date) return 0;

            // If the person has left before this day then return zero
            if (EndDate != null && EndDate < date) return 0;

            // Get WLM in play on date
            var wlm = GetWorkloadModelOnDateOrDefault(date);
            return wlm.ProjectManagementFTE;
        }

        /// <summary>
        /// Method to return the total workload model FTE for a person on the provided date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal double GetWorkloadModelTotalOnDate(DateTime date)
        {
            // If person hasn't started on day then return zero
            if (StartDate > date) return 0;

            // If the person has left before this day then return zero
            if (EndDate != null && EndDate < date) return 0;

            // Get WLM in play on date
            var wlm = GetWorkloadModelOnDateOrDefault(date);
            return wlm.Total();
        }

        /// <summary>
        /// Method to return the grade of the person on the date or the default of 6 if no WLM to consider.
        /// Returns null if not started or left.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal int? GetGradeOnDate(DateTime date)
        {
            // If person hasn't started on day then return null
            if (StartDate > date) return null;

            // If the person has left before this day then return null
            if (EndDate != null && EndDate < date) return null;

            return GetWorkloadModelOnDateOrDefault(date).Grade;
        }

        /// <summary>
        /// Gets the first workload model change on or after the date given
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal WorkloadModelChange GetFirstWorkloadModelAfter(DateTime date)
        {
            return WorkloadModelChanges
                .Where(x => x.ChangeDate >= date)
                .OrderBy(x => x.ChangeDate)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the most recent workload model change on or before the date given
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal WorkloadModelChange GetLastWorkloadModelBefore(DateTime date)
        {
            return WorkloadModelChanges
                .Where(x => x.ChangeDate <= date)
                .OrderBy(x => x.ChangeDate)
                .LastOrDefault();
        }
    }
}
