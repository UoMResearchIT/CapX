// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PPMTool.Tests.API.Users
{
    [TestFixture]
    public class WriteTests : BaseWriteApiTest
    {
        [Test]
        public async Task UserCanBeCreatedForAPerson()
        {
            var username = $"writetestuser{RunId}";
            using (var client = GetClientAsSuperuser())
            {
                var response = await client.PostAsJsonAsync("people/add", new
                {
                    Name = $"Write Test User Person {RunId}",
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = (DateTime?)null,
                    FTE = 1.0
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var personId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("personId").GetInt32();

                response = await client.PostAsJsonAsync("users/add", new
                {
                    CASUserName = username,
                    EmailAddress = $"{username}@example.invalid",
                    PersonId = personId,
                    RoleType = "Developer"
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());

                var person = await client.GetFromJsonAsync<JsonElement>($"people/getById?personId={personId}");
                Assert.That(person.GetProperty("usernames").EnumerateArray().Select(x => x.GetString()), Is.EquivalentTo(new[] { username }));

                // The same username again is refused
                response = await client.PostAsJsonAsync("users/add", new
                {
                    CASUserName = username,
                    EmailAddress = $"{username}@example.invalid",
                    PersonId = personId,
                    RoleType = "Developer"
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }
    }
}
