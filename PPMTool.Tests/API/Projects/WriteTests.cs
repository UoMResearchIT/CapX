// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PPMTool.Tests.API.Projects
{
    [TestFixture]
    public class WriteTests : BaseWriteApiTest
    {
        // The fallback author projects/notes/add needs to exist before it adds any comment
        private const string fallbackAuthorUsername = "migration-import";

        [Test]
        public async Task ProjectCanBeCreatedThenUpdated()
        {
            var rtp = RunNumber * 10 + 1;
            using (var client = GetClientAsSuperuser())
            {
                var projectId = await CreateProjectAsync(client, rtp, $"WTP{RunNumber}");

                var project = await client.GetFromJsonAsync<JsonElement>($"projects/getById?projectId={projectId}");
                Assert.That(project.GetProperty("capXProjectId").GetInt32(), Is.EqualTo(rtp));
                Assert.That(project.GetProperty("schoolId").GetInt32(), Is.EqualTo(SchoolId));
                Assert.That(project.GetProperty("budget").GetDouble(), Is.EqualTo(1000.0));
                Assert.That(project.GetProperty("timesheetActivityCode").GetString(), Is.EqualTo($"WTP{RunNumber}"));

                var response = await client.PutAsJsonAsync("projects/update", new
                {
                    RTP = rtp,
                    Budget = 2500.0,
                    ProjectStatus = "Paused",
                    CostModel = "DayRate",
                    DayRate = 450.0,
                    Description = "<p>Updated by the write API tests</p>"
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

                project = await client.GetFromJsonAsync<JsonElement>($"projects/getById?projectId={projectId}");
                Assert.That(project.GetProperty("budget").GetDouble(), Is.EqualTo(2500.0));
                Assert.That(project.GetProperty("projectStatus").GetString(), Is.EqualTo("Paused"));
                Assert.That(project.GetProperty("costModel").GetString(), Is.EqualTo("Uses a specific day rate"));
                Assert.That(project.GetProperty("dayRate").GetDouble(), Is.EqualTo(450.0));
                Assert.That(project.GetProperty("description").GetString(), Is.EqualTo("<p>Updated by the write API tests</p>"));
            }
        }

        [Test]
        public async Task ProjectReadCanBeWrittenBackUnchanged()
        {
            // Everything projects/update writes must read back in a form it accepts
            var rtp = RunNumber * 10 + 2;
            using (var client = GetClientAsSuperuser())
            {
                var projectId = await CreateProjectAsync(client, rtp);
                var before = await client.GetFromJsonAsync<JsonElement>($"projects/getById?projectId={projectId}");

                var response = await client.PutAsJsonAsync("projects/update", new
                {
                    RTP = rtp,
                    Name = before.GetProperty("name").GetString(),
                    PI = before.GetProperty("principalInvestigator").GetString(),
                    SchoolId = before.GetProperty("schoolId").GetInt32(),
                    Budget = before.GetProperty("budget").GetDouble(),
                    CostModel = before.GetProperty("costModel").GetString(),
                    DayRate = before.GetProperty("dayRate").GetDouble(),
                    Description = before.GetProperty("description").GetString(),
                    RequestDocLink = before.GetProperty("requestDocLink").GetString()
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

                var after = await client.GetFromJsonAsync<JsonElement>($"projects/getById?projectId={projectId}");
                Assert.That(after.GetRawText(), Is.EqualTo(before.GetRawText()));
            }
        }

        [Test]
        public async Task NotesCanBeAddedThenUpdated()
        {
            var rtp = RunNumber * 10 + 3;
            using (var client = GetClientAsSuperuser())
            {
                await CreateProjectAsync(client, rtp);

                // A 400 is acceptable only when an earlier run already created it.
                var response = await client.PostAsJsonAsync("users/add", new
                {
                    CASUserName = fallbackAuthorUsername,
                    EmailAddress = $"{fallbackAuthorUsername}@example.invalid",
                    Name = "Migration Import",
                    RoleType = "None"
                });
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("already exists"));
                }
                else
                {
                    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                }

                response = await client.PostAsJsonAsync("projects/notes/add", new
                {
                    CapXProjectId = rtp,
                    Comments = new[]
                    {
                        new { AuthorUsername = (string?)SuperuserUsername, AuthorDisplayName = SuperuserPersonName, ContentHtml = "<p>First note</p>", CreatedDate = new DateTime(2025, 2, 1) },
                        new { AuthorUsername = (string?)null, AuthorDisplayName = "Someone Unknown", ContentHtml = "<p>Second note</p>", CreatedDate = new DateTime(2025, 2, 2) }
                    }
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());

                var notes = await client.GetFromJsonAsync<JsonElement>($"projects/notes/getAll?capxProjectId={rtp}");
                Assert.That(notes.GetArrayLength(), Is.EqualTo(2));
                var first = notes.EnumerateArray().First(n => n.GetProperty("htmlContent").GetString()!.Contains("First note"));
                var second = notes.EnumerateArray().First(n => n.GetProperty("htmlContent").GetString()!.Contains("Second note"));
                Assert.That(first.GetProperty("capXProjectId").GetInt32(), Is.EqualTo(rtp));
                Assert.That(second.GetProperty("capXProjectId").GetInt32(), Is.EqualTo(rtp));
                Assert.That(first.GetProperty("authorUsername").GetString(), Is.EqualTo(SuperuserUsername));
                Assert.That(second.GetProperty("authorUsername").GetString(), Is.EqualTo(fallbackAuthorUsername));

                var noteId = first.GetProperty("noteId").GetInt32();
                response = await client.PutAsJsonAsync("projects/notes/update", new { NoteId = noteId, HtmlContent = "<p>First note, edited</p>" });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

                notes = await client.GetFromJsonAsync<JsonElement>($"projects/notes/getAll?capxProjectId={rtp}");
                var edited = notes.EnumerateArray().First(n => n.GetProperty("noteId").GetInt32() == noteId);
                Assert.That(edited.GetProperty("htmlContent").GetString(), Does.Contain("First note, edited"));
            }
        }
    }
}
