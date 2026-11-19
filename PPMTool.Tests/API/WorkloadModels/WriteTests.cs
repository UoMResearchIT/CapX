// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PPMTool.Tests.API.WorkloadModels
{
    [TestFixture]
    public class WriteTests : BaseWriteApiTest
    {
        [Test]
        public async Task WorkloadModelChangeCanBeCreatedThenUpdated()
        {
            // A change date unique to this run, since the endpoint upserts on username and date
            var changeDate = new DateTime(2031, 1, 1).AddDays(RunNumber % 3000);

            using (var client = GetClientAsSuperuser())
            {
                var response = await client.PostAsJsonAsync("workloadmodels/add", Change(changeDate, projectWork: 0.8, businessAsUsual: 0.2));
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var created = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.That(created.GetProperty("created").GetBoolean());
                var changeId = created.GetProperty("workloadModelChangeId").GetInt32();

                var change = await client.GetFromJsonAsync<JsonElement>($"wlm/getById?workloadModelChangeId={changeId}");
                Assert.That(change.GetProperty("personId").GetInt32(), Is.EqualTo(SuperuserPersonId));
                Assert.That(change.GetProperty("changeDate").GetString(), Is.EqualTo(changeDate.ToString("yyyy-MM-dd")));
                Assert.That(change.GetProperty("projectWorkFTE").GetDouble(), Is.EqualTo(0.8));

                // The same person and date again updates that change
                response = await client.PostAsJsonAsync("workloadmodels/add", Change(changeDate, projectWork: 0.5, businessAsUsual: 0.5));
                Assert.That(response.StatusCode, Is.AnyOf(HttpStatusCode.OK, HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.That(updated.GetProperty("created").GetBoolean(), Is.False);
                Assert.That(updated.GetProperty("workloadModelChangeId").GetInt32(), Is.EqualTo(changeId));

                change = await client.GetFromJsonAsync<JsonElement>($"wlm/getById?workloadModelChangeId={changeId}");
                Assert.That(change.GetProperty("projectWorkFTE").GetDouble(), Is.EqualTo(0.5));
                Assert.That(change.GetProperty("businessAsUsualFTE").GetDouble(), Is.EqualTo(0.5));
            }
        }

        private static object Change(DateTime changeDate, double projectWork, double businessAsUsual) => new
        {
            Username = SuperuserUsername,
            ChangeDate = changeDate,
            Grade = 6,
            ProjectWorkFTE = projectWork,
            BusinessAsUsualFTE = businessAsUsual,
            PersonalDevelopmentFTE = 0.0,
            StaffManagementFTE = 0.0,
            ArchitectureFTE = 0.0,
            ServiceManagementFTE = 0.0,
            ProjectManagementFTE = 0.0,
            Notes = "Write API tests"
        };
    }
}
