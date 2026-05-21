// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json.Serialization;
using PPMTool.Data.Enums;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    public class SkillTag : ILoggableObject
    {
        public int SkillTagId { get; set; }

        /// <summary>
        /// This is the name of the skill tag as visible to users
        /// </summary>
        [Required]
        public string Name { get; set; } = null!;

        private string controlledName = null!;
        /// <summary>
        /// This is the controlled vocabulary name for the skill tag -- historically it has come from Wikipedia main entries
        /// </summary>
        [Required]
        public string ControlledName
        {
            get => controlledName;
            set
            {
                if (controlledName != value)
                {
                    controlledName = value;
                    HasValidWikiLink = LinkCheckState.Pending;
                }
            }
        }

        /// <summary>
        /// Whether or not the controlled name, when spaces are swapped for underscores, resolves to a wikipedia main entry
        /// </summary>
        [Required]
        public LinkCheckState HasValidWikiLink { get; set; }

        /// <summary>
        /// Instances of this skill tag owned by people (not serialisable to avoid circular references)
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<OwnedSkill>? OwnedSkills { get; set; }

        /// <summary>
        /// Rareness of the skill based on how many people have an owned instance of it
        /// </summary>
        public SkillRareness Rareness { get; set; }

        /// <summary>
        /// Number of people with owned instance of the skill tag
        /// </summary>
        public int RarenessCount { get; set; }

        /// <summary>
        /// The tasks that require this skill
        /// </summary>
        public virtual ICollection<SubTask>? TasksNeedingThisSkill { get; set; }

        /// <summary>
        /// Required override for logging identification
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return $"Skill Tag: {Name}";
        }

        /// <summary>
        /// Generate a wikipedia link for this skill tag
        /// </summary>
        /// <returns></returns>
        public string GetWikiLink()
        {
            return GetWikiLink(ControlledName);
        }

        /// <summary>
        /// Static version of the method
        /// </summary>
        /// <param name="controlledName"></param>
        /// <returns></returns>
        public static string GetWikiLink(string controlledName)
        {
            return $"https://en.wikipedia.org/wiki/{controlledName.Replace(' ', '_')}";
        }

        /// <summary>
        /// Check whether the <see cref="controlledName"/> resolves to a proper Wikipeida link
        /// </summary>
        public async Task<LinkCheckState> UpdateValidLinkAsync()
        {
            var newLinkStatus = LinkCheckState.Pending;
            var url = GetWikiLink();

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    $"CapX/SkillVerification (contact: University of Manchester)"
                );

                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        newLinkStatus = LinkCheckState.Success;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Debug.WriteLine($"** Link validation failed: {url}");
                        newLinkStatus = LinkCheckState.Fail;
                    }
                }
                catch (TaskCanceledException)
                {
                    // Timeout occurred -- should we log somewhere?
                }
                catch (Exception)
                {
                    // Other exceptions
                }
            }

            // Update the link status
            HasValidWikiLink = newLinkStatus;
            return newLinkStatus;
        }

        /// <summary>
        /// Update the rareness for the skill based on how many people have it as an owned skill
        /// </summary>
        /// <param name="count">Number of people who have the skill</param>
        /// <param name="total">Number of people in the department currently active</param>
        /// <returns></returns>
        public void UpdateRareness(int count, int total)
        {
            var rareness = SkillRareness.Common;
            if (count == 0)
            {
                rareness = SkillRareness.Legendary;
            }
            else if (total == 0)
            {
                rareness = SkillRareness.Common;
            }
            else
            {
                var percent = (count / (double)total) * 100;
                if (percent < 5)
                {
                    rareness = SkillRareness.Legendary;
                }
                else if (percent < 10)
                {
                    rareness = SkillRareness.Epic;
                }
                else if (percent < 18)
                {
                    rareness = SkillRareness.Rare;
                }
                else if (percent < 30)
                {
                    rareness = SkillRareness.Uncommon;
                }
            }

            // Set the values in the entity
            Rareness = rareness;
            RarenessCount = count;
        }
    }
}
