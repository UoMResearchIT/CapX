using PPMTool.API.Services;
using PPMTool.Data.Context;

namespace PPMTool.API.Authentication
{
    /// <summary>
    /// Middleware to authenticate API key.
    /// </summary>
    public sealed class APIKeyAuthMiddleware
    {
        private readonly RequestDelegate next;
        private readonly APIAuthService authService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="authService"></param>
        public APIKeyAuthMiddleware(
            RequestDelegate next,
            APIAuthService authService)
        {
            this.next = next;
            this.authService = authService;
        }

        /// <summary>
        /// Middleware implementation
        /// </summary>
        /// <param name="context">HTTP Context</param>
        /// <param name="dbContext">DB Context</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context, PPMToolContext dbContext)
        {
            // Authentication everything else
            if (!context.Request.Headers.TryGetValue("x-api-key", out var extractedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key is missing");
                return;
            }

            var matchingUser = authService.GetUserIfApiKeyActive(dbContext, extractedApiKey);

            if (matchingUser == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            // Add the user as well
            context.Items.Add("User", matchingUser);

            await next(context);
        }
    }
}