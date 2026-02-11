// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

namespace PPMTool.API.Tests.Skills
{
    [TestFixture]
    public class EndpointOKTests
    {
        [Test]
        public async Task GetAllSkillsShouldReturnOK()
        {
            using (var client = Setup.GetClientAsManager())
            {
                var response = await client.GetAsync("/skills/getAll");
                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}