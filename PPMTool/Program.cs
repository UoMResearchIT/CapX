using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace PPMTool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Host Created");
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            try
            {
                return Host.CreateDefaultBuilder(args)
#if RELEASE
                .ConfigureLogging((context, logging) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Logger(l =>
                        {
                            l.WriteTo.Console();
                            l.WriteTo.File(context.Configuration.GetValue<string>("LogPath"),
                                rollingInterval: RollingInterval.Day,
                                retainedFileCountLimit: null,
                                retainedFileTimeLimit: TimeSpan.FromDays(60));
                        })
                        .CreateLogger();
                    logging.AddSerilog();
                })
#endif
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables();

                    // Get the API key from the environment
                    var apiKeySecret = Environment.GetEnvironmentVariable("API_KEY_SECRET");
                    if (!string.IsNullOrEmpty(apiKeySecret))
                    {
                        // Add or override the Jwt:SecretKey in the configuration
                        config.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { "Jwt:SecretKey", apiKeySecret }
                        });
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
#if RELEASE
                    webBuilder.UseSentry();
#endif
                });

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host builder error");

                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}