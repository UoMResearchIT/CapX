using System;
using Microsoft.AspNetCore.Hosting;
#if RELEASE
using Microsoft.Extensions.Configuration;
#endif
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
#if !RELEASE
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
#endif
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
                                retainedFileTimeLimit: TimeSpan.FromDays(365));
                        })
                        .CreateLogger();
                    logging.AddSerilog();
                })
#endif
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
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