using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PPMTool.Data.Enums;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    public class User : ILoggableClass
    {
        public int UserId { get; set; }

        /// <summary>
        /// Type of role the user has
        /// </summary>
        [Required]
        public RoleType RoleType { get; set; }

        /// <summary>
        /// UoM username of the user
        /// </summary>
        [Required]
        public string CASUserName { get; set; }

        /// <summary>
        /// Display name of the user (will get it from the person if there is one)
        /// </summary>
        [Required]
        public string Name { get; set; }

        private Person person;
        /// <summary>
        /// Optional person associated with this user
        /// </summary>
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (person != null)
                    {
                        Name = person.Name;
                    }
                }
            }
        }

        /// <summary>
        /// DateTime when the user last logged in
        /// </summary>
        public string LastLoggedIn { get; set; }

        /// <summary>
        /// Comma separated list of email addresses for this user
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// List of notes this person has authored
        /// </summary>
        [InverseProperty("Author")]
        public virtual ICollection<Note> AuthoredNotes { get; set; } = new List<Note>();

        /// <summary>
        /// List of notes this person has edited
        /// </summary>
        [InverseProperty("Editor")]
        public virtual ICollection<Note> EditedNotes { get; set; } = new List<Note>();

        public string GetSensibleObjectName()
        {
            return $"{GetName()} ({GetStandardisedUserName()})";
        }

        /// <summary>
        /// Method to return the trimmed lowercase instance of the CAS user name
        /// </summary>
        /// <returns></returns>
        public string? GetStandardisedUserName()
        {
            return CASUserName?.Clean();
        }

        /// <summary>
        /// Gets either the name associated with the person or the manually input name for the role if there is no person attached
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return Person?.Name ?? Name;
        }

        /// <summary>
        /// Checks whether this user matches the provided claim name (either by CAS username or email address)
        /// </summary>
        /// <param name="claimName"></param>
        /// <returns></returns>
        public bool MatchesClaim(string claimName)
        {
            var emails = GetNormalisedEmailAddresses();
            return GetStandardisedUserName() == claimName || emails.Contains(claimName);
        }

        /// <summary>
        /// Returns the email addresses for this user as a list, splitting on semicolons and trimming whitespace, and removing any empty entries.
        /// </summary>
        /// <returns></returns>
        public List<string> GetNormalisedEmailAddresses()
        {
            return EmailAddress?
                .Split(';')
                .Select(e => e.Clean())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList() ?? new List<string>();
        }
    }
}
