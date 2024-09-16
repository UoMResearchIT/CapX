using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Blazored.SessionStorage;
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
using PPMTool.Data.Context;
using PPMTool.Services;
using Radzen;

namespace PPMTool
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddDbContextFactory<PPMToolContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("PPMToolContextConnection"))
            );

            services.AddBlazoredSessionStorage();

            services.AddRadzenComponents();

            services.AddScoped<InnateCodeService>();
            services.AddScoped<RolesService>();
            services.AddScoped<PersonService>();
            services.AddScoped<ProjectService>();
            services.AddScoped<SubTaskService>();
            services.AddScoped<TagService>();
            services.AddScoped<EmailService>();
            services.AddScoped<NoteService>();
            services.AddScoped<FinancialReferenceService>();
            services.AddTransient<ILogger>(s => s.GetRequiredService<ILogger<Startup>>());

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 2;
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.Secure = CookieSecurePolicy.Always;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = new PathString("/Account/Login");
                    options.LogoutPath = new PathString("/Account/Logout");
                    options.Cookie.IsEssential = true;
                    options.Cookie.Name = "CapXAuth";
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnSigningOut = OnCookieSigningOut,
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
                        OnCreatingTicket = OnCreatingTicket,
                        OnRemoteFailure = OnRemoteFailure
                    };
                });

            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            RolesService roleService,
            SubTaskService taskService,
            ProjectService projectService,
            FinancialReferenceService financialReferenceService,
            ILogger<Startup> logger,
            IDbContextFactory<PPMToolContext> contextFactory
        )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                logger.LogInformation("DEVELOPMENT ENVIRONMENT");
                app.UseForwardedHeaders();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseForwardedHeaders();
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                logger.LogInformation("PRODUCTION ENVIRONMENT");
            }

            app.UseCookiePolicy();
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

            // TODO: REMOVE THIS AFTER v11 RELEASE
            var context = contextFactory.CreateDbContext();
            var tasks = taskService.GetAll(context);
            foreach (var task in tasks)
            {
                logger.LogInformation($"Updating planned and actual hours for sub-task {task.SubTaskId}");

                // Get total units
                var totalunits = task.AssignedResources.Sum(x => x.AssignmentFTE);

                // Assign proportion of planned work and actuals to resources
                foreach (var res in task.AssignedResources)
                {
                    res.PlannedWorkHours = task.PlannedWorkHours * (res.AssignmentFTE / totalunits);
                    res.ActualWorkHours = task.ActualWorkHours * (res.AssignmentFTE / totalunits);
                }

                taskService.Update(context, task);
            }
            var projects = projectService.GetAll(context);
            foreach (var project in projects)
            {
                logger.LogInformation($"Updating planned and actual costs for project {project.ProjectId}");

                project.UpdateProjectMetaData(true, financialReferenceService.GetAll(context));
                projectService.Update(context, project);
            }
            // TODO: REMOVE THE ABOVE AFTER v11 RELEASE
        }

        private async Task OnCreatingTicket(CasCreatingTicketContext context)
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
                var dbContextFactory = context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<PPMToolContext>>();
                var dbContext = dbContextFactory.CreateDbContext();
                var role = dbContext.Roles
                    .Include(x => x.Person)
                    .ToList()
                    .FirstOrDefault(x => x.GetStandardisedUserName() == assertion.PrincipalName.Trim().ToLower());
                if (role != null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role.RoleType.ToString()));
                }

                await context.HttpContext.SignInAsync(context.Principal);

                // Update last logged in and log
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
                var roleService = context.HttpContext.RequestServices.GetRequiredService<RolesService>();
                if (roleService != null)
                {
                    if (role != null)
                    {
                        roleService.UpdateLastLoggedIn(dbContext, role);
                    }
                }
                else
                {
                    logger?.LogError("RoleService not found! Cannot update last logged in!");
                }

                logger?.LogInformation($"{context.Principal.Identity.Name}: Logged In");
            }
        }

        private Task OnCookieSigningOut(CookieSigningOutContext context)
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
        }

        private Task OnRemoteFailure(RemoteFailureContext context)
        {
            var failure = context.Failure;
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
            if (!string.IsNullOrWhiteSpace(failure?.Message))
            {
                logger.LogError(failure, "{Exception}", failure.Message);
            }

            context.Response.Redirect($"/Account/ExternalLoginFailure?message={HttpUtility.UrlEncode(failure?.Message)}");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    }
}
