// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PPMTool.Tests.API.SubTasks
{
    [TestFixture]
    public class WriteTests : BaseWriteApiTest
    {
        [Test]
        public async Task SubTaskCanBeCreatedThenUpdated()
        {
            var rtp = RunNumber * 10 + 5;
            using (var client = GetClientAsSuperuser())
            {
                var projectId = await CreateProjectAsync(client, rtp);

                var response = await client.PostAsJsonAsync("subtasks/add", new
                {
                    RTP = rtp,
                    Name = "Write Test Task",
                    TaskDuty = "ProjectWork",
                    StartDate = new DateTime(2030, 1, 1),
                    EndDate = new DateTime(2030, 6, 30),
                    Demand = 0.5
                });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
                var subTaskId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("subTaskId").GetInt32();

                var task = await client.GetFromJsonAsync<JsonElement>($"subtasks/getById?subTaskId={subTaskId}");
                Assert.That(task.GetProperty("owningProjectProjectId").GetInt32(), Is.EqualTo(projectId));
                Assert.That(task.GetProperty("name").GetString(), Is.EqualTo("Write Test Task"));
                Assert.That(task.GetProperty("startDate").GetString(), Is.EqualTo("2030-01-01"));
                Assert.That(task.GetProperty("demand").GetDouble(), Is.EqualTo(0.5));
                Assert.That(task.GetProperty("unmetDemand").GetDouble(), Is.EqualTo(0.5));

                // The duty reads back as its description, which the write accepts too
                var duty = task.GetProperty("taskDuty").GetString();
                response = await client.PutAsJsonAsync("subtasks/update", new { SubTaskId = subTaskId, Name = "Write Test Task Renamed", TaskDuty = duty, Demand = 0.8 });
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

                task = await client.GetFromJsonAsync<JsonElement>($"subtasks/getById?subTaskId={subTaskId}");
                Assert.That(task.GetProperty("name").GetString(), Is.EqualTo("Write Test Task Renamed"));
                Assert.That(task.GetProperty("taskDuty").GetString(), Is.EqualTo(duty));
                Assert.That(task.GetProperty("demand").GetDouble(), Is.EqualTo(0.8));

                var tasks = await client.GetFromJsonAsync<JsonElement>($"subtasks/getAll?projectId={projectId}");
                Assert.That(tasks.EnumerateArray().Select(t => t.GetProperty("subTaskId").GetInt32()), Does.Contain(subTaskId));
            }
        }
    }
}
