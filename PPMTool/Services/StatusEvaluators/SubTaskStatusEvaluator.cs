// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data;
using PPMTool.Data.Entities;

namespace PPMTool.Services.StatusEvaluators
{
    /// <summary>
    /// Evaluates the status of a SubTask entity and provides relevant status messages.
    /// </summary>
    public sealed class SubTaskStatusEvaluator : BaseStatusEvaluatorService<SubTask>
    {
        protected override IReadOnlyList<StatusMessage> BuildCoreStatusMessages(SubTask task)
        {
            return new List<StatusMessage>
            {
                // Info
                new StatusMessage("Task will start soon.", StatusMessage.MessageType.Info, task.WillStartWithinAMonth),
                new StatusMessage("Task has recently started.", StatusMessage.MessageType.Info, task.HasStartedInTheLastWeek),
                new StatusMessage("Task has absent resources and has started or will start soon!", StatusMessage.MessageType.Info, task.HasAbsentResourcesAndStartsWithinAWeek, Data.Enums.FeatureType.Absences),
                new StatusMessage("Task has resources with absence during or near the start of this task.", StatusMessage.MessageType.Info, task.IsAffectedByAbsence, Data.Enums.FeatureType.Absences),
                new StatusMessage("Task has zero demand.", StatusMessage.MessageType.Info, task.HasZeroDemandAndNoResources),

                // Warnings
                new StatusMessage("Task has provisional resources!", StatusMessage.MessageType.Warning, task.HasProvisionalResources),
                new StatusMessage("Task is under-resourced!", StatusMessage.MessageType.Warning, task.HasUnmetDemand),
                new StatusMessage("Task has resource(s) with zero FTE assignment!", StatusMessage.MessageType.Warning, task.HasResourceWithZeroFTE),

                // Errors
                new StatusMessage("Task has zero demand but assigned resources!", StatusMessage.MessageType.Error, task.HasZeroDemandButResourced),
                new StatusMessage("Resource on this task has no associated funding source and task is in progress or ran in the past!", StatusMessage.MessageType.Error, task.HasResourceWithNoFundingSourceAndRunning, Data.Enums.FeatureType.ProjectFinance)
            };
        }
    }
}
