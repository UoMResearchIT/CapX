// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net.Http.Json;
using System.Text.Json;

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
                var response = await client.GetAsync($"schools/getById?schoolId={SchoolId}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetSchoolByIdShouldReturnCode()
        {
            // The write endpoints can name a School by code, so reads must return it.
            using (var client = GetClientAsManager())
            {
                var school = await client.GetFromJsonAsync<JsonElement>($"schools/getById?schoolId={SchoolId}");
                Assert.That(school.GetProperty("code").GetString(), Is.Not.Empty);
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
                var response = await client.GetAsync($"faculties/getById?facultyId={FacultyId}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}