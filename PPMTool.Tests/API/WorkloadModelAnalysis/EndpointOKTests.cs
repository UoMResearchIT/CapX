// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.WorkloadModelAnalysis
{
    [TestFixture]
    public class EndpointOKTests : BaseApiTest
    {
        [Test]
        public async Task GetWorkloadAnalysisDataShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                // Query requires personNames parameter
                // Use empty string to get data for the API key owner by default
                var response = await client.GetAsync($"/wlm/getAnalysis?personNames=&startDate={GetStartDate()}&endDate={GetEndDate()}");

                // The endpoint can return various status codes depending on parameters:
                // 200 OK if successful
                // 400 Bad Request if parameters are invalid
                // We verify the endpoint is accessible and returns a valid response
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetWorkloadAnalysisDataWithComparisonShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync($"/wlm/getAnalysis?personNames=&startDate={GetStartDate()}&endDate={GetEndDate()}&compareToWLM=true&normalisedByTotalHours=true");

                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}
