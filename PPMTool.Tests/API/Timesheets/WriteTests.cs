// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PPMTool.Tests.API.Timesheets
{
    [TestFixture]
    public class WriteTests : BaseWriteApiTest
    {
        [Test]
        public async Task TimesheetEntryCanBeCreatedThenUpdated()
        {
            var code = $"WTT{RunNumber}";
            // A Monday unique to this run, after the test Superuser's start date
            var week = new DateTime(2021, 1, 4).AddDays(7 * (RunNumber % 500));
            var range = $"startDate={week:yyyy-MM-dd}&endDate={week.AddDays(6):yyyy-MM-dd}&name={ToApiPersonName(SuperuserPersonName)}";

            using (var client = GetClientAsSuperuser())
            {
                var projectId = await CreateProjectAsync(client, RunNumber * 10 + 4, code);

                var response = await client.PostAsJsonAsync("timesheets/add", new
                {
                    PersonId = SuperuserPersonId,
                    ProjectId = projectId,
                    WeekStartDate = week,
                    TaskName = "Development",
                    MondayHours = 7.0,
                    TuesdayHours = 7.0,
                    WednesdayHours = 0.0,
                    ThursdayHours = 0.0,
                    FridayHours = 3.5,
                    SaturdayHours = 0.0,
                    SundayHours = 0.0
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var created = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.That(created.GetProperty("entryCreated").GetBoolean());
                Assert.That(created.GetProperty("totalHours").GetDouble(), Is.EqualTo(17.5));
                Assert.That(created.GetProperty("timesheetStatus").GetString(), Is.EqualTo("Approved"));

                var entry = await GetEntryAsync(client, range, code);
                Assert.That(entry.GetProperty("taskName").GetString(), Is.EqualTo("Development"));
                Assert.That(entry.GetProperty("mondayHours").GetDouble(), Is.EqualTo(7.0));
                Assert.That(entry.GetProperty("fridayHours").GetDouble(), Is.EqualTo(3.5));

                // The same person, week and task again replaces the entry's hours
                response = await client.PostAsJsonAsync("timesheets/add", new
                {
                    PersonId = SuperuserPersonId,
                    ProjectId = projectId,
                    WeekStartDate = week,
                    TaskName = "Development",
                    MondayHours = 1.0,
                    TuesdayHours = 0.0,
                    WednesdayHours = 0.0,
                    ThursdayHours = 0.0,
                    FridayHours = 0.0,
                    SaturdayHours = 0.0,
                    SundayHours = 0.0
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                Assert.That((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("entryCreated").GetBoolean(), Is.False);

                // Move the entry to another task and change one day
                var entryId = entry.GetProperty("timesheetEntryId").GetInt32();
                response = await client.PutAsJsonAsync("timesheets/update", new { TimesheetEntryId = entryId, NewTaskName = "Management", FridayHours = 2.0 });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

                entry = await GetEntryAsync(client, range, code);
                Assert.That(entry.GetProperty("taskName").GetString(), Is.EqualTo("Management"));
                Assert.That(entry.GetProperty("mondayHours").GetDouble(), Is.EqualTo(1.0));
                Assert.That(entry.GetProperty("fridayHours").GetDouble(), Is.EqualTo(2.0));
            }
        }

        /// <summary>
        /// The test Superuser's one entry on the given timesheet code in the given week.
        /// </summary>
        private static async Task<JsonElement> GetEntryAsync(HttpClient client, string range, string code)
        {
            var timesheets = await client.GetFromJsonAsync<JsonElement>($"timesheets/getEntries?{range}");
            return timesheets.EnumerateArray()
                .SelectMany(t => t.GetProperty("entries").EnumerateArray())
                .Single(e => e.GetProperty("innateCode").GetString() == code);
        }
    }
}
