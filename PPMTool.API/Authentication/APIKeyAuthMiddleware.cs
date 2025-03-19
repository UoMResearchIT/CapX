using Microsoft.EntityFrameworkCore;
using PPMTool.API.Services;
using PPMTool.Data.Context;

namespace PPMTool.API.Authentication
{
    /// <summary>
    /// Middleware to authenticate API key
    /// </summary>
    public class APIKeyAuthMiddleware
    {
        private readonly RequestDelegate next;
        private readonly PPMToolContext dbContext;
        private readonly APIAuthService authService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="contextFactory"></param>
        /// <param name="authService"></param>
        public APIKeyAuthMiddleware(
            RequestDelegate next,
            IDbContextFactory<PPMToolContext> contextFactory,
            APIAuthService authService)
        {
            this.next = next;
            dbContext = contextFactory.CreateDbContext();
            this.authService = authService;
        }

        /// <summary>
        /// Middleware implementation
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("x-api-key", out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is missing");
                return;
            }

            var matchingUser = authService.GetUserIfApiKeyActive(dbContext, extractedApiKey);

            if (matchingUser == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            await next(context);
        }
    }
}
