using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                .ConfigureLogging((context, logging) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Logger(l =>
                        {
                            l.WriteTo.Console();
                            l.WriteTo.File(context.Configuration.GetValue<string>("Logging:LogPath"),
                                rollingInterval: RollingInterval.Day,
                                retainedFileCountLimit: null,
                                retainedFileTimeLimit: TimeSpan.FromDays(365));
                        })
                        .CreateLogger();
                    logging.AddSerilog();
                })
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