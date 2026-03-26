using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using PPMTool;
using PPMTool.API.Authentication;
using PPMTool.API.Endpoints;
using PPMTool.API.Filters;
using PPMTool.API.Services;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Helpers;
using PPMTool.Services;
using Radzen;
using EnvironmentHelper = PPMTool.Helpers.EnvironmentHelper;


#if RELEASE
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
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
var dbProvider = builder.Configuration.GetValue<string>("DbProvider");
builder.Services.AddDbContextFactory<PPMToolContext>(options => options.AddDbProvider(connectionString, builder.Configuration));
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
builder.Services.AddSingleton<FeatureService>();
builder.Services.AddScoped<FacultyService>();
builder.Services.AddScoped<SchoolService>();
builder.Services.AddSingleton<APIAuthService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// Choose the authentication type based on configuration
var authenticationType = builder.Configuration.GetValue("Authentication:Type", "CAS");

#if !RELEASE
// Override choice in local deployment mode
authenticationType = "CAS";
#endif

// This is always true in local mode
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

#if RELEASE
        // Set expiration to match CAS session timeout
        int time = builder.Configuration.GetValue("Authentication:CAS:CookieExpiryTimeInHours", 24);
        options.ExpireTimeSpan = TimeSpan.FromHours(time);
        options.SlidingExpiration = false;

        options.Events = new CookieAuthenticationEvents
        {
            OnSigningOut = args => AuthenticationCallbackHelper.OnCookieSigningOut(args, builder.Configuration),
        };
#endif
    })

#if RELEASE
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

        // Register callbacks
        options.Events = new CasEvents
        {
            OnCreatingTicket = AuthenticationCallbackHelper.OnCreatingTicket,
            OnRemoteFailure = AuthenticationCallbackHelper.OnRemoteFailure
        };
    })
#endif
    ;
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

        // Register callbacks
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = context =>
            {
                AuthenticationCallbackHelper.OnAzureAdTokenValidated(context);
                return Task.CompletedTask;
            },
            OnRemoteFailure = AuthenticationCallbackHelper.OnRemoteFailure
        };

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

builder.Services.AddAuthorization();

// Add API
const string ApiKeySchemeName = "API Key";
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    opt =>
    {
        opt.SwaggerDoc(
            name: "v1",
            info: new() { Title = "CapX API", Version = "v1" }
        );

        // Operation filters for schema simplifications
        opt.OperationFilter<SkillTagShallowOperationFilter>();

        // Include XMl comments for better documentation in Swagger UI
        string docFilePath = Directory.GetFiles(
            path: AppContext.BaseDirectory,
            searchPattern: $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
            searchOption: SearchOption.AllDirectories)
        .FirstOrDefault();

        if (docFilePath != null)
        {
            opt.IncludeXmlComments(docFilePath);
        }
        else
        {
            Debug.Assert(false, "XML documentation file not found");
        }

        opt.AddSecurityDefinition(ApiKeySchemeName, new OpenApiSecurityScheme
        {
            Description = "The API key to access the endpoints",
            Type = SecuritySchemeType.ApiKey,
            Name = "x-api-key",
            In = ParameterLocation.Header,
            Scheme = "ApiKeyScheme"
        });

        // Add a requirement using the new delegate overload and a scheme reference
        opt.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(ApiKeySchemeName, document)] = new List<string>()
        });
    }
);

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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CapX API v1");
    c.RoutePrefix = "swagger";
});

app.UseCookiePolicy();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();

// Map API endpoints
var api = app.MapGroup("/api");
api.MapGet($"/skills/getAll", Skills.GetAllSkillTagsAsync);
api.MapGet($"/skills/getAllForPerson", Skills.GetAllSkillsTagsForPersonAsync);
api.MapGet($"/skills/getAllGrouped", Skills.GetAllPeopleWithSkillTagsAsync);
api.MapGet($"/timesheets/getEntries", Timesheets.GetTimesheetEntriesForPersonForDateRange);
api.MapGet($"/timesheets/getByCodeTask", Timesheets.GetTimesheetBookingsByCodeAndTask);
api.MapGet($"/wlm/getAnalysis", WorkloadModelAnalysis.GetWorkloadAnalysisData);
api.MapGet($"/leavebookings/getForSelfAndStaff", LeaveBookings.GetStaffBookingsForYearAsync);

// API middleware -- conditional on /api routes only
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    apiApp =>
    {
        apiApp.UseMiddleware<APIKeyAuthMiddleware>();
    }
);

// This always goes last!!!
app.MapFallbackToPage("/_Host");

// Set the journal mode on the DB
if (dbProvider == "sqlite")
{
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
}

// Set dummy data seed flag
// This is intended for development and testing purposes only and should be used with caution as it will delete existing data.
var shouldSeed = builder.Configuration.GetValue<bool>("DeveloperSettings:SeedDummyData");

// Create a context to run migrations and seed data if required.
using var scope = app.Services.CreateScope();
var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PPMToolContext>>();
using (var context = dbContextFactory.CreateDbContext())
{
    // Delete existing DB if seeding to ensure a clean slate
    if (shouldSeed)
    {
        context.Database.EnsureDeleted();
    }

    // Run migrations
    context.Database.Migrate();
}

// Seed the default superuser from the settings if it doesn't already exist
SeedHelper.SeedSuperUserIfNotExist(scope.ServiceProvider);

// Seed features
SeedHelper.SeedFeatures(scope.ServiceProvider);

// If seeding run the dummy data seeding methods
if (shouldSeed)
{

    // Seed tables with suitable values -- Note that competencies are already seeded by migrations
    SeedHelper.SeedPeople(scope.ServiceProvider);
    SeedHelper.SeedAbsences(scope.ServiceProvider);
    SeedHelper.SeedUsers(scope.ServiceProvider);
    SeedHelper.SeedWorkloadModelChanges(scope.ServiceProvider);
    SeedHelper.SeedSkillTags(scope.ServiceProvider);
    SeedHelper.SeedOwnedSkillsForPeople(scope.ServiceProvider);
    SeedHelper.SeedCompetencyAssessments(scope.ServiceProvider);
    SeedHelper.SeedInnateCodesAndTasks(scope.ServiceProvider);
    SeedHelper.SeedFinancialReferences(scope.ServiceProvider);
    SeedHelper.SeedOrganisationalUnits(scope.ServiceProvider);
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

// Initialise feature service cache
using (var context = dbContextFactory.CreateDbContext())
{
    var featureService = app.Services.GetRequiredService<FeatureService>();
    _ = featureService.IntialiseServiceCacheAsync(context);
}

// Run the app
app.Run();