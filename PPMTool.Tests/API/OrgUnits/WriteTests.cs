// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PPMTool.Tests.API.OrgUnits
{
    [TestFixture]
    public class WriteTests : BaseWriteApiTest
    {
        [Test]
        public async Task WritesNeedASuperuserKey()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.PostAsJsonAsync("faculties/add", new { Name = $"Refused Faculty {RunId}", Code = $"RF{RunId}" });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            }
        }

        [Test]
        public async Task FacultyAndSchoolCanBeCreatedThenUpdated()
        {
            var facultyCode = $"WTF{RunId}";
            var schoolCode = $"WTS{RunId}";

            using (var client = GetClientAsSuperuser())
            {
                // Create a faculty with one school, and read both back
                var response = await client.PostAsJsonAsync("faculties/add", new
                {
                    Name = $"Write Test Faculty {RunId}",
                    Code = facultyCode,
                    Schools = new[] { new { Name = $"Write Test School {RunId}", Code = schoolCode } }
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var created = await response.Content.ReadFromJsonAsync<JsonElement>();
                var facultyId = created.GetProperty("facultyId").GetInt32();
                var schoolId = created.GetProperty("schoolIds")[0].GetInt32();

                var school = await client.GetFromJsonAsync<JsonElement>($"schools/getById?schoolId={schoolId}");
                Assert.That(school.GetProperty("code").GetString(), Is.EqualTo(schoolCode));
                Assert.That(school.GetProperty("facultyId").GetInt32(), Is.EqualTo(facultyId));

                // Rename the faculty
                response = await client.PutAsJsonAsync("faculties/update", new { Code = facultyCode, Name = $"Write Test Faculty {RunId} Renamed" });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                var faculty = await client.GetFromJsonAsync<JsonElement>($"faculties/getById?facultyId={facultyId}");
                Assert.That(faculty.GetProperty("name").GetString(), Is.EqualTo($"Write Test Faculty {RunId} Renamed"));

                // Rename the school and change its code
                response = await client.PutAsJsonAsync("schools/update", new { Code = schoolCode, Name = $"Write Test School {RunId} Renamed", NewCode = $"{schoolCode}X" });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                school = await client.GetFromJsonAsync<JsonElement>($"schools/getById?schoolId={schoolId}");
                Assert.That(school.GetProperty("name").GetString(), Is.EqualTo($"Write Test School {RunId} Renamed"));
                Assert.That(school.GetProperty("code").GetString(), Is.EqualTo($"{schoolCode}X"));
            }
        }

        [Test]
        public async Task SchoolCodeCannotBeReusedInAnotherFaculty()
        {
            using (var client = GetClientAsSuperuser())
            {
                var response = await client.PostAsJsonAsync("faculties/add", new
                {
                    Name = $"Code Clash Faculty {RunId}",
                    Code = $"CCF{RunId}",
                    Schools = new[] { new { Name = $"Code Clash School {RunId}", Code = $"CCS{RunId}" } }
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());

                response = await client.PostAsJsonAsync("faculties/add", new
                {
                    Name = $"Code Clash Faculty Two {RunId}",
                    Code = $"CCG{RunId}",
                    Schools = new[] { new { Name = $"Code Clash School Two {RunId}", Code = $"CCS{RunId}" } }
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }
    }
}
