using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using SkillsDTO = PPMTool.API.DTOs.Skills;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Skill endpoint function mapping
/// </summary>
public static class Skills
{
    /// <summary>
    /// Get all skills tags from DB
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillsDTO.SkillTagDTO>))] // <-- UPDATED
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillTagsAsync(PPMToolContext context, ILogger logger)
    {
        try
        {
            var tags = await context.SkillTags
                .AsNoTracking()
                .ToListAsync();

            if (tags == null || !tags.Any())
            {
                logger.LogWarning("API: GetAllSkillsTags: No tags found!");
                return Results.NotFound();
            }

            // Map the database entities to our new DTOs
            var tagDtos = tags.Select(t => new SkillsDTO.SkillTagDTO(
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillsDTO.SkillTagDTO>))] // <-- UPDATED
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillsTagsForPersonAsync(PPMToolContext context, ILogger logger, string name)
    {
        try
        {
            var person = await context.People
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.Trim().ToLower().Replace("_", " "));

            if (person == null)
            {
                logger.LogWarning("API: GetAllSkillsTagsForPerson: Person = {Name} not found!", name);
                return Results.NotFound();
            }

            var tags = await context.OwnedSkills
                .AsNoTracking()
                .Where(x => x.Owner.PersonId == person.PersonId)
                .Select(x => x.SkillTag) // The query already selects the SkillTag entity
                .ToListAsync();

            // Map the SkillTag entities to our SkillTagDTOs
            var tagDtos = tags.Select(t => new SkillsDTO.SkillTagDTO(
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillsDTO.PersonSkillsDTO>))] // <-- UPDATED
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllPeopleWithSkillTagsAsync(PPMToolContext context, ILogger logger)
    {
        try
        {
            var ownedSkills = await context.OwnedSkills
                .AsNoTracking()
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
                .GroupBy(os => os.Owner)
                .Select(group => new SkillsDTO.PersonSkillsDTO(
                    Name: group.Key.Name,
                    Skills: group.Select(os => new SkillsDTO.SkillTagDTO(
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