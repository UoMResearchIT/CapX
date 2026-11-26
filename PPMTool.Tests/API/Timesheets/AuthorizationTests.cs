// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.Timesheets
{
    public class AuthorizationTests : BaseApiTest
    {
        [Test]
        public async Task GetTimesheetEntriesForManagerReportAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"timesheets/getEntries?name={AuthorisedPersonNameForManager}&startDate={GetStartDate()}&endDate={GetEndDate()}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task GetTimesheetBookingsByCodeAndTaskAsNonManagerShouldReturnUnauthorised()
        {
            using (var client = GetClientAsDeveloper())
            {
                var response = await client.GetAsync($"timesheets/getByCodeTask?code={TimesheetCode}&startDate={GetStartDate()}&endDate={GetEndDate()}");
                Assert.That(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
            }
        }
    }
}
