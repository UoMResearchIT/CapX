// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Tests.API
{
    /// <summary>
    /// Base for tests of the write endpoints, which need a Superuser key and the
    /// WriteApiEndpointsEnabled setting (switched on when the dummy data is seeded).
    /// The Superuser is created here, with a linked Person because projects/add makes the
    /// caller's Person the Request Owner, rather than changing the seeded one. Tests create
    /// their own records, named with <see cref="RunId"/>, and leave the seeded ones alone.
    /// </summary>
    public abstract class BaseWriteApiTest : BaseApiTest
    {
        protected const string SuperuserUsername = "write-api-test-superuser";
        protected const string SuperuserPersonName = "Write API Tester";

        protected static string? SuperuserApiKey { get; private set; }
        protected static int SuperuserPersonId { get; private set; }

        private static readonly DateTime runStarted = DateTime.UtcNow;

        /// <summary>
        /// Unique to this test run, so records one run creates can't clash with another's
        /// when the database is reused.
        /// </summary>
        protected static readonly string RunId = runStarted.ToString("MMddHHmmss");

        /// <summary>
        /// <see cref="RunId"/> as a number, for RTPs and dates.
        /// </summary>
        protected static readonly int RunNumber = int.Parse(runStarted.ToString("ddHHmmss"));

        /// <summary>
        /// Provides a configured HttpClient for making requests to the API with an API key for a superuser.
        /// </summary>
        /// <returns></returns>
        protected static HttpClient GetClientAsSuperuser()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri($"{Setup.BaseUrl}/api/");
            client.DefaultRequestHeaders.Add("x-api-key", SuperuserApiKey!);
            return client;
        }

        /// <summary>
        /// Creates an Active project in the first seeded school, with a timesheet code if given.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="rtp"></param>
        /// <param name="timesheetActivityCode"></param>
        /// <returns>The project's database id.</returns>
        protected static async Task<int> CreateProjectAsync(HttpClient client, int rtp, string? timesheetActivityCode = null)
        {
            var response = await client.PostAsJsonAsync("projects/add", new
            {
                Name = $"Write Test Project {rtp}",
                RTP = rtp,
                PI = "Test PI",
                SchoolId,
                Budget = 1000.0,
                CostModel = "TechOnly",
                DayRate = 0.0,
                ProjectStatus = "Active",
                Description = "<p>Created by the write API tests</p>",
                RequestDocLink = "Unknown",
                ManagementStartDate = new DateTime(2025, 1, 1),
                ManagementEndDate = new DateTime(2025, 12, 31),
                TimesheetActivityCode = timesheetActivityCode
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());
            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            return created.GetProperty("projectId").GetInt32();
        }

        [OneTimeSetUp]
        protected override void OneTimeSetup()
        {
            base.OneTimeSetup();
            SetupSuperuser();
        }

        /// <summary>
        /// Creates the test Superuser, its Person and an API key, once per test run.
        /// </summary>
        private static void SetupSuperuser()
        {
            if (SuperuserApiKey != null) return;

            using var context = CreateDbContext();
            var user = context.Users
                .Include(x => x.Person)
                .FirstOrDefault(x => x.CASUserName == SuperuserUsername);
            if (user == null)
            {
                user = new User
                {
                    CASUserName = SuperuserUsername,
                    EmailAddress = $"{SuperuserUsername}@example.invalid",
                    RoleType = RoleType.Superuser,
                    Person = new Person
                    {
                        Name = SuperuserPersonName,
                        ShortName = "WAT",
                        StartDate = new DateTime(2020, 1, 1),
                        FTE = 1.0
                    }
                };
                context.Users.Add(user);
            }

            var key = new ApiKey
            {
                Active = true,
                Description = "Write API tests",
                ExpiresAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1), DateTimeKind.Unspecified),
                Key = $"write-api-test-{Guid.NewGuid():N}",
                Owner = user
            };
            context.ApiKeys.Add(key);
            context.SaveChanges();

            SuperuserApiKey = key.Key;
            SuperuserPersonId = user.Person!.PersonId;
        }
    }
}
