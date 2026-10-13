// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services.StatusEvaluators
{
    /// <summary>
    /// Evaluates the status of a Person entity and provides relevant status messages.
    /// </summary>
    public sealed class PersonStatusEvaluator : BaseStatusEvaluatorService<Person>
    {
        public PersonStatusEvaluator(FeatureService featureService) : base(featureService)
        {
        }

        protected override IReadOnlyList<StatusMessage> BuildCoreStatusMessages(Person person, int? messageViewerPersonId = null)
        {
            return new List<StatusMessage>
            {
                new StatusMessage("This person is currently absent.", StatusMessage.MessageType.Info, person.IsCurrentlyAbsent),
                new StatusMessage(
                    "Project Finance is enabled and this person has one or more workload model changes without a cost key. Missing cost keys fall back to zero cost.",
                    StatusMessage.MessageType.Warning,
                    () => person.HasWorkloadModelsWithoutCostKey(),
                    FeatureType.ProjectFinance)
            };
        }
    }
}
