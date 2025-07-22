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
                    .ConfigureAppConfiguration((hostingContext, configBuilder) =>
                    {
                        var env = hostingContext.HostingEnvironment;
                        var overridingValues = new Dictionary<string, string>();

                        var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
                        if (!string.IsNullOrEmpty(sentryDsn))
                        {
                            overridingValues.Add("Sentry:Dsn", sentryDsn);
                        }
                        else if (env.IsProduction())
                        {
                            throw new InvalidOperationException("SENTRY_DSN environment variable is not set!");
                        }

                        configBuilder.AddInMemoryCollection(overridingValues);
                    })
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
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStaticWebAssets();
                        webBuilder.UseStartup<Startup>();
#if RELEASE

                        webBuilder.ConfigureAppConfiguration((context, configBuilder) =>
                        {
                            var config = context.Configuration;
                            webBuilder.UseSentry(o =>
                            {
                                o.Dsn = config["Sentry:Dsn"];
                                o.Release = config["VersionNumber"];
                            });
                        });
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