using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

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
    /// <param name="logger"></param>
    /// <returns></returns> 
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillTag>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillTagsAsync(PPMToolContext context, ILogger logger)
    {
        try
        {
            var tags = await context.SkillTags.ToListAsync();
            if (tags == null || tags.Count == 0)
            {
                logger.LogWarning($"API: GetAllSkillsTags: No tags found!");
                return Results.NotFound();
            }
            logger.LogInformation($"API: GetAllSkillsTags: Count = {tags?.Count}");
            return Results.Json(tags);
        }
        catch (Exception ex)
        {
            logger.LogError($"API: GetAllSkillsTags: {ex}");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get all skills tags grouped by person
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <returns></returns> 
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, IEnumerable<OwnedSkill>>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllPeopleWithSkillTagsAsync(PPMToolContext context, ILogger logger)
    {
        try
        {
            var people = await context.People.Include(x => x.OwnedSkills).ToListAsync();
            if (people == null || people.Count == 0)
            {
                logger.LogWarning($"API: GetAllPeopleWithSkillTags: No people found!");
                return Results.NotFound();
            }

            // Assemble into correct form
            Dictionary<string, IEnumerable<OwnedSkill>> results = new Dictionary<string, IEnumerable<OwnedSkill>>();
            foreach (var person in people)
            {
                results.Add(person.Name, person.OwnedSkills);
            }

            logger.LogInformation($"API: GetAllPeopleWithSkillTags: Count = {people.Count}");
            return Results.Json(results);
        }
        catch (Exception ex)
        {
            logger.LogError($"API: GetAllPeopleWithSkillTags: {ex}");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }


    }

    /// <summary>
    /// Get all skills tags for a person based on their name with spaces between their names replaced with underscores
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OwnedSkill>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillsTagsForPersonAsync(PPMToolContext context, ILogger logger, string name)
    {
        try
        {
            // Try to retrieve the person
            var person = context.People
            .FirstOrDefault(x => x.Name.ToLower() == name.Trim().ToLower().Replace("_", " "));
            if (person == null)
            {
                logger.LogWarning($"API: GetAllSkillsTagsForPerson: Person = {name} not found in the DB!");
                return Results.NotFound();
            }

            // Get the tags for this person
            var tags = await context.OwnedSkills
                .Include(x => x.Owner)
                .Where(x => x.Owner.PersonId == person.PersonId)
                .ToListAsync();
            logger.LogInformation($"API: GetAllSkillsTagsForPerson: Person = {person.Name}, Count = {tags.Count}");
            return Results.Json(tags);
        }
        catch (Exception ex)
        {
            logger.LogError($"API: GetAllSkillsTagsForPerson: {ex}");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}