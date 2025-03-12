using System;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

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
        public Person Owner { get; set; }

        /// <summary>
        /// Which skill tag this instance refers to
        /// </summary>
        [Required]
        public SkillTag SkillTag { get; set; }

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
        public bool OpportunityWanted { get; set; }

        /// <summary>
        /// Get the icon name for the emblem for the skill based on how many people have it
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public SkillRareness GetRareness(int count)
        {
            if (count < 3)
            {
                return SkillRareness.Legendary;
            }
            else if (count < 6)
            {
                return SkillRareness.Epic;
            }
            else if (count < 9)
            {
                return SkillRareness.Rare;
            }
            else if (count < 12)
            {
                return SkillRareness.Uncommon;
            }
            else
            {
                return SkillRareness.Common;
            }
        }
    }
}
