using System.Reflection;
using Microsoft.OpenApi;
using PPMTool.API.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PPMTool.API.Filters
{
    /// <summary>
    /// Method to ensure that if SkillTag is featured in a return structure, the documentation only shows a shallow object.
    /// </summary>
    internal class SkillTagShallowOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attribute = context.MethodInfo.GetCustomAttribute<SkillTagShallowSchemaAttribute>();
            if (attribute != null)
            {
                var schema = context.SchemaRepository.Schemas["SkillTag"];

                // Remove the tasks needing this skill as this won't be returned by methods with this attribute
                schema?.Properties?.Remove("tasksNeedingThisSkill");
            }
        }
    }
}
