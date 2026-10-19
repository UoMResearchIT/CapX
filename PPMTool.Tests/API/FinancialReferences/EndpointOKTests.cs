// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.FinancialReferences
{
    [TestFixture]
    public class EndpointOKTests : BaseApiTest
    {
        [Test]
        public async Task GetAllFinancialReferencesShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync("financialReferences/getAll");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetFinancialReferenceByFinancialYearShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync(
                    $"financialReferences/getByFinancialYear?financialYear={FinancialYear}");

                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}