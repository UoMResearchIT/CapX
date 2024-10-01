using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Services;
namespace PPMTool.Api;

class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        


        //builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        builder.Services.AddDbContextFactory<PPMToolContext>(opt =>
        {
            opt.UseInMemoryDatabase("PPMTool"); // just for testing
        });

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
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
        
        // map endpoints to methods
        app.MapGet("/skill/getAll", Endpoints.Skill.GetAll);
        
        
        app.Run();
    }
}