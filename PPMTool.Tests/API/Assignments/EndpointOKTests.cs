// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.Assignments
{
    [TestFixture]
    public class EndpointOKTests : BaseApiTest
    {
        [Test]
        public async Task GetAssignmentDataShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                // Just confirm that the endpoint is reachable and returns a 200 OK status code for a manager
                var response = await client.GetAsync($"assignments/getAssignments?startDate={GetStartDate()}&endDate={GetEndDate()}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

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

        [Test]
        public async Task GetAssignmentDataForSingleProjectShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                // Confirm the optional project filter is accepted for manager calls
                var response = await client.GetAsync($"assignments/getAssignments?startDate={GetStartDate()}&endDate={GetEndDate()}&projectId={ProjectId}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}
