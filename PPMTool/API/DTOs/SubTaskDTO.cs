namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a sub-task suitable for API data exports.
    /// </summary>
    /// <param name="SubTaskId">The unique sub-task ID.</param>
    /// <param name="OwningProjectProjectId">The ID of the project that owns the sub-task.</param>
    /// <param name="Name">The name of the sub-task.</param>
    /// <param name="StartDate">The start date of the sub-task.</param>
    /// <param name="EndDate">The end date of the sub-task.</param>
    /// <param name="TaskDuty">The task duty.</param>
    public sealed record SubTaskDTO(
        int SubTaskId,
        int OwningProjectProjectId,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        string TaskDuty
    );
}
