using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PPMTool.Areas.Identity.Data;
using PPMTool.Data;

[assembly: HostingStartup(typeof(PPMTool.Areas.Identity.IdentityHostingStartup))]
namespace PPMTool.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<PPMToolContext>(options =>
                    options.UseSqlite(
                        context.Configuration.GetConnectionString("PPMToolContextConnection")));

                services.AddDefaultIdentity<PPMToolUser>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<PPMToolContext>();
            });
        }
    }
}