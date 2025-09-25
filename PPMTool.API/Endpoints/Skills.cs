using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Skill endpoint function mapping
/// </summary>
public static class Skills
{
    /// <summary>
    /// Get all skills tags from DB
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillTagDTO>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillTagsAsync(PPMToolContext context, ILogger logger)
    {
        try
        {
            var tags = await context.SkillTags
                .ToListAsync();

            if (tags == null || !tags.Any())
            {
                logger.LogWarning("API: GetAllSkillsTags: No tags found!");
                return Results.NotFound();
            }

            // Map the database entities to DTOs
            var tagDtos = tags.Select(t => new SkillTagDTO(
                SkillTagId: t.SkillTagId,
                Name: t.Name
            ));

            logger.LogInformation("API: GetAllSkillsTags: Count = {Count}", tagDtos.Count());
            return Results.Json(tagDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAllSkillsTags error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get all skills tags for a person
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillTagDTO>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillsTagsForPersonAsync(PPMToolContext context, ILogger logger, [FromQuery] string name)
    {
        try
        {
            // Find the person by name
            var person = await APIHelper.FindPersonWithLineManagerByNameAsync(context, name);
            if (person == null)
            {
                logger.LogWarning("API: GetAllSkillsTagsForPerson: Person = {Name} not found!", name);
                return Results.NotFound();
            }

            // Get all SkillTags associated with the person via OwnedSkills
            var tags = await context.OwnedSkills
                .Where(x => x.Owner.PersonId == person.PersonId)
                .Select(x => x.SkillTag)
                .ToListAsync();

            // Map the SkillTag entities to our SkillTagDTOs
            var tagDtos = tags.Select(t => new SkillTagDTO(
                SkillTagId: t.SkillTagId,
                Name: t.Name
            ));

            logger.LogInformation("API: GetAllSkillsTagsForPerson: Person = {Name}, Count = {Count}", person.Name, tagDtos.Count());
            return Results.Json(tagDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAllSkillsTagsForPerson error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get all skills tags grouped by person
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PersonSkillsDTO>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllPeopleWithSkillTagsAsync(PPMToolContext context, ILogger logger)
    {
        try
        {
            // Load all OwnedSkills with related Owner and SkillTag in one query
            var ownedSkills = await context.OwnedSkills
                .Include(x => x.Owner)
                .Include(x => x.SkillTag)
                .ToListAsync();

            if (ownedSkills == null || !ownedSkills.Any())
            {
                logger.LogWarning("API: GetAllPeopleWithSkillTags: No owned skills found!");
                return Results.NotFound();
            }

            // Use LINQ to group and map directly to the new DTO structure
            var results = ownedSkills
                .GroupBy(os => os.Owner.Name)
                .Select(group => new PersonSkillsDTO(
                    Name: group.Key,
                    Skills: group.Select(os => new SkillTagDTO(
                        SkillTagId: os.SkillTag.SkillTagId,
                        Name: os.SkillTag.Name
                    ))
                ))
                .ToList();

            logger.LogInformation("API: GetAllPeopleWithSkillTags: People Count = {Count}", results.Count);
            return Results.Json(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAllPeopleWithSkillTags error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}