// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.People
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllPeopleAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("people/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
