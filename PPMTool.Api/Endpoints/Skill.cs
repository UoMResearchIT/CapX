using Microsoft.AspNetCore.Mvc;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Api.Endpoints;

/// <summary>
/// Skill endpoint function mapping
/// </summary>
public static class Skill
{
    /// <summary>
    /// Get all distinct skills 
    /// </summary>
    /// <returns>
    /// A list of distinct skills
    /// </returns> 
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Records.Skill>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAll(PPMToolContext ctx, TagService tags)
    {
        tags.Add(ctx, new SkillTag { Name = "C#" });
        tags.Add(ctx, new SkillTag { Name = "Java" });
        tags.Add(ctx, new SkillTag { Name = "Python" });

        var skillTags = await tags.GetAllAsync(ctx);
        var uniqueSkillTags = skillTags
            .GroupBy(st => st.Name)
            .Select(g => g.First())
            .Select(st => new Records.Skill(st))
            .ToList();

        return Results.Json(uniqueSkillTags);
    }
}
