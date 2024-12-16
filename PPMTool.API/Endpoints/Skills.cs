using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
    /// <returns>
    /// A list of skills tags in DB
    /// </returns> 
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillTag>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAll(PPMToolContext context, TagService tags)
    {
        var skillTags = await tags.GetAllAsync(context);
        Debug.WriteLine($"** API: {skillTags.Count()} skill tags found.");
        return Results.Json(skillTags);
    }
}
