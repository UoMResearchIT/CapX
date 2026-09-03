// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Tests.API.Timesheets
{
    [TestFixture]
    public class EndpointOKTests : BaseApiTest
    {
        [Test]
        public async Task GetTimesheetEntriesForPersonForDateRangeShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync($"timesheets/getEntries?startDate={GetStartDate()}&endDate={GetEndDate()}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }

        [Test]
        public async Task GetTimesheetBookingsByCodeAndTaskShouldReturnOK()
        {
            using (var client = GetClientAsManager())
            {
                var response = await client.GetAsync($"timesheets/getByCodeTask?code={TimesheetCode}&startDate={GetStartDate()}&endDate={GetEndDate()}");
                Assert.That(response.IsSuccessStatusCode);
            }
        }
    }
}
