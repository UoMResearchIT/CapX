using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PPMTool.Data.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// This represents an instance of a skill tag owned by a person
    /// </summary>
    public class OwnedSkill
    {
        /// <summary>
        /// Id of the owned skill
        /// </summary>
        public int OwnedSkillId { get; set; }

        /// <summary>
        /// The owner of the skill tag instance
        /// </summary>
        [Required]
        public virtual Person Owner { get; set; } = null!;

        /// <summary>
        /// Which skill tag this instance refers to
        /// </summary>
        [Required]
        public virtual SkillTag SkillTag { get; set; } = null!;

        /// <summary>
        /// The last time this skill was used "in anger"
        /// </summary>
        [Required]
        public DateTime LastUsed { get; set; }

        /// <summary>
        /// How proficient / experienced is this person with this skill
        /// </summary>
        [Required]
        public SkillProficiency Proficiency { get; set; }

        /// <summary>
        /// Whether this person would like to be considered for opportunities to develop this skill
        /// </summary>
        [Required]
        public bool FavouriteSkill { get; set; }

        /// <summary>
        /// Special un-mapped property to allow binding to ratings control
        /// </summary>
        [NotMapped]
        public int ProficiencyRating
        {
            get => (int)Proficiency;
            set => Proficiency = (SkillProficiency)value;
        }

        /// <summary>
        /// Measure of whether this skill record is complete.
        /// Needs to have a last used date if any kind of proficiency is present.
        /// </summary>
        /// <returns></returns>
        public bool RecordComplete()
        {
            return (Proficiency != SkillProficiency.NotRated && LastUsed != default) || (Proficiency == SkillProficiency.None && FavouriteSkill);
        }
    }
}
