// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.WorkloadModels
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetAllWorkloadModelChangesAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync("wlm/getAll");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task GetWorkloadModelChangeByIdAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"wlm/getById?workloadModelChangeId={WorkloadModelChangeId}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task GetWorkloadAnalysisDataForManagerReportAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"wlm/getAnalysis?personNames={AuthorisedPersonNameForManager}&startDate={GetStartDate()}&endDate={GetEndDate()}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
