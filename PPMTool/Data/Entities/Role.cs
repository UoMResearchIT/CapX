using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class Role : ILoggableClass
    {
        public int RoleId { get; set; }

        [Required]
        public RoleType RoleType { get; set; }

        [Required]
        public string CASUserName { get; set; }

        [Required]
        public string Name { get; set; }

        private Person person;
        public Person Person
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

        public string LastLoggedIn { get; set; }

        [DataType(DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        public string GetSensibleObjectName()
        {
            return $"{GetName()} ({GetStandardisedUserName()})";
        }

        /// <summary>
        /// Method to return the trimmed lowercase instance of the CAS user name
        /// </summary>
        /// <returns></returns>
        internal string GetStandardisedUserName()
        {
            return CASUserName?.Trim().ToLower();
        }

        /// <summary>
        /// Gets either the name associated with the person or the manually input name for the role if there is no person attached
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return Person?.Name ?? Name;
        }
    }
}
