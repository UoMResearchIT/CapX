using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PPMTool.API.Filters
{
    /// <summary>
    /// Filter to adjust the "Try It Out" docs to add the required base path.
    /// </summary>
    public class BasePathDocumentFilter : IDocumentFilter
    {
        private readonly string basePath;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="basePath"></param>
        public BasePathDocumentFilter(string basePath) => this.basePath = basePath;

        /// <inheritdoc />
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = swaggerDoc.Paths.ToDictionary(
            path => $"{basePath}{path.Key}",
            path => path.Value
            );

            swaggerDoc.Paths.Clear();
            foreach (var path in paths)
            {
                swaggerDoc.Paths.Add(path.Key, path.Value);
            }
        }
    }

}
