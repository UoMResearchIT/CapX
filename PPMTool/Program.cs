using System.Linq.Dynamic.Core;
using System.Security.Claims;
using System.Web;
using Blazored.SessionStorage;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Services;
using Radzen;
#if RELEASE
using Serilog;
#endif

var builder = WebApplication.CreateBuilder(args);

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
    o.Debug = true;
});
#endif

// Add environment variables to the configuration
builder.Configuration.AddEnvironmentVariables();

// Get the API key from the environment
var apiKeySecret = Environment.GetEnvironmentVariable("API_KEY_SECRET");
if (!string.IsNullOrEmpty(apiKeySecret))
{
    // Add or override the Jwt:SecretKey in the configuration
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "Jwt:SecretKey", apiKeySecret }
        });
}

// Configure the services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    o.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});
builder.Services.AddDbContextFactory<PPMToolContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PPMToolContextConnection"))
);
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddRadzenComponents();
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
builder.Services.AddTransient<Microsoft.Extensions.Logging.ILogger>(s => s.GetRequiredService<ILogger<Program>>());
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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = new PathString("/Account/Login");
        options.LogoutPath = new PathString("/Account/Logout");
        options.Cookie.IsEssential = true;
        options.Cookie.Name = "CapXAuth";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;
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
builder.Services.AddAuthorization();

// Build the application from the configuration
var app = builder.Build();

// Set up middleware
var logger = app.Services.GetRequiredService<ILogger<Program>>();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    logger.LogInformation("DEVELOPMENT ENVIRONMENT");
    app.UseForwardedHeaders();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseForwardedHeaders();
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    logger.LogInformation("PRODUCTION ENVIRONMENT");
}

app.UseCookiePolicy();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();

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

        // Map UoM user name to claim
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
        identity.AddClaim(new Claim(ClaimTypes.Name, assertion.PrincipalName));

        // Lookup the username in the DB and add role claim
        // Has to be done manually since service provider not built yet?
        var dbContextFactory = context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<PPMToolContext>>();
        var dbContext = dbContextFactory.CreateDbContext();
        var user = dbContext.Users
            .Include(x => x.Person)
            .ToList()
            .FirstOrDefault(x => x.GetStandardisedUserName() == assertion.PrincipalName.Trim().ToLower());
        if (user != null)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, user.RoleType.ToString()));
        }

        await context.HttpContext.SignInAsync(context.Principal);

        // Update last logged in and log
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
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

        logger?.LogInformation($"{context.Principal.Identity.Name}: Logged In");
    }
}

/// <summary>
/// What to do when the user signs out from a CAS session
/// </summary>
Task OnCookieSigningOut(CookieSigningOutContext context, IConfiguration configuration)
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
    context.Options.Events.RedirectToLogout(logoutRedirectContext);
    return Task.CompletedTask;
}

/// <summary>
/// What to do when there is a failure during login
/// </summary>
Task OnRemoteFailure(RemoteFailureContext context)
{
    var failure = context.Failure;
    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
    if (!string.IsNullOrWhiteSpace(failure?.Message))
    {
        logger.LogError(failure, "{Exception}", failure.Message);
    }

    context.Response.Redirect($"/Account/ExternalLoginFailure?message={HttpUtility.UrlEncode(failure?.Message)}");
    context.HandleResponse();
    return Task.CompletedTask;
}