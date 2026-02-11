// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PPMTool.API.Filters
{
    /// <summary>
    /// Filter to adjust the "Try It Out" docs to add the required base path.
    /// </summary>
    public class BasePathDocumentFilter : IDocumentFilter
    {
        private readonly string _basePath;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="basePath"></param>
        public BasePathDocumentFilter(string basePath) => _basePath = basePath;

        /// <inheritdoc />
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = swaggerDoc.Paths.ToDictionary(
            path => $"{_basePath}{path.Key}",
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
