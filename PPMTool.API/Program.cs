using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PPMTool.API.Authentication;
using PPMTool.API.Endpoints;
using PPMTool.Data.Context;
using PPMTool.Services;
#if RELEASE
using Serilog;
#endif
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

// Access the configuration to get the connection string
var configuration = builder.Configuration;

#if RELEASE
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Logger(l =>
    {
        l.WriteTo.Console();
        l.WriteTo.File(
            path: configuration.GetValue<string>("LogPath"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: null,
            retainedFileTimeLimit: TimeSpan.FromDays(60));
    })
    .CreateLogger();
builder.Host.UseSerilog();
#endif

// Add the custom appsettings file to the configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.api.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.api.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Use a different connection string in production
builder.Services.AddDbContext<PPMToolContext>(options =>
        options.UseSqlite(
            configuration.GetConnectionString(
#if RELEASE
                "PPMToolContextConnectionProduction"
#else
                "PPMToolContextConnection"
#endif
                ) ?? throw new Exception("Invalid connection string!")
        )
    );
builder.Services.AddScoped<TagService>();
builder.Services.AddTransient<ILogger>(s => s.GetRequiredService<ILogger<Program>>());

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    opt =>
    {
        opt.SwaggerDoc(
            name: "v1",
            info: new() { Title = "CapX API", Version = "v1" }
        );

        string? docFilePath = Directory.GetFiles(
            path: Directory.GetCurrentDirectory(),
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
    }
);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// API key authentication middleware -- maybe replace with endpoint filter after .NET 8.0 upgrade?
// https://youtu.be/GrJJXixjR8M?feature=shared&t=775
app.UseMiddleware<APIKeyAuthMiddleware>();

// Map endpoints to methods
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/skills/getAll", Skills.GetAllSkillTagsAsync);
    endpoints.MapGet("/skills/getAllForPerson/{name}", Skills.GetAllSkillsTagsForPersonAsync);
    endpoints.MapGet("/skills/getAllGrouped", Skills.GetAllPeopleWithSkillTagsAsync);
    endpoints.MapFallback(context =>
    {
        context.Response.StatusCode = 404;
        return context.Response.WriteAsync($"Endpoint {context.Request.Path} not found!");
    });
});

app.Run();
