// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.WorkloadModels
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
                // Use empty string for personNames to get data for the API key owner by default
                var response = await client.GetAsync($"wlm/getAnalysis?personNames={PersonName}&startDate={GetStartDate()}&endDate={GetEndDate()}");

                // We verify the endpoint is accessible and returns a valid response
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetWorkloadAnalysisDataWithComparisonShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                // Check additional parameters for comparison and normalisation are accepted and return a valid response
                var response = await client.GetAsync($"wlm/getAnalysis?personNames={PersonName}&startDate={GetStartDate()}&endDate={GetEndDate()}&compareToWLM=true&normalisedByTotalHours=true");

                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetAllWorkloadModelChangesShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync("wlm/getAll");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetWorkloadModelChangeByIdShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync(
                    $"wlm/getById?workloadModelChangeId={WorkloadModelChangeId}");

                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetAllWorkloadModelChangesWithDateRangeShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync(
                    "wlm/getAll?startDate=2026-01-01&endDate=2026-12-31");

                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}
