using PPMTool.Data.Entities;

namespace PPMTool.Api.Records;

internal record Skill
{
    public string Name { get; init; }
    public int SkillTagId { get; init; }

    public Skill(SkillTag tag)
    {
        Name = tag.Name;
        SkillTagId = tag.SkillTagId;
    }
}
