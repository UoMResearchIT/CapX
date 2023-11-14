using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GSS.Authentication.CAS.AspNetCore;
using GSS.Authentication.CAS.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Services;

namespace PPMTool
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private ILogger<Startup> Logger;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddDbContext<PPMToolContext>(options =>
            {
                var str = Configuration.GetConnectionString("PPMToolContextConnection");
                options.UseSqlite(str);
                options.EnableSensitiveDataLogging();
            });

            services.AddScoped<RolesService>();
            services.AddScoped<PersonService>();
            services.AddScoped<ProjectService>();
            services.AddScoped<SubTaskService>();
            services.AddScoped<TagService>();
            services.AddTransient<ILogger>(s => s.GetRequiredService<ILogger<Startup>>());

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.Secure = CookieSecurePolicy.Always;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = new PathString("/Account/Login");
                    options.LogoutPath = new PathString("/Account/Logout");
                    options.Cookie.IsEssential = true;
                    options.Cookie.Name = "CapXAuth";
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnSigningOut = context =>
                        {
                            // Single Sign-Out
                            var casUrl = new Uri(Configuration["Authentication:CAS:ServerUrlBase"]);
                            var redirectUri = UriHelper.BuildAbsolute(
                                casUrl.Scheme,
                                new HostString(casUrl.Host, casUrl.Port),
                                casUrl.LocalPath,
                                "/logout",
                                QueryString.Create("service", Configuration["HostUrl"])
                            );

                            var logoutRedirectContext = new RedirectContext<CookieAuthenticationOptions>(
                                context.HttpContext,
                                context.Scheme,
                                context.Options,
                                context.Properties,
                                redirectUri
                            );
                            context.Response.StatusCode = 204; // Prevent RedirectToReturnUrl
                            context.Options.Events.RedirectToLogout(logoutRedirectContext);
                            return Task.CompletedTask;
                        },
                    };
                })
                .AddCAS(options =>
                {
                    options.CasServerUrlBase = Configuration["Authentication:CAS:ServerUrlBase"];
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    var protocolVersion = Configuration.GetValue("Authentication:CAS:ProtocolVersion", 2);
                    if (protocolVersion != 3)
                    {
                        options.ServiceTicketValidator = protocolVersion switch
                        {
                            1 => new Cas10ServiceTicketValidator(options),
                            2 => new Cas20ServiceTicketValidator(options),
                            _ => null
                        };
                    }

                    options.Events = new CasEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            if (context.Identity == null)
                            {
                                return;
                            }

                            if (context.Principal?.Identity is ClaimsIdentity identity)
                            {
                                // Map claims from assertion and sign in
                                var assertion = context.Assertion;

                                // Map UoM user name to claim
                                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, assertion.PrincipalName));
                                identity.AddClaim(new Claim(ClaimTypes.Name, assertion.PrincipalName));

                                // Lookup the username in the DB and add role claim
                                // Has to be done manually since service provider not built yet?
                                var dbContext = new PPMToolContext();
                                var role = dbContext.Roles.FirstOrDefault(x => x.GetStandardisedUserName() == assertion.PrincipalName.Trim().ToLower());
                                if (role != null)
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, role.RoleType.ToString()));
                                }

                                await context.HttpContext.SignInAsync(context.Principal);
                                Logger?.LogInformation($"{context.Principal.Identity.Name} Logged In");
                            }
                        },
                        OnRemoteFailure = context =>
                        {
                            var failure = context.Failure;
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
                            if (!string.IsNullOrWhiteSpace(failure?.Message))
                            {
                                logger.LogError(failure, "{Exception}", failure.Message);
                            }

                            context.Response.Redirect("/Account/ExternalLoginFailure");
                            context.HandleResponse();
                            return Task.CompletedTask;
                        },
                    };
                });

            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            RolesService roleService,
            ILogger<Startup> logger)
        {
            // Assign the logger for use in the ConfigureServices callbacks
            Logger = logger;

            var forwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                ForwardLimit = 2
            };
            forwardedHeaderOptions.KnownNetworks.Clear();
            forwardedHeaderOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeaderOptions);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                logger.LogInformation("DEVELOPMENT ENVIRONMENT");
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                logger.LogInformation("PRODUCTION ENVIRONMENT");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            // Seed the superuser
            roleService.SeedSuperUser();

            // Initialise the Resource Helper
            ResourceHelper.Initialise();
        }
    }
}
