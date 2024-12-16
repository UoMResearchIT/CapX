using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Skill endpoint function mapping
/// </summary>
public static class Skills
{
    /// <summary>
    /// Get all skills tags from DB
    /// </summary>
    /// <param name="context"></param>
    /// <param name="tagService"></param>
    /// <returns></returns> 
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillTag>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillTagsAsync(PPMToolContext context, TagService tagService)
    {
        var skillTags = await tagService.GetAllAsync(context);
        Debug.WriteLine($"** API: {skillTags.Count()} skill tags found.");
        return Results.Json(skillTags);
    }

    /// <summary>
    /// Get all skills tags for a person based on their shortname
    /// </summary>
    /// <param name="context"></param>
    /// <param name="tagService"></param>
    /// <param name="shortname"></param>
    /// <returns></returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillTag>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillsTagsForPersonAsync(PPMToolContext context, TagService tagService, string shortname)
    {
        // Try to retrieve the person
        var person = context.People
            .FirstOrDefault(x => x.ShortName.ToLower() == shortname.Trim().ToLower());
        if (person == null)
        {
            return Results.NotFound($"Cannot find a person with short name \"{shortname}\" in the database!");
        }

        // Get the tags for this person
        var tags = await context.SkillTags
            .Include(x => x.People)
            .Where(x => x.People.Any(p => p.PersonId == person.PersonId))
            .ToListAsync();
        Debug.WriteLine($"** API: {tags.Count} skill tags found for {person.Name}.");
        return Results.Json(tags);
    }
}
