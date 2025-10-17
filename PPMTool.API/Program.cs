using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PPMTool.API.Authentication;
using PPMTool.API.Endpoints;
using PPMTool.API.Filters;
using PPMTool.API.Services;
using PPMTool.Data.Context;
using PPMTool.API.Helpers;
using PPMTool.Services;
#if RELEASE
using Serilog;
#endif
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

// Add the custom appsettings file to the configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.api.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.api.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

// Add environment variables to the configuration
EnvironmentHelper.LoadEnvironmentVariables(builder);

#if RELEASE
// Get the log path from the configuration file
var logPath = configuration.GetValue<string>("LogPath");
if (string.IsNullOrEmpty(logPath))
{
    throw new Exception("LogPath configuration is missing or empty!");
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Logger(l =>
    {
        l.WriteTo.Console();
        l.WriteTo.File(
            path: logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: null,
            retainedFileTimeLimit: TimeSpan.FromDays(60));
    })
    .CreateLogger();
builder.Host.UseSerilog();
#endif

builder.Services.AddDbContext<PPMToolContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PPMToolContextConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
);
builder.Services.AddScoped<SkillTagService>();
builder.Services.AddSingleton<APIAuthService>();
builder.Services.AddTransient<ILogger>(s => s.GetRequiredService<ILogger<Program>>());

// Add services to the container
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    opt =>
    {
        opt.SwaggerDoc(
            name: "v1",
            info: new() { Title = "CapX API", Version = "v1" }
        );
        opt.UseInlineDefinitionsForEnums();

        // Operation filters for schema simplifications
        opt.OperationFilter<SkillTagShallowOperationFilter>();

        string? docFilePath = Directory.GetFiles(
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

        opt.AddSecurityDefinition("API Key", new OpenApiSecurityScheme
        {
            Description = "The API key to access the endpoints",
            Type = SecuritySchemeType.ApiKey,
            Name = "x-api-key",
            In = ParameterLocation.Header,
            Scheme = "ApiKeyScheme"
        });

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "API Key"
            },
            In = ParameterLocation.Header
        };

        var requirement = new OpenApiSecurityRequirement
        {
            { scheme, new string[] { } }
        };
        opt.AddSecurityRequirement(requirement);

#if !LOCAL
        // Add the custom DocumentFilter for production
        opt.DocumentFilter<BasePathDocumentFilter>("/api");
#endif
    }
);

var app = builder.Build();

// Check the environment variables are configured correctly
EnvironmentHelper.ValidateConfiguration(builder);

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// API key authentication middleware -- maybe replace with endpoint filter after .NET 8.0 upgrade?
// https://youtu.be/GrJJXixjR8M?feature=shared&t=775
app.UseMiddleware<APIKeyAuthMiddleware>();

// Map endpoints directly using top-level routing
app.MapGet($"/skills/getAll", Skills.GetAllSkillTagsAsync);
app.MapGet($"/skills/getAllForPerson/", Skills.GetAllSkillsTagsForPersonAsync);
app.MapGet($"/skills/getAllGrouped", Skills.GetAllPeopleWithSkillTagsAsync);
app.MapGet($"/timesheets/entries", Timesheets.GetTimesheetEntriesForPersonForDateRange);
app.MapGet($"/wlm/analysis", WorkloadModelAnalysis.GetWorkloadAnalysisData);
app.MapGet($"/leavebookings/getForSelfAndStaff", LeaveBookings.GetStaffBookingsForYearAsync);

// Fallback for unmatched routes
app.MapFallback(async context =>
{
    context.Response.StatusCode = 404;
    await context.Response.WriteAsync($"Endpoint {context.Request.Path} not found!");
});

app.Run();
