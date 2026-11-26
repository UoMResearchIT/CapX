// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.Skills
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllForSubtaskAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"skills/getAllForSubTask?subTaskId={SubTaskId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task GetAllForProjectAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"skills/getAllForProject?projectId={ProjectId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
