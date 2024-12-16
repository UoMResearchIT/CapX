using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PPMTool.API.Authentication;
using PPMTool.API.Endpoints;
using PPMTool.Data.Context;
using PPMTool.Services;

var builder = WebApplication.CreateBuilder(args);

// Access the configuration to get the connection string
var configuration = builder.Configuration;

builder.Services.AddDbContext<PPMToolContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("PPMToolContextConnection") ?? throw new Exception("Invalid connection string!"))
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
            info: new() { Title = "PPMTool", Version = "v1" }
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
app.MapGet("/skills/getAll", Skills.GetAllSkillTagsAsync);
app.MapGet("/skills/getAllForPerson/{username}", Skills.GetAllSkillsTagsForPersonAsync);

app.Run();
