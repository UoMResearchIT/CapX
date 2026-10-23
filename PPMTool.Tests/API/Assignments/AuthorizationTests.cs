// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.Assignments
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAssignmentsAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                // Just confirm that the endpoint is reachable and returns a 401 Unauthorized status code for a non-manager
                var response = await client.GetAsync($"assignments/getAssignments?startDate={GetStartDate()}&endDate={GetEndDate()}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
