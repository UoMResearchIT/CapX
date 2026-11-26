// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.SubTasks
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllSubTasksAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"subTasks/getAll?projectId={ProjectId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task GetSubTaskByIdAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"subTasks/getById?subTaskId={SubTaskId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
