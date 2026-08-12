// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data;
using PPMTool.Data.Entities;

namespace PPMTool.Services.StatusEvaluators
{
    /// <summary>
    /// Evaluates the status of a Person entity and provides relevant status messages.
    /// </summary>
    public sealed class PersonStatusEvaluator : BaseStatusEvaluatorService<Person>
    {
        protected override IReadOnlyList<StatusMessage> BuildCoreStatusMessages(Person person)
        {
            return new List<StatusMessage>
            {
                new StatusMessage("This person is currently absent.", StatusMessage.MessageType.Info, person.IsCurrentlyAbsent)
            };
        }
    }
}
