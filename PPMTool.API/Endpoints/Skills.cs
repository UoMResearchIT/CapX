// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using Microsoft.AspNetCore.Mvc;
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
        var tags = await context.SkillTags.ToListAsync();
        logger.LogInformation($"API: GetAllSkillsTags: Count = {tags.Count}");
        return Results.Json(tags);
    }

    /// <summary>
    /// Get all skills tags grouped by person
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <returns></returns> 
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, IEnumerable<SkillTag>>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllPeopleWithSkillTagsAsync(PPMToolContext context, ILogger logger)
    {
        var people = await context.People.Include(x => x.SkillTags).ToListAsync();

        // Assemble into correct form
        Dictionary<string, IEnumerable<SkillTag>> results = new Dictionary<string, IEnumerable<SkillTag>>();
        foreach (var person in people)
        {
            results.Add(person.Name, person.SkillTags);
        }

        logger.LogInformation($"API: GetAllPeopleWithSkillTags: Count = {people.Count}");
        return Results.Json(results);
    }

    /// <summary>
    /// Get all skills tags for a person based on their name with spaces between their names replaced with underscores
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SkillTag>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetAllSkillsTagsForPersonAsync(PPMToolContext context, ILogger logger, string name)
    {
        // Try to retrieve the person
        var person = context.People
            .FirstOrDefault(x => x.Name.ToLower() == name.Trim().ToLower().Replace("_", " "));
        if (person == null)
        {
            logger.LogWarning($"API: GetAllSkillsTagsForPerson: Person = {name} not found in the DB!");
            return Results.NotFound($"Cannot find a {name} in the database!");
        }

        // Get the tags for this person
        var tags = await context.SkillTags
            .Include(x => x.People)
            .Where(x => x.People.Any(p => p.PersonId == person.PersonId))
            .ToListAsync();
        logger.LogInformation($"API: GetAllSkillsTagsForPerson: Person = {person.Name}, Count = {tags.Count}");
        return Results.Json(tags);
    }
}