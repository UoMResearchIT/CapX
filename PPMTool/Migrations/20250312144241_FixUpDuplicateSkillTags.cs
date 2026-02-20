// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class FixUpDuplicateSkillTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Step 0: Prep the mispelled ones
                UPDATE SkillTags
                SET ControlledName = REPLACE(ControlledName, 'Artifical', 'Artificial')
                WHERE ControlledName LIKE '%Artifical%';

                UPDATE SkillTags
                SET ControlledName = 'Android SDK'
                WHERE ControlledName = 'Android (operating system)';

                UPDATE SkillTags
                SET ControlledName = 'Apache'
                WHERE ControlledName = 'Apache HTTP Server';

                UPDATE SkillTags
                SET ControlledName = '.NET MVC'
                WHERE ControlledName = 'ASP.NET MVC';

                UPDATE SkillTags
                SET ControlledName = 'Atmospheric science'
                WHERE ControlledName = 'Atmospheric chemistry';

                UPDATE SkillTags
                SET ControlledName = '.NET Blazor'
                WHERE ControlledName = 'Blazor';

                UPDATE SkillTags
                SET ControlledName = 'C Sharp (programming language)'
                WHERE ControlledName = 'C#';

                UPDATE SkillTags
                SET ControlledName = 'C++'
                WHERE ControlledName = 'C++/CLI';

                UPDATE SkillTags
                SET ControlledName = 'CSS'
                WHERE ControlledName = 'Cascading Style Sheets';

                UPDATE SkillTags
                SET ControlledName = 'Embedded systems'
                WHERE ControlledName = 'Embedded system';

                UPDATE SkillTags
                SET ControlledName = 'GraphQL'
                WHERE ControlledName = 'Graph database';

                UPDATE SkillTags
                SET ControlledName = 'High-performance computing'
                WHERE ControlledName = 'HPC';

                UPDATE SkillTags
                SET ControlledName = 'Linux'
                WHERE ControlledName = 'Linux SysAdmin';

                UPDATE SkillTags
                SET ControlledName = 'Mathematical model'
                WHERE ControlledName = 'Mathematical modelling';

                UPDATE SkillTags
                SET ControlledName = 'Message Passing Interface'
                WHERE ControlledName = 'Message passing';

                UPDATE SkillTags
                SET ControlledName = 'Mobile app development'
                WHERE ControlledName = 'Mobile Development';

                UPDATE SkillTags
                SET ControlledName = 'Monte Carlo method'
                WHERE ControlledName = 'Monte Carlo methods';

                UPDATE SkillTags
                SET ControlledName = 'Message Passing Interface'
                WHERE ControlledName = 'MPI';

                UPDATE SkillTags
                SET ControlledName = 'Node.js'
                WHERE ControlledName = 'Node js';

                UPDATE SkillTags
                SET ControlledName = 'User interface design'
                WHERE ControlledName = 'User interface';

                UPDATE SkillTags
                SET ControlledName = 'Visual Studio Code'
                WHERE ControlledName = 'VS Code';

                UPDATE SkillTags
                SET ControlledName = 'Web development'
                WHERE ControlledName = 'Web application';

                UPDATE SkillTags
                SET ControlledName = 'Web API'
                WHERE ControlledName = 'Web service';

                UPDATE SkillTags
                SET ControlledName = 'Xamarin'
                WHERE ControlledName = 'Xamarin / .NET MAUI';

                UPDATE SkillTags
                SET ControlledName = 'Xamarin'
                WHERE ControlledName = 'Xamarin.Forms';

                -- Step 1: Create a temporary table to store the entries to keep, including the SkillTagId
                CREATE TEMP TABLE KeepSkillTags AS
                SELECT SkillTagId, ControlledName
                FROM SkillTags
                WHERE ControlledName IN (
                    'AngularJS', 'Artificial Intelligence', 'Data Science', 'Design Patterns', 
                    'Embedded systems', 'GitHub', 'JavaScript', 'Jquery', 'Machine learning', 
                    'Natural Language Processing', 'Software testing', 'Version control', 'Virtual Reality',
                    'Android SDK', 'Apache', '.NET MVC', 'Atmospheric science', '.NET Blazor', 
                    'C Sharp (programming language)', 'C++', 'CSS', 'GraphQL', 'High-performance computing', 
                    'Linux', 'Mathematical model', 'Message Passing Interface', 'Mobile app development', 
                    'Monte Carlo method', 'Node.js', 'User interface design', 'Visual Studio Code', 
                    'Web development', 'Web API', 'Xamarin'
                );

                -- Step 2: Create a second temporary table to store all SkillTagId values for matching ControlledName entries
                CREATE TEMP TABLE MatchSkillTags AS
                SELECT k.SkillTagId AS KeepSkillTagId, s.SkillTagId AS MatchSkillTagId
                FROM KeepSkillTags k
                JOIN SkillTags s ON LOWER(REPLACE(s.ControlledName, ' ', '')) = LOWER(REPLACE(k.ControlledName, ' ', ''));

                -- Step 3: Create a temporary table to copy all rows from PersonSkillTag without unique constraint
                CREATE TEMP TABLE TempPersonSkillTag AS
                SELECT PeoplePersonId, SkillTagsSkillTagId
                FROM PersonSkillTag;

                -- Step 4: Update the TempPersonSkillTag table to reference the kept SkillTagId
                UPDATE TempPersonSkillTag
                SET SkillTagsSkillTagId = (
                    SELECT KeepSkillTagId
                    FROM MatchSkillTags m
                    WHERE m.MatchSkillTagId = TempPersonSkillTag.SkillTagsSkillTagId
                )
                WHERE SkillTagsSkillTagId IN (
                    SELECT MatchSkillTagId
                    FROM MatchSkillTags
                );

                -- Step 5: Remove duplicates from TempPersonSkillTag
                DELETE FROM TempPersonSkillTag
                WHERE rowid NOT IN (
                    SELECT MIN(rowid)
                    FROM TempPersonSkillTag
                    GROUP BY PeoplePersonId, SkillTagsSkillTagId
                );

                -- Step 6: Delete all records from PersonSkillTag
                DELETE FROM PersonSkillTag;

                -- Step 7: Insert the updated records into PersonSkillTag
                INSERT INTO PersonSkillTag (PeoplePersonId, SkillTagsSkillTagId)
                SELECT PeoplePersonId, SkillTagsSkillTagId
                FROM TempPersonSkillTag;

                -- Step 8: Delete the duplicate SkillTags
                DELETE FROM SkillTags
                WHERE SkillTagId NOT IN (
                    SELECT SkillTagsSkillTagId
                    FROM PersonSkillTag
                );

                -- Clean up temporary tables
                DROP TABLE KeepSkillTags;
                DROP TABLE MatchSkillTags;
                DROP TABLE TempPersonSkillTag;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Don't want to reverse this migration
        }
    }
}
