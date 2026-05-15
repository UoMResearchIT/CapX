CREATE TABLE IF NOT EXISTS "__EFMigrationsLock" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,
    "Timestamp" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "Settings" (
    "SettingId" INTEGER NOT NULL CONSTRAINT "PK_Settings" PRIMARY KEY AUTOINCREMENT,
    "SettingType" INTEGER NOT NULL,
    "SettingValue" TEXT NOT NULL,
    "Description" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "Competencies" (
    "CompetencyId" INTEGER NOT NULL CONSTRAINT "PK_Competencies" PRIMARY KEY AUTOINCREMENT,
    "Description" TEXT NOT NULL,
    "Objective" TEXT NOT NULL,
    "Grade" INTEGER NOT NULL,
    "Category" INTEGER NOT NULL,
    "Revision" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "RevisionDate" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "LegacyId" TEXT NULL,
    "Number" INTEGER NOT NULL
);
#CREATE TABLE sqlite_sequence(name,seq);
CREATE TABLE IF NOT EXISTS "Faculties" (
    "FacultyId" INTEGER NOT NULL CONSTRAINT "PK_Faculties" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Code" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS "Features" (
    "FeatureId" INTEGER NOT NULL CONSTRAINT "PK_Features" PRIMARY KEY AUTOINCREMENT,
    "FeatureType" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Enabled" INTEGER NOT NULL,
    "MustAlwaysBeEnabled" INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS "FinancialReferences" (
    "FinancialReferenceId" INTEGER NOT NULL CONSTRAINT "PK_FinancialReferences" PRIMARY KEY AUTOINCREMENT,
    "FinancialYear" INTEGER NOT NULL,
    "Grade41Costs" REAL NOT NULL,
    "Grade51Costs" REAL NOT NULL,
    "Grade55Costs" REAL NOT NULL,
    "Grade65Costs" REAL NOT NULL,
    "Grade71Costs" REAL NOT NULL,
    "Grade75Costs" REAL NOT NULL,
    "RecoveryTarget" REAL NOT NULL
);
CREATE TABLE IF NOT EXISTS "InnateCodes" (
    "InnateCodeId" INTEGER NOT NULL CONSTRAINT "PK_InnateCodes" PRIMARY KEY AUTOINCREMENT,
    "ActivityCode" TEXT NOT NULL,
    "ActivityName" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "IsSensitive" INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS "People" (
    "PersonId" INTEGER NOT NULL CONSTRAINT "PK_People" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "ShortName" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NULL,
    "FTE" REAL NOT NULL,
    "LineManagerPersonId" INTEGER NULL,
    "TimesheetTemplateData" TEXT NULL,
    CONSTRAINT "FK_People_People_LineManagerPersonId" FOREIGN KEY ("LineManagerPersonId") REFERENCES "People" ("PersonId")
);
CREATE TABLE IF NOT EXISTS "SkillTags" (
    "SkillTagId" INTEGER NOT NULL CONSTRAINT "PK_SkillTags" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "ControlledName" TEXT NOT NULL,
    "HasValidWikiLink" INTEGER NOT NULL,
    "Rareness" INTEGER NOT NULL,
    "RarenessCount" INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS "Schools" (
    "SchoolId" INTEGER NOT NULL CONSTRAINT "PK_Schools" PRIMARY KEY AUTOINCREMENT,
    "FacultyId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Code" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_Schools_Faculties_FacultyId" FOREIGN KEY ("FacultyId") REFERENCES "Faculties" ("FacultyId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "InnateCodeTasks" (
    "InnateCodeTaskId" INTEGER NOT NULL CONSTRAINT "PK_InnateCodeTasks" PRIMARY KEY AUTOINCREMENT,
    "TaskName" TEXT NOT NULL,
    "Duty" INTEGER NOT NULL,
    "InnateCodeId" INTEGER NOT NULL,
    CONSTRAINT "FK_InnateCodeTasks_InnateCodes_InnateCodeId" FOREIGN KEY ("InnateCodeId") REFERENCES "InnateCodes" ("InnateCodeId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Absence" (
    "AbsenceId" INTEGER NOT NULL CONSTRAINT "PK_Absence" PRIMARY KEY AUTOINCREMENT,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NULL,
    "PersonId" INTEGER NOT NULL,
    CONSTRAINT "FK_Absence_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("PersonId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "CompetencyAssessments" (
    "CompetencyAssessmentId" INTEGER NOT NULL CONSTRAINT "PK_CompetencyAssessments" PRIMARY KEY AUTOINCREMENT,
    "Evidence" TEXT NULL,
    "DateCreated" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "CompetencyRevision" INTEGER NOT NULL,
    "CompetencyDescription" TEXT NOT NULL,
    "CompetencyObjective" TEXT NOT NULL,
    "CompetencyId" INTEGER NOT NULL,
    "PersonId" INTEGER NOT NULL,
    CONSTRAINT "FK_CompetencyAssessments_Competencies_CompetencyId" FOREIGN KEY ("CompetencyId") REFERENCES "Competencies" ("CompetencyId") ON DELETE CASCADE,
    CONSTRAINT "FK_CompetencyAssessments_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("PersonId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Timesheets" (
    "TimesheetId" INTEGER NOT NULL CONSTRAINT "PK_Timesheets" PRIMARY KEY AUTOINCREMENT,
    "OwnerId" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "Info" TEXT NULL,
    "Status" INTEGER NOT NULL,
    "DateStatusChanged" TEXT NOT NULL,
    "StatusChangedByPersonId" INTEGER NULL,
    CONSTRAINT "FK_Timesheets_People_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "People" ("PersonId") ON DELETE CASCADE,
    CONSTRAINT "FK_Timesheets_People_StatusChangedByPersonId" FOREIGN KEY ("StatusChangedByPersonId") REFERENCES "People" ("PersonId")
);
CREATE TABLE IF NOT EXISTS "Users" (
    "UserId" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "RoleType" INTEGER NOT NULL,
    "CASUserName" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "PersonId" INTEGER NULL,
    "LastLoggedIn" TEXT NULL,
    "EmailAddress" TEXT NOT NULL,
    CONSTRAINT "FK_Users_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("PersonId")
);
CREATE TABLE IF NOT EXISTS "WorkloadModelChanges" (
    "WorkloadModelChangeId" INTEGER NOT NULL CONSTRAINT "PK_WorkloadModelChanges" PRIMARY KEY AUTOINCREMENT,
    "Grade" INTEGER NOT NULL,
    "ChangeDate" TEXT NOT NULL,
    "ProjectWorkFTE" REAL NOT NULL,
    "BusinessAsUsualFTE" REAL NOT NULL,
    "PersonalDevelopmentFTE" REAL NOT NULL,
    "StaffManagementFTE" REAL NOT NULL,
    "ProjectAndServiceManagementFTE" REAL NOT NULL,
    "ArchitectureFTE" REAL NOT NULL,
    "ServiceManagementFTE" REAL NOT NULL,
    "ProjectManagementFTE" REAL NOT NULL,
    "Notes" TEXT NULL,
    "PersonId" INTEGER NOT NULL,
    CONSTRAINT "FK_WorkloadModelChanges_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("PersonId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "OwnedSkills" (
    "OwnedSkillId" INTEGER NOT NULL CONSTRAINT "PK_OwnedSkills" PRIMARY KEY AUTOINCREMENT,
    "OwnerPersonId" INTEGER NOT NULL,
    "SkillTagId" INTEGER NOT NULL,
    "LastUsed" TEXT NOT NULL,
    "Proficiency" INTEGER NOT NULL,
    "FavouriteSkill" INTEGER NOT NULL,
    CONSTRAINT "FK_OwnedSkills_People_OwnerPersonId" FOREIGN KEY ("OwnerPersonId") REFERENCES "People" ("PersonId") ON DELETE CASCADE,
    CONSTRAINT "FK_OwnedSkills_SkillTags_SkillTagId" FOREIGN KEY ("SkillTagId") REFERENCES "SkillTags" ("SkillTagId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Projects" (
    "ProjectId" INTEGER NOT NULL CONSTRAINT "PK_Projects" PRIMARY KEY AUTOINCREMENT,
    "RTP" INTEGER NOT NULL,
    "PI" TEXT NOT NULL,
    "SchoolId" INTEGER NOT NULL,
    "ProjectManagerPersonId" INTEGER NULL,
    "Budget" REAL NOT NULL,
    "DayRate" REAL NOT NULL,
    "CostModel" INTEGER NOT NULL,
    "ProjectStatus" INTEGER NOT NULL,
    "InnateActivityInnateCodeId" INTEGER NULL,
    "Description" TEXT NOT NULL,
    "ScrumProjectLink" TEXT NULL,
    "RequestDocLink" TEXT NOT NULL,
    "PlannedLeadershipCosts" REAL NOT NULL,
    "ActualLeadershipCosts" REAL NOT NULL,
    "BudgetedIndirects" REAL NOT NULL,
    "ActualsLastUpdated" TEXT NULL,
    "PlannedWorkHours" REAL NOT NULL,
    "ActualWorkHours" REAL NOT NULL,
    "PlannedCost" REAL NOT NULL,
    "ActualCost" REAL NOT NULL,
    "PlannedIndirectCost" REAL NOT NULL,
    "ActualIndirectCost" REAL NOT NULL,
    "Name" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NOT NULL,
    CONSTRAINT "FK_Projects_InnateCodes_InnateActivityInnateCodeId" FOREIGN KEY ("InnateActivityInnateCodeId") REFERENCES "InnateCodes" ("InnateCodeId"),
    CONSTRAINT "FK_Projects_People_ProjectManagerPersonId" FOREIGN KEY ("ProjectManagerPersonId") REFERENCES "People" ("PersonId"),
    CONSTRAINT "FK_Projects_Schools_SchoolId" FOREIGN KEY ("SchoolId") REFERENCES "Schools" ("SchoolId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "TimesheetEntries" (
    "TimesheetEntryId" INTEGER NOT NULL CONSTRAINT "PK_TimesheetEntries" PRIMARY KEY AUTOINCREMENT,
    "TimesheetId" INTEGER NOT NULL,
    "InnateCodeTaskId" INTEGER NOT NULL,
    "MondayHours" REAL NOT NULL,
    "TuesdayHours" REAL NOT NULL,
    "WednesdayHours" REAL NOT NULL,
    "ThursdayHours" REAL NOT NULL,
    "FridayHours" REAL NOT NULL,
    "SaturdayHours" REAL NOT NULL,
    "SundayHours" REAL NOT NULL,
    CONSTRAINT "FK_TimesheetEntries_InnateCodeTasks_InnateCodeTaskId" FOREIGN KEY ("InnateCodeTaskId") REFERENCES "InnateCodeTasks" ("InnateCodeTaskId") ON DELETE CASCADE,
    CONSTRAINT "FK_TimesheetEntries_Timesheets_TimesheetId" FOREIGN KEY ("TimesheetId") REFERENCES "Timesheets" ("TimesheetId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "ApiKeys" (
    "ApiKeyId" INTEGER NOT NULL CONSTRAINT "PK_ApiKeys" PRIMARY KEY AUTOINCREMENT,
    "OwnerUserId" INTEGER NOT NULL,
    "Key" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Active" INTEGER NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    CONSTRAINT "FK_ApiKeys_Users_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "FundingSources" (
    "FundingSourceId" INTEGER NOT NULL CONSTRAINT "PK_FundingSources" PRIMARY KEY AUTOINCREMENT,
    "FundingSourceType" INTEGER NOT NULL,
    "HasAccountCode" INTEGER NOT NULL,
    "AccountCode" TEXT NULL,
    "Description" TEXT NULL,
    "AmountAvailable" REAL NOT NULL,
    "ProjectId" INTEGER NOT NULL,
    CONSTRAINT "FK_FundingSources_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("ProjectId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Invoices" (
    "InvoiceId" INTEGER NOT NULL CONSTRAINT "PK_Invoices" PRIMARY KEY AUTOINCREMENT,
    "InvoiceReference" TEXT NULL,
    "Status" INTEGER NOT NULL,
    "InvoiceUrl" TEXT NOT NULL,
    "ProjectId" INTEGER NOT NULL,
    "KeyDate" TEXT NOT NULL,
    "Value" REAL NOT NULL,
    "Description" TEXT NOT NULL,
    CONSTRAINT "FK_Invoices_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("ProjectId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Notes" (
    "NoteId" INTEGER NOT NULL CONSTRAINT "PK_Notes" PRIMARY KEY AUTOINCREMENT,
    "HtmlContent" TEXT NOT NULL,
    "AuthorUserId" INTEGER NOT NULL,
    "ProjectId" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "EditedDate" TEXT NOT NULL,
    "EditorUserId" INTEGER NULL,
    "IsFinanceInfo" INTEGER NOT NULL,
    "DueDate" TEXT NULL,
    "CompletedDate" TEXT NULL,
    CONSTRAINT "FK_Notes_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("ProjectId") ON DELETE CASCADE,
    CONSTRAINT "FK_Notes_Users_AuthorUserId" FOREIGN KEY ("AuthorUserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE,
    CONSTRAINT "FK_Notes_Users_EditorUserId" FOREIGN KEY ("EditorUserId") REFERENCES "Users" ("UserId")
);
CREATE TABLE IF NOT EXISTS "PersonProject" (
    "FollowedProjectsProjectId" INTEGER NOT NULL,
    "FollowersPersonId" INTEGER NOT NULL,
    CONSTRAINT "PK_PersonProject" PRIMARY KEY ("FollowedProjectsProjectId", "FollowersPersonId"),
    CONSTRAINT "FK_PersonProject_People_FollowersPersonId" FOREIGN KEY ("FollowersPersonId") REFERENCES "People" ("PersonId") ON DELETE CASCADE,
    CONSTRAINT "FK_PersonProject_Projects_FollowedProjectsProjectId" FOREIGN KEY ("FollowedProjectsProjectId") REFERENCES "Projects" ("ProjectId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "SubTasks" (
    "SubTaskId" INTEGER NOT NULL CONSTRAINT "PK_SubTasks" PRIMARY KEY AUTOINCREMENT,
    "TaskType" INTEGER NOT NULL,
    "PredecessorSubTaskId" INTEGER NULL,
    "HasFixedStart" INTEGER NOT NULL,
    "HasFixedEndDate" INTEGER NOT NULL,
    "Demand" REAL NOT NULL,
    "OriginalDemand" REAL NOT NULL,
    "UnmetDemand" REAL NOT NULL,
    "Lag" INTEGER NOT NULL,
    "OwningProjectProjectId" INTEGER NOT NULL,
    "IsLeadershipTask" INTEGER NOT NULL,
    "PlannedWorkHours" REAL NOT NULL,
    "ActualWorkHours" REAL NOT NULL,
    "PlannedCost" REAL NOT NULL,
    "ActualCost" REAL NOT NULL,
    "PlannedIndirectCost" REAL NOT NULL,
    "ActualIndirectCost" REAL NOT NULL,
    "Name" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NOT NULL,
    "DurationDays" INTEGER NOT NULL,
    "DurationBillableDays" INTEGER NOT NULL,
    CONSTRAINT "FK_SubTasks_Projects_OwningProjectProjectId" FOREIGN KEY ("OwningProjectProjectId") REFERENCES "Projects" ("ProjectId") ON DELETE CASCADE,
    CONSTRAINT "FK_SubTasks_SubTasks_PredecessorSubTaskId" FOREIGN KEY ("PredecessorSubTaskId") REFERENCES "SubTasks" ("SubTaskId")
);
CREATE TABLE IF NOT EXISTS "Payments" (
    "PaymentId" INTEGER NOT NULL CONSTRAINT "PK_Payments" PRIMARY KEY AUTOINCREMENT,
    "InvoiceId" INTEGER NULL,
    "SourceFundingSourceId" INTEGER NOT NULL,
    "ProjectId" INTEGER NOT NULL,
    "KeyDate" TEXT NOT NULL,
    "Value" REAL NOT NULL,
    "Description" TEXT NOT NULL,
    CONSTRAINT "FK_Payments_FundingSources_SourceFundingSourceId" FOREIGN KEY ("SourceFundingSourceId") REFERENCES "FundingSources" ("FundingSourceId") ON DELETE CASCADE,
    CONSTRAINT "FK_Payments_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("InvoiceId"),
    CONSTRAINT "FK_Payments_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("ProjectId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Resources" (
    "ResourceId" INTEGER NOT NULL CONSTRAINT "PK_Resources" PRIMARY KEY AUTOINCREMENT,
    "PersonId" INTEGER NOT NULL,
    "DayRate" REAL NULL,
    "AssignmentFTE" REAL NOT NULL,
    "BilledFTE" REAL NOT NULL,
    "IsProvisional" INTEGER NOT NULL,
    "UseProjectDayRate" INTEGER NOT NULL,
    "FundedFromFundingSourceId" INTEGER NULL,
    "SubTaskId" INTEGER NOT NULL,
    "PlannedWorkHours" REAL NOT NULL,
    "ActualWorkHours" REAL NOT NULL,
    "PlannedCost" REAL NOT NULL,
    "ActualCost" REAL NOT NULL,
    "PlannedIndirectCost" REAL NOT NULL,
    "ActualIndirectCost" REAL NOT NULL,
    CONSTRAINT "FK_Resources_FundingSources_FundedFromFundingSourceId" FOREIGN KEY ("FundedFromFundingSourceId") REFERENCES "FundingSources" ("FundingSourceId"),
    CONSTRAINT "FK_Resources_People_PersonId" FOREIGN KEY ("PersonId") REFERENCES "People" ("PersonId") ON DELETE CASCADE,
    CONSTRAINT "FK_Resources_SubTasks_SubTaskId" FOREIGN KEY ("SubTaskId") REFERENCES "SubTasks" ("SubTaskId") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "SkillTagSubTask" (
    "SkillsRequiredSkillTagId" INTEGER NOT NULL,
    "TasksNeedingThisSkillSubTaskId" INTEGER NOT NULL,
    CONSTRAINT "PK_SkillTagSubTask" PRIMARY KEY ("SkillsRequiredSkillTagId", "TasksNeedingThisSkillSubTaskId"),
    CONSTRAINT "FK_SkillTagSubTask_SkillTags_SkillsRequiredSkillTagId" FOREIGN KEY ("SkillsRequiredSkillTagId") REFERENCES "SkillTags" ("SkillTagId") ON DELETE CASCADE,
    CONSTRAINT "FK_SkillTagSubTask_SubTasks_TasksNeedingThisSkillSubTaskId" FOREIGN KEY ("TasksNeedingThisSkillSubTaskId") REFERENCES "SubTasks" ("SubTaskId") ON DELETE CASCADE
);
CREATE INDEX "IX_Absence_PersonId" ON "Absence" ("PersonId");
CREATE INDEX "IX_ApiKeys_OwnerUserId" ON "ApiKeys" ("OwnerUserId");
CREATE INDEX "IX_CompetencyAssessments_CompetencyId" ON "CompetencyAssessments" ("CompetencyId");
CREATE INDEX "IX_CompetencyAssessments_PersonId" ON "CompetencyAssessments" ("PersonId");
CREATE INDEX "IX_FundingSources_ProjectId" ON "FundingSources" ("ProjectId");
CREATE INDEX "IX_InnateCodeTasks_InnateCodeId" ON "InnateCodeTasks" ("InnateCodeId");
CREATE INDEX "IX_Invoices_ProjectId" ON "Invoices" ("ProjectId");
CREATE INDEX "IX_Notes_AuthorUserId" ON "Notes" ("AuthorUserId");
CREATE INDEX "IX_Notes_EditorUserId" ON "Notes" ("EditorUserId");
CREATE INDEX "IX_Notes_ProjectId" ON "Notes" ("ProjectId");
CREATE INDEX "IX_OwnedSkills_OwnerPersonId" ON "OwnedSkills" ("OwnerPersonId");
CREATE INDEX "IX_OwnedSkills_SkillTagId" ON "OwnedSkills" ("SkillTagId");
CREATE INDEX "IX_Payments_InvoiceId" ON "Payments" ("InvoiceId");
CREATE INDEX "IX_Payments_ProjectId" ON "Payments" ("ProjectId");
CREATE INDEX "IX_Payments_SourceFundingSourceId" ON "Payments" ("SourceFundingSourceId");
CREATE INDEX "IX_People_LineManagerPersonId" ON "People" ("LineManagerPersonId");
CREATE INDEX "IX_PersonProject_FollowersPersonId" ON "PersonProject" ("FollowersPersonId");
CREATE INDEX "IX_Projects_InnateActivityInnateCodeId" ON "Projects" ("InnateActivityInnateCodeId");
CREATE INDEX "IX_Projects_ProjectManagerPersonId" ON "Projects" ("ProjectManagerPersonId");
CREATE INDEX "IX_Projects_SchoolId" ON "Projects" ("SchoolId");
CREATE INDEX "IX_Resources_FundedFromFundingSourceId" ON "Resources" ("FundedFromFundingSourceId");
CREATE INDEX "IX_Resources_PersonId" ON "Resources" ("PersonId");
CREATE INDEX "IX_Resources_SubTaskId" ON "Resources" ("SubTaskId");
CREATE INDEX "IX_Schools_FacultyId" ON "Schools" ("FacultyId");
CREATE INDEX "IX_SkillTagSubTask_TasksNeedingThisSkillSubTaskId" ON "SkillTagSubTask" ("TasksNeedingThisSkillSubTaskId");
CREATE INDEX "IX_SubTasks_OwningProjectProjectId" ON "SubTasks" ("OwningProjectProjectId");
CREATE INDEX "IX_SubTasks_PredecessorSubTaskId" ON "SubTasks" ("PredecessorSubTaskId");
CREATE INDEX "IX_TimesheetEntries_InnateCodeTaskId" ON "TimesheetEntries" ("InnateCodeTaskId");
CREATE INDEX "IX_TimesheetEntries_TimesheetId" ON "TimesheetEntries" ("TimesheetId");
CREATE INDEX "IX_Timesheets_OwnerId" ON "Timesheets" ("OwnerId");
CREATE INDEX "IX_Timesheets_StatusChangedByPersonId" ON "Timesheets" ("StatusChangedByPersonId");
CREATE INDEX "IX_Users_PersonId" ON "Users" ("PersonId");
CREATE INDEX "IX_WorkloadModelChanges_PersonId" ON "WorkloadModelChanges" ("PersonId");
