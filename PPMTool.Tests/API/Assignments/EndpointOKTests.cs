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
                // Query requires personNames parameter
                // Use empty string to get data for the API key owner by default
                var response = await client.GetAsync($"/assignments/getAssignments?startDate={GetStartDate()}&endDate={GetEndDate()}");

                // The endpoint can return various status codes depending on parameters:
                // 200 OK if successful
                // 400 Bad Request if parameters are invalid
                // We verify the endpoint is accessible and returns a valid response
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetAssignmentsAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"/assignments/getAssignments?startDate={GetStartDate()}&endDate={GetEndDate()}");

                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
