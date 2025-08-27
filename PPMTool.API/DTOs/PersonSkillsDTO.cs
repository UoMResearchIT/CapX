
namespace PPMTool.API.DTOs
{
    /// <summary>
    /// DTOs for returning skills data.
    /// </summary>
    public class Skills
    {
        /// <summary>
        /// A simplified representation of a SkillTag.
        /// </summary>
        public sealed record SkillTagDTO(
            int SkillTagId,
            string Name
        );

        /// <summary>
        /// DTO grouping a person's name with their associated skills.
        /// </summary>
        public sealed record PersonSkillsDTO(
            string Name,
            IEnumerable<SkillTagDTO> Skills
        );
    }
}