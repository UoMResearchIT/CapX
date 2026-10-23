// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.OrgUnits
{
    [TestFixture]
    public class EndpointOKTests : BaseApiTest
    {
        [Test]
        public async Task GetAllSchoolsShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync("schools/getAll");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetSchoolByIdShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync($"schools?schoolId={SchoolId}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetAllFacultiesShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync("faculties/getAll");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetFacultyByIdShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync($"faculties?facultyId={FacultyId}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}