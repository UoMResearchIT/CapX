// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Helpers;
using PPMTool.Services;
using Radzen;

#if RELEASE
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using System.Web;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Identity.Web;
using Serilog;
#endif

// Add environment variables to the configuration
var builder = WebApplication.CreateBuilder(args);
EnvironmentHelper.LoadEnvironmentVariables(builder);

#if RELEASE
// Configure logging
builder.Logging.AddSerilog(new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: builder.Configuration.GetValue<string>("LogPath"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: null,
        retainedFileTimeLimit: TimeSpan.FromDays(30)
    )
.CreateLogger());

// Configure Sentry
SentrySdk.Init(o =>
{
    o.Dsn = builder.Configuration.GetValue<string>("Sentry:Dsn");
    o.Release = builder.Configuration.GetValue<string>("VersionNumber");
    o.Environment = builder.Environment.EnvironmentName;
    o.Debug = !builder.Environment.IsProduction();
});
#endif

// Configure the services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    o.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});

var connectionString = builder.Configuration.GetConnectionString("PPMToolContextConnection");
builder.Services.AddDbContextFactory<PPMToolContext>(options =>
    options.UseSqlite(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
);

builder.Services.AddBlazoredSessionStorage();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddRadzenComponents();
builder.Services.AddTransient<Microsoft.Extensions.Logging.ILogger>(s => s.GetRequiredService<ILogger<Program>>());
builder.Services.AddScoped<InnateCodeService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<SubTaskService>();
builder.Services.AddScoped<SkillTagService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<FinancialReferenceService>();
builder.Services.AddScoped<CompetencyService>();
builder.Services.AddScoped<TimesheetService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<FundingSourceService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 2;
});
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// Choose the authentication type based on configuration
var authenticationType = builder.Configuration.GetValue("Authentication:Type", "CAS");

#if RELEASE
if (authenticationType == "CAS")
{
    // Set up cookie details
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "CapXAuth";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.IsEssential = true;
        options.LoginPath = new PathString("/Account/Login");
        options.LogoutPath = new PathString("/Account/Logout");

        // Set expiration to match CAS session timeout
        int time = builder.Configuration.GetValue("Authentication:CAS:CookieExpiryTimeInHours", 24);
        options.ExpireTimeSpan = TimeSpan.FromHours(time);
        options.SlidingExpiration = false;

        options.Events = new CookieAuthenticationEvents
        {
            OnSigningOut = args => OnCookieSigningOut(args, builder.Configuration),
        };
    })
    .AddCAS(options =>
    {
        options.CasServerUrlBase = builder.Configuration["Authentication:CAS:ServerUrlBase"];
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        var protocolVersion = builder.Configuration.GetValue("Authentication:CAS:ProtocolVersion", 2);
        if (protocolVersion != 3)
        {
            options.ServiceTicketValidator = protocolVersion switch
            {
                1 => new Cas10ServiceTicketValidator(options),
                2 => new Cas20ServiceTicketValidator(options),
                _ => null
            };
        }
        options.Events = new CasEvents
        {
            OnCreatingTicket = OnCreatingTicket,
            OnRemoteFailure = OnRemoteFailure
        };
    });
}
else if (authenticationType == "AzureAd")
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

    })
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("Authentication:AzureAd", options);
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

    // Cookie settings
    builder.Services.PostConfigure<CookieAuthenticationOptions>(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.Cookie.Name = "CapXAuth";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.IsEssential = true;

        });
}
else
{
    throw new Exception($"Unsupported authentication type: {authenticationType}");
}
#endif

builder.Services.AddAuthorization();

// Build the application from the configuration
var app = builder.Build();

// Get logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Check configuration is correct
EnvironmentHelper.ValidateConfiguration(logger, builder, authenticationType);

// Set up middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseForwardedHeaders();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseForwardedHeaders();
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCookiePolicy();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Set the journal mode on the DB
using (var connection = new SqliteConnection(connectionString))
{
    // Setting this should persist across connections
    // https://learn.microsoft.com/en-gb/dotnet/standard/data/sqlite/compare#connection-strings
    connection.Open();
    using (var command = new SqliteCommand("PRAGMA journal_mode=WAL;", connection))
    {
        command.ExecuteNonQuery();
    }
    connection.Close();
}

// Seed the default superuser from the settings if it doesn't already exist
using var scope = app.Services.CreateScope();
SeedHelper.SeedSuperUserIfNotExist(scope.ServiceProvider);

