// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net.Http.Json;
using System.Text.Json;
using PPMTool.Data.Enums;

namespace PPMTool.Tests.API.Projects
{
    [TestFixture]
    public class EndpointOKTests : BaseApiTest
    {
        [Test]
        public async Task GetAllProjectsShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync("projects/getAll");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetProjectByIdShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync($"projects/getById?projectId={ProjectId}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetProjectByIdShouldReturnUpdatableFields()
        {
            // Every field PUT /api/projects/update can write must be readable
            // back, in the form update accepts, or nothing written can be verified.
            using (var client = GetClientAsManager())
            {
                var project = await client.GetFromJsonAsync<JsonElement>($"projects/getById?projectId={ProjectId}");
                Assert.That(project.GetProperty("schoolCode").GetString(), Is.Not.Empty);
                Assert.That(project.GetProperty("budget").ValueKind, Is.EqualTo(JsonValueKind.Number));
                Assert.That(project.GetProperty("dayRate").ValueKind, Is.EqualTo(JsonValueKind.Number));
                Assert.That(Enum.TryParse<CostModel>(project.GetProperty("costModel").GetString(), out _));
                Assert.That(project.GetProperty("description").ValueKind, Is.EqualTo(JsonValueKind.String));
            }
        }

        [Test]
        public async Task GetAllProjectsAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("projects/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
