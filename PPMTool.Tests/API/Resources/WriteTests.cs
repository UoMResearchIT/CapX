// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PPMTool.Tests.API.Resources
{
    [TestFixture]
    public class WriteTests : BaseWriteApiTest
    {
        [Test]
        public async Task AssignmentCanBeAddedThenUpdated()
        {
            var rtp = RunNumber * 10 + 6;
            using (var client = GetClientAsSuperuser())
            {
                var projectId = await CreateProjectAsync(client, rtp);
                var response = await client.PostAsJsonAsync("subtasks/add", new
                {
                    RTP = rtp,
                    Name = "Write Test Resourced Task",
                    TaskDuty = "ProjectWork",
                    StartDate = new DateTime(2030, 1, 1),
                    EndDate = new DateTime(2030, 6, 30),
                    Demand = 0.5
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var subTaskId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("subTaskId").GetInt32();

                response = await client.PostAsJsonAsync("resources/add", new
                {
                    SubTaskId = subTaskId,
                    Resourcing = new[] { new { PersonId = SuperuserPersonId, AssignmentFTE = 0.3 } }
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());

                // A whole project's assignments in one call
                var resources = await client.GetFromJsonAsync<JsonElement>($"resources/getAllForProject?projectId={projectId}");
                Assert.That(resources.GetArrayLength(), Is.EqualTo(1));
                var resource = resources[0];
                Assert.That(resource.GetProperty("subTaskId").GetInt32(), Is.EqualTo(subTaskId));
                Assert.That(resource.GetProperty("personId").GetInt32(), Is.EqualTo(SuperuserPersonId));
                Assert.That(resource.GetProperty("personName").GetString(), Is.EqualTo(SuperuserPersonName));
                Assert.That(resource.GetProperty("usernames").EnumerateArray().Select(x => x.GetString()), Does.Contain(SuperuserUsername));
                Assert.That(resource.GetProperty("assignmentFTE").GetDouble(), Is.EqualTo(0.3));

                var task = await client.GetFromJsonAsync<JsonElement>($"subtasks/getById?subTaskId={subTaskId}");
                Assert.That(task.GetProperty("unmetDemand").GetDouble(), Is.EqualTo(0.2).Within(1e-9));

                var resourceId = resource.GetProperty("resourceId").GetInt32();
                response = await client.PutAsJsonAsync("resources/update", new { ResourceId = resourceId, AssignmentFTE = 0.5, IsProvisional = false });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

                resource = await client.GetFromJsonAsync<JsonElement>($"resources/getById?resourceId={resourceId}");
                Assert.That(resource.GetProperty("assignmentFTE").GetDouble(), Is.EqualTo(0.5));
                Assert.That(resource.GetProperty("isProvisional").GetBoolean(), Is.False);
                task = await client.GetFromJsonAsync<JsonElement>($"subtasks/getById?subTaskId={subTaskId}");
                Assert.That(task.GetProperty("unmetDemand").GetDouble(), Is.EqualTo(0.0).Within(1e-9));
            }
        }

        [Test]
        public async Task ResourcesForProjectNeedsAProjectId()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync("resources/getAllForProject?projectId=0");
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }
    }
}
