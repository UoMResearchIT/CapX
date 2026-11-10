// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.Projects
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllProjectsAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("projects/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task GetByProjectIdAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"projects/getById?projectId={ProjectId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task GetByCapXProjectIdAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"projects/getByCapXProjectId?capxProjectId={RtpId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
