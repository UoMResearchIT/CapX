// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.OrgUnits
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllSchoolsAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("schools/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task GetAllFacultiesAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("faculties/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