// Seed dummy data if the flag is set to do so.
// This is intended for development and testing purposes only and should be used with caution as it will delete existing data.
var shouldSeed = builder.Configuration.GetValue<bool>("DeveloperSettings:SeedDummyData");
if (shouldSeed)
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();

    // Clear the existing DB and recreate a vanilla file
    using (var context = dbContextFactory.CreateDbContext())
    {
        context.Database.EnsureDeleted();
        context.Database.Migrate();
    }

    // Seed tables with suitable values -- Note that competencies are already seeded by migrations
    SeedHelper.SeedSuperUserIfNotExist(scope.ServiceProvider);
    SeedHelper.SeedPeople(scope.ServiceProvider);
    SeedHelper.SeedAbsences(scope.ServiceProvider);
    SeedHelper.SeedUsers(scope.ServiceProvider);
    SeedHelper.SeedWorkloadModelChanges(scope.ServiceProvider);
    SeedHelper.SeedSkillTags(scope.ServiceProvider);
    SeedHelper.SeedOwnedSkillsForPeople(scope.ServiceProvider);
    SeedHelper.SeedCompetencyAssessments(scope.ServiceProvider);
    SeedHelper.SeedInnateCodesAndTasks(scope.ServiceProvider);
    SeedHelper.SeedFinancialReferences(scope.ServiceProvider);
    SeedHelper.SeedProjects(scope.ServiceProvider);
    SeedHelper.SeedFundingSources(scope.ServiceProvider);
    SeedHelper.SeedSubTasks(scope.ServiceProvider);
    SeedHelper.SeedResources(scope.ServiceProvider);
    SeedHelper.SeedNotes(scope.ServiceProvider);
    SeedHelper.SeedInvoicesAndPayments(scope.ServiceProvider);
    SeedHelper.SeedTimesheets(scope.ServiceProvider);
}

// Set default culture
var cultureInfo = new CultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Clean local application file path
FileHelper.CleanLocalApplicationFilePath(logger);

// Run the app
app.Run();

#if RELEASE
/// <summary>
/// What to do when a ticket is to be created from a CAS callback
/// </summary>
async Task OnCreatingTicket(CasCreatingTicketContext context)
{
    if (context.Identity == null)
    {
        return;
    }

    if (context.Principal?.Identity is ClaimsIdentity identity)
    {
        // Map claims from assertion and sign in
        var assertion = context.Assertion;

        // Map UoM user name to claim (could also be email depending on what they signed in with)
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
        identity.AddClaim(new Claim(ClaimTypes.Name, assertion.PrincipalName));

        // Lookup the username / email in the DB and add role claim
        // Has to be done manually since service provider not built yet?
        var dbContextFactory = context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<PPMToolContext>>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
        using (var dbContext = dbContextFactory.CreateDbContext())
        {
            // Log the attempt
            var claimName = assertion.PrincipalName?.Trim().ToLower();
            logger?.LogInformation($"Signing in {claimName}");

            // Find the matching user in the DB
            var user = dbContext.Users
                .Include(x => x.Person)
                .ToList()
                .FirstOrDefault(x => x.MatchesClaim(claimName));

            // If found a user in the DB that matches, add their role claim
            if (user != null)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, user.RoleType.ToString()));
            }
            else
            {
                logger?.LogError($"Cannot find a user in the access DB with UID: {claimName}");
            }

            // Sign in the user
            await context.HttpContext.SignInAsync(context.Principal);

            // Update last logged in and log
            var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();
            if (userService != null)
            {
                if (user != null)
                {
                    userService.UpdateLastLoggedIn(dbContext, user);
                }
            }
            else
            {
                logger?.LogError("User Service not found! Cannot update last logged in!");
            }
        }

        logger?.LogInformation($"{context.Principal.Identity.Name}: Logged In");
    }
}

/// <summary>
/// What to do when the user signs out from a CAS session
/// </summary>
async Task OnCookieSigningOut(CookieSigningOutContext context, IConfiguration configuration)
{
    // Single Sign-Out
    var casUrl = new Uri(configuration["Authentication:CAS:ServerUrlBase"]);
    var redirectUri = UriHelper.BuildAbsolute(
        casUrl.Scheme,
        new HostString(casUrl.Host, casUrl.Port),
        casUrl.LocalPath,
        "/logout",
        QueryString.Create("service", configuration["HostUrl"])
    );

    var logoutRedirectContext = new RedirectContext<CookieAuthenticationOptions>(
        context.HttpContext,
        context.Scheme,
        context.Options,
        context.Properties,
        redirectUri
    );
    context.Response.StatusCode = 204; // Prevent RedirectToReturnUrl
    await context.Options.Events.RedirectToLogout(logoutRedirectContext);
}

/// <summary>
/// What to do when there is a failure during login
/// </summary>
async Task OnRemoteFailure(RemoteFailureContext context)
{
    var failure = context.Failure;
    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
    if (!string.IsNullOrWhiteSpace(failure?.Message))
    {
        logger.LogError(failure, "CAS authentication failed: {Exception}", failure?.Message);
    }

    // Clear local cookie
    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    context.Response.Redirect($"/Account/ExternalLoginFailure?message={HttpUtility.UrlEncode(failure?.Message)}");
    context.HandleResponse();
}
#endif