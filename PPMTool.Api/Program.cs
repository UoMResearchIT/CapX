using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Services;

var builder = WebApplication.CreateBuilder(args);

// Access the configuration to get the connection string
var configuration = builder.Configuration;

builder.Services.AddDbContext<PPMToolContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("PPMToolContextConnection"))
    );
builder.Services.AddScoped<TagService>();

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
    }
);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// Map endpoints to methods
app.MapGet("/skills/getAll", PPMTool.API.Endpoints.Skills.GetAll);


app.Run();
