// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.People
{
    [TestFixture]
    public class EndpointOKTests : BaseApiTest
    {
        [Test]
        public async Task GetAllPeopleShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync("people/getAll");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetPersonByIdShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync($"people?personId={PersonId}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

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
