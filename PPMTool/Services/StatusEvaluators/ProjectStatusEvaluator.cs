// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services.StatusEvaluators
{
    public sealed class ProjectStatusEvaluator : BaseStatusEvaluatorService<Project>
    {
        private readonly SettingsService settingsService;
        private readonly IDbContextFactory<PPMToolContext> contextFactory;
        private readonly InvoiceService invoiceService;

        public ProjectStatusEvaluator(
            SettingsService settingsService,
            IDbContextFactory<PPMToolContext> contextFactory,
            InvoiceService invoiceService)
        {
            this.settingsService = settingsService;
            this.contextFactory = contextFactory;
            this.invoiceService = invoiceService;
        }

        protected override IReadOnlyList<StatusMessage> BuildCoreStatusMessages(Project project)
        {
            // Compute some pre-requisites for the status messages that need to be retrieved from other services
            var today = DateTime.Today;
            var currentFY = FinancialReference.GetFinancialYear(today);

            // Only run the other funding invoice checks if the project is not cancelled, has other funding sources, and it is between May and July
            var shouldRunInvoiceChecks =
                !project.ProjectStatus.IsCancelled()
                && (project.FundingSources?.Any(x => x.FundingSourceType == FundingSourceType.Other) ?? false)
                && today.Month is >= 5 and <= 7;

            // These are the bits of information we need for the invoice status message
            var hasCurrentFYInvoice = true;
            var requestedThisFY = 0d;

            // Use the invoice service to get the information we need if we should run the checks
            if (shouldRunInvoiceChecks)
            {
                using var context = contextFactory.CreateDbContext();
                hasCurrentFYInvoice = invoiceService.HasInvoiceInFinancialYear(context, project.ProjectId, currentFY);
                requestedThisFY = invoiceService.GetFundsRequestedForFinancialYear(context, project.ProjectId, currentFY);
            }

            return new List<StatusMessage>
            {
                // Info
                new StatusMessage("A task in this project will start soon.", StatusMessage.MessageType.Info, () => project.SubTasks?.Any(x => x.WillStartWithinAMonth()) ?? false),
                new StatusMessage("A task in this project has recently started.", StatusMessage.MessageType.Info, () => project.SubTasks?.Any(x => x.HasStartedInTheLastWeek()) ?? false),
                new StatusMessage("A task in this project has absent resources and has started or will start soon!", StatusMessage.MessageType.Info, () => project.SubTasks?.Any(x => x.HasAbsentResourcesAndStartsWithinAWeek()) ?? false, FeatureType.Absences),
                
                // Warnings
                new StatusMessage("A task in this project has provisional resources!", StatusMessage.MessageType.Warning, () => project.SubTasks?.Any(x => x.HasProvisionalResources()) ?? false),
                new StatusMessage("A current or future task in this project is under-resourced!", StatusMessage.MessageType.Warning, () => project.HasUnmetDemandInWindow()),
                new StatusMessage("This project has started but has no link to a project board!", StatusMessage.MessageType.Warning, project.HasStartedButHasNoScrumProjectLink),
                new StatusMessage("Task has resource(s) with zero FTE assignment!", StatusMessage.MessageType.Warning, project.HasResourceWithZeroFTE),
                new StatusMessage("This project is active and overbudget!", StatusMessage.MessageType.Warning, () => project.ProjectStatus.IsActive() && project.IsOverBudget() && !project.IsOverBudget(settingsService.GetSetting(SettingType.OverbudgetThreshold, 0d)), FeatureType.ProjectFinance),
                new StatusMessage("Funds requested this financial year are significantly below planned costs, has Other funding sources but no invoice in the current financial year. Raise one by June to request funds.", StatusMessage.MessageType.Warning, () => shouldRunInvoiceChecks ? !hasCurrentFYInvoice && project.FundsRequestedBelowPlannedCostForFY(requestedThisFY, settingsService.GetSetting(SettingType.UnderclaimedFundsThreshold, 10d), today) : false, FeatureType.ProjectFinance),

                // Errors
                new StatusMessage("This project is active and overbudget!", StatusMessage.MessageType.Error, () => project.ProjectStatus.IsActive() && project.IsOverBudget(settingsService.GetSetting(SettingType.OverbudgetThreshold, 0d)), FeatureType.ProjectFinance),
                new StatusMessage("This project has no agreed budget!", StatusMessage.MessageType.Error, project.HasNoBudget, FeatureType.ProjectFinance),
                new StatusMessage("A task in this project is running but the project is not active!", StatusMessage.MessageType.Error, project.RunningTaskButInactive),
                new StatusMessage("This project is active but has no currently running tasks!", StatusMessage.MessageType.Error, project.ActiveButNoRunningTask),
                new StatusMessage("This project has no project manager set!", StatusMessage.MessageType.Error, project.NotFinishedOrCancelledButNoPM),
                new StatusMessage("This project has no timesheet activity set and project has started or will start soon!", StatusMessage.MessageType.Error, () => project.NotFinishedOrCancelledButNoInnateCodeAndUpcoming(), FeatureType.Timesheets),
                new StatusMessage("This project has no project ID specified!", StatusMessage.MessageType.Error, () => project.RTP == 0),
                new StatusMessage("This project has no link to a request document!", StatusMessage.MessageType.Error, project.HasNoRequestDocLink),
                new StatusMessage("This project has no description!", StatusMessage.MessageType.Error, project.HasNoDescription),
                new StatusMessage("This project has no tasks!", StatusMessage.MessageType.Error, () => project.SubTasks == null || project.SubTasks.Count == 0),
                new StatusMessage("This project is active but hasn't had its actuals updated for more than a month!", StatusMessage.MessageType.Error, project.ActiveButNotHadActualsUpdatedForAMonth, FeatureType.Timesheets),
                new StatusMessage("This project has no funding sources but is either finished or is active!", StatusMessage.MessageType.Error, project.HasNoFundingSourcesButRan, FeatureType.ProjectFinance),
                new StatusMessage("This project has a task with a resource without a funding source and is currently running or has run in the past!", StatusMessage.MessageType.Error, project.HasResourcesWithNoFundingSourceOnRunningTask, FeatureType.ProjectFinance),
                new StatusMessage("This project uses the Day Rate model but has a DI funding source which is not allowed! DI funding sources must use salary costs for recharge.", StatusMessage.MessageType.Error, () => project.DayRateWithDIFunding(), FeatureType.ProjectFinance),
                new StatusMessage("This project does not have a project management task!", StatusMessage.MessageType.Error, () => !project.SubTasks?.Any(x => x.TaskDuty == Duty.ProjectAndServiceMgmt) ?? true),
                new StatusMessage("This project has funding sources contributing to the budget that are not associated with task resources!", StatusMessage.MessageType.Error, () => project.HasFundingSourcesNotLinkedToResources(), FeatureType.ProjectFinance)
            };
        }
    }
}
