// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PPMTool.Tests.API.People
{
    [TestFixture]
    public class WriteTests : BaseWriteApiTest
    {
        [Test]
        public async Task PersonCanBeCreatedThenUpdated()
        {
            using (var client = GetClientAsSuperuser())
            {
                var response = await client.PostAsJsonAsync("people/add", new
                {
                    Name = $"Write Test Person {RunId}",
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = (DateTime?)null,
                    FTE = 1.0
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var personId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personId").GetInt32();

                var person = await client.GetFromJsonAsync<JsonElement>($"people/getById?personId={personId}");
                Assert.That(person.GetProperty("name").GetString(), Is.EqualTo($"Write Test Person {RunId}"));
                Assert.That(person.GetProperty("startDate").GetString(), Is.EqualTo("2024-01-01"));
                Assert.That(person.GetProperty("usernames").GetArrayLength(), Is.EqualTo(0));

                response = await client.PutAsJsonAsync("people/update", new
                {
                    PersonId = personId,
                    FTE = 0.5,
                    EndDate = new DateTime(2026, 6, 30),
                    LineManagerPersonId = SuperuserPersonId
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

                person = await client.GetFromJsonAsync<JsonElement>($"people/getById?personId={personId}");
                Assert.That(person.GetProperty("postFTE").GetDouble(), Is.EqualTo(0.5));
                Assert.That(person.GetProperty("endDate").GetString(), Is.EqualTo("2026-06-30"));
                Assert.That(person.GetProperty("lineManagerId").GetInt32(), Is.EqualTo(SuperuserPersonId));
            }
        }

        [Test]
        public async Task LineManagerUsernameMustLeadToAPerson()
        {
            using (var client = GetClientAsSuperuser())
            {
                var response = await client.PostAsJsonAsync("people/add", new
                {
                    Name = $"Managed Person {RunId}",
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = (DateTime?)null,
                    FTE = 1.0
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var personId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personId").GetInt32();

                // The primary form, by PersonId
                response = await client.PutAsJsonAsync("people/update", new { PersonId = personId, LineManagerPersonId = SuperuserPersonId });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

                // The username form, for a User with a linked Person
                response = await client.PutAsJsonAsync("people/update", new { PersonId = personId, LineManagerUsername = SuperuserUsername });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                var person = await client.GetFromJsonAsync<JsonElement>($"people/getById?personId={personId}");
                Assert.That(person.GetProperty("lineManagerId").GetInt32(), Is.EqualTo(SuperuserPersonId));

                // A username matching no User is a 400 saying so
                response = await client.PutAsJsonAsync("people/update", new { PersonId = personId, LineManagerUsername = $"nosuchuser{RunId}" });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("does not match any User"));

                // Both forms at once are refused
                response = await client.PutAsJsonAsync("people/update", new { PersonId = personId, LineManagerPersonId = SuperuserPersonId, LineManagerUsername = SuperuserUsername });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task PersonCannotManageThemselves()
        {
            using (var client = GetClientAsSuperuser())
            {
                var response = await client.PostAsJsonAsync("people/add", new
                {
                    Name = $"Self Managed Person {RunId}",
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = (DateTime?)null,
                    FTE = 1.0
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var personId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personId").GetInt32();

                response = await client.PutAsJsonAsync("people/update", new { PersonId = personId, LineManagerPersonId = personId });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }
    }
}
