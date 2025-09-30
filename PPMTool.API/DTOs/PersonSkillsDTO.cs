namespace PPMTool.API.DTOs
{
    /// <summary>
    /// DTO simplifying the representation of a skill tag.
    /// </summary>
    public sealed record SkillTagDTO(
        int SkillTagId,
        string Name
    );

    /// <summary>
    /// DTO grouping a person's name with their owned skills.
    /// </summary>
    public sealed record PersonSkillsDTO(
        string Name,
        IEnumerable<SkillTagDTO> Skills
    );
}