using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Competencies",
                columns: table => new
                {
                    CompetencyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RevisionDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LegacyId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competencies", x => x.CompetencyId);
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    FacultyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.FacultyId);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    FeatureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    MustAlwaysBeEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.FeatureId);
                });

            migrationBuilder.CreateTable(
                name: "FinancialReferences",
                columns: table => new
                {
                    FinancialReferenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialYear = table.Column<int>(type: "int", nullable: false),
                    Grade41Costs = table.Column<float>(type: "real", nullable: false),
                    Grade51Costs = table.Column<float>(type: "real", nullable: false),
                    Grade55Costs = table.Column<float>(type: "real", nullable: false),
                    Grade65Costs = table.Column<float>(type: "real", nullable: false),
                    Grade71Costs = table.Column<float>(type: "real", nullable: false),
                    Grade75Costs = table.Column<float>(type: "real", nullable: false),
                    RecoveryTarget = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialReferences", x => x.FinancialReferenceId);
                });

            migrationBuilder.CreateTable(
                name: "InnateCodes",
                columns: table => new
                {
                    InnateCodeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnateCodes", x => x.InnateCodeId);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FTE = table.Column<double>(type: "float", nullable: false),
                    LineManagerPersonId = table.Column<int>(type: "int", nullable: true),
                    TimesheetTemplateData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.PersonId);
                    table.ForeignKey(
                        name: "FK_People_People_LineManagerPersonId",
                        column: x => x.LineManagerPersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                });

            migrationBuilder.CreateTable(
                name: "SkillTags",
                columns: table => new
                {
                    SkillTagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ControlledName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasValidWikiLink = table.Column<int>(type: "int", nullable: false),
                    Rareness = table.Column<int>(type: "int", nullable: false),
                    RarenessCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTags", x => x.SkillTagId);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    SchoolId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.SchoolId);
                    table.ForeignKey(
                        name: "FK_Schools_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "FacultyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InnateCodeTasks",
                columns: table => new
                {
                    InnateCodeTaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duty = table.Column<int>(type: "int", nullable: false),
                    InnateCodeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnateCodeTasks", x => x.InnateCodeTaskId);
                    table.ForeignKey(
                        name: "FK_InnateCodeTasks_InnateCodes_InnateCodeId",
                        column: x => x.InnateCodeId,
                        principalTable: "InnateCodes",
                        principalColumn: "InnateCodeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Absence",
                columns: table => new
                {
                    AbsenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Absence", x => x.AbsenceId);
                    table.ForeignKey(
                        name: "FK_Absence_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyAssessments",
                columns: table => new
                {
                    CompetencyAssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompetencyRevision = table.Column<int>(type: "int", nullable: false),
                    CompetencyDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompetencyObjective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompetencyId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyAssessments", x => x.CompetencyAssessmentId);
                    table.ForeignKey(
                        name: "FK_CompetencyAssessments_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Competencies",
                        principalColumn: "CompetencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetencyAssessments_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                columns: table => new
                {
                    TimesheetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Info = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DateStatusChanged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusChangedByPersonId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.TimesheetId);
                    table.ForeignKey(
                        name: "FK_Timesheets_People_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Timesheets_People_StatusChangedByPersonId",
                        column: x => x.StatusChangedByPersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleType = table.Column<int>(type: "int", nullable: false),
                    CASUserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    LastLoggedIn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                });

            migrationBuilder.CreateTable(
                name: "WorkloadModelChanges",
                columns: table => new
                {
                    WorkloadModelChangeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProjectWorkFTE = table.Column<double>(type: "float", nullable: false),
                    BusinessAsUsualFTE = table.Column<double>(type: "float", nullable: false),
                    PersonalDevelopmentFTE = table.Column<double>(type: "float", nullable: false),
                    StaffManagementFTE = table.Column<double>(type: "float", nullable: false),
                    ProjectAndServiceManagementFTE = table.Column<double>(type: "float", nullable: false),
                    ArchitectureFTE = table.Column<double>(type: "float", nullable: false),
                    ServiceManagementFTE = table.Column<double>(type: "float", nullable: false),
                    ProjectManagementFTE = table.Column<double>(type: "float", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkloadModelChanges", x => x.WorkloadModelChangeId);
                    table.ForeignKey(
                        name: "FK_WorkloadModelChanges_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OwnedSkills",
                columns: table => new
                {
                    OwnedSkillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerPersonId = table.Column<int>(type: "int", nullable: false),
                    SkillTagId = table.Column<int>(type: "int", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Proficiency = table.Column<int>(type: "int", nullable: false),
                    FavouriteSkill = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedSkills", x => x.OwnedSkillId);
                    table.ForeignKey(
                        name: "FK_OwnedSkills_People_OwnerPersonId",
                        column: x => x.OwnerPersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OwnedSkills_SkillTags_SkillTagId",
                        column: x => x.SkillTagId,
                        principalTable: "SkillTags",
                        principalColumn: "SkillTagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RTP = table.Column<int>(type: "int", nullable: false),
                    PI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    ProjectManagerPersonId = table.Column<int>(type: "int", nullable: true),
                    Budget = table.Column<double>(type: "float", nullable: false),
                    DayRate = table.Column<double>(type: "float", nullable: false),
                    CostModel = table.Column<int>(type: "int", nullable: false),
                    ProjectStatus = table.Column<int>(type: "int", nullable: false),
                    InnateActivityInnateCodeId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScrumProjectLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestDocLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlannedLeadershipCosts = table.Column<double>(type: "float", nullable: false),
                    ActualLeadershipCosts = table.Column<double>(type: "float", nullable: false),
                    BudgetedIndirects = table.Column<double>(type: "float", nullable: false),
                    ActualsLastUpdated = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannedWorkHours = table.Column<double>(type: "float", nullable: false),
                    ActualWorkHours = table.Column<double>(type: "float", nullable: false),
                    PlannedCost = table.Column<double>(type: "float", nullable: false),
                    ActualCost = table.Column<double>(type: "float", nullable: false),
                    PlannedIndirectCost = table.Column<double>(type: "float", nullable: false),
                    ActualIndirectCost = table.Column<double>(type: "float", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_InnateCodes_InnateActivityInnateCodeId",
                        column: x => x.InnateActivityInnateCodeId,
                        principalTable: "InnateCodes",
                        principalColumn: "InnateCodeId");
                    table.ForeignKey(
                        name: "FK_Projects_People_ProjectManagerPersonId",
                        column: x => x.ProjectManagerPersonId,
                        principalTable: "People",
                        principalColumn: "PersonId");
                    table.ForeignKey(
                        name: "FK_Projects_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "SchoolId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetEntries",
                columns: table => new
                {
                    TimesheetEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimesheetId = table.Column<int>(type: "int", nullable: false),
                    InnateCodeTaskId = table.Column<int>(type: "int", nullable: false),
                    MondayHours = table.Column<double>(type: "float", nullable: false),
                    TuesdayHours = table.Column<double>(type: "float", nullable: false),
                    WednesdayHours = table.Column<double>(type: "float", nullable: false),
                    ThursdayHours = table.Column<double>(type: "float", nullable: false),
                    FridayHours = table.Column<double>(type: "float", nullable: false),
                    SaturdayHours = table.Column<double>(type: "float", nullable: false),
                    SundayHours = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetEntries", x => x.TimesheetEntryId);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_InnateCodeTasks_InnateCodeTaskId",
                        column: x => x.InnateCodeTaskId,
                        principalTable: "InnateCodeTasks",
                        principalColumn: "InnateCodeTaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheets",
                        principalColumn: "TimesheetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    ApiKeyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.ApiKeyId);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundingSources",
                columns: table => new
                {
                    FundingSourceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundingSourceType = table.Column<int>(type: "int", nullable: false),
                    HasAccountCode = table.Column<bool>(type: "bit", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AmountAvailable = table.Column<double>(type: "float", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingSources", x => x.FundingSourceId);
                    table.ForeignKey(
                        name: "FK_FundingSources_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InvoiceUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    KeyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_Invoices_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    NoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditorUserId = table.Column<int>(type: "int", nullable: true),
                    IsFinanceInfo = table.Column<bool>(type: "bit", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.NoteId);
                    table.ForeignKey(
                        name: "FK_Notes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notes_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notes_Users_EditorUserId",
                        column: x => x.EditorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PersonProject",
                columns: table => new
                {
                    FollowedProjectsProjectId = table.Column<int>(type: "int", nullable: false),
                    FollowersPersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonProject", x => new { x.FollowedProjectsProjectId, x.FollowersPersonId });
                    table.ForeignKey(
                        name: "FK_PersonProject_People_FollowersPersonId",
                        column: x => x.FollowersPersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonProject_Projects_FollowedProjectsProjectId",
                        column: x => x.FollowedProjectsProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubTasks",
                columns: table => new
                {
                    SubTaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    PredecessorSubTaskId = table.Column<int>(type: "int", nullable: true),
                    HasFixedStart = table.Column<bool>(type: "bit", nullable: false),
                    HasFixedEndDate = table.Column<bool>(type: "bit", nullable: false),
                    Demand = table.Column<double>(type: "float", nullable: false),
                    OriginalDemand = table.Column<double>(type: "float", nullable: false),
                    UnmetDemand = table.Column<double>(type: "float", nullable: false),
                    Lag = table.Column<int>(type: "int", nullable: false),
                    OwningProjectProjectId = table.Column<int>(type: "int", nullable: false),
                    IsLeadershipTask = table.Column<bool>(type: "bit", nullable: false),
                    PlannedWorkHours = table.Column<double>(type: "float", nullable: false),
                    ActualWorkHours = table.Column<double>(type: "float", nullable: false),
                    PlannedCost = table.Column<double>(type: "float", nullable: false),
                    ActualCost = table.Column<double>(type: "float", nullable: false),
                    PlannedIndirectCost = table.Column<double>(type: "float", nullable: false),
                    ActualIndirectCost = table.Column<double>(type: "float", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    DurationBillableDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubTasks", x => x.SubTaskId);
                    table.ForeignKey(
                        name: "FK_SubTasks_Projects_OwningProjectProjectId",
                        column: x => x.OwningProjectProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubTasks_SubTasks_PredecessorSubTaskId",
                        column: x => x.PredecessorSubTaskId,
                        principalTable: "SubTasks",
                        principalColumn: "SubTaskId");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    SourceFundingSourceId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    KeyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_FundingSources_SourceFundingSourceId",
                        column: x => x.SourceFundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "FundingSourceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId");
                    table.ForeignKey(
                        name: "FK_Payments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    ResourceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    DayRate = table.Column<double>(type: "float", nullable: true),
                    AssignmentFTE = table.Column<double>(type: "float", nullable: false),
                    BilledFTE = table.Column<double>(type: "float", nullable: false),
                    IsProvisional = table.Column<bool>(type: "bit", nullable: false),
                    UseProjectDayRate = table.Column<bool>(type: "bit", nullable: false),
                    FundedFromFundingSourceId = table.Column<int>(type: "int", nullable: true),
                    SubTaskId = table.Column<int>(type: "int", nullable: false),
                    PlannedWorkHours = table.Column<double>(type: "float", nullable: false),
                    ActualWorkHours = table.Column<double>(type: "float", nullable: false),
                    PlannedCost = table.Column<double>(type: "float", nullable: false),
                    ActualCost = table.Column<double>(type: "float", nullable: false),
                    PlannedIndirectCost = table.Column<double>(type: "float", nullable: false),
                    ActualIndirectCost = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.ResourceId);
                    table.ForeignKey(
                        name: "FK_Resources_FundingSources_FundedFromFundingSourceId",
                        column: x => x.FundedFromFundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "FundingSourceId");
                    table.ForeignKey(
                        name: "FK_Resources_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Resources_SubTasks_SubTaskId",
                        column: x => x.SubTaskId,
                        principalTable: "SubTasks",
                        principalColumn: "SubTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillTagSubTask",
                columns: table => new
                {
                    SkillsRequiredSkillTagId = table.Column<int>(type: "int", nullable: false),
                    TasksNeedingThisSkillSubTaskId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTagSubTask", x => new { x.SkillsRequiredSkillTagId, x.TasksNeedingThisSkillSubTaskId });
                    table.ForeignKey(
                        name: "FK_SkillTagSubTask_SkillTags_SkillsRequiredSkillTagId",
                        column: x => x.SkillsRequiredSkillTagId,
                        principalTable: "SkillTags",
                        principalColumn: "SkillTagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillTagSubTask_SubTasks_TasksNeedingThisSkillSubTaskId",
                        column: x => x.TasksNeedingThisSkillSubTaskId,
                        principalTable: "SubTasks",
                        principalColumn: "SubTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Absence_PersonId",
                table: "Absence",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OwnerUserId",
                table: "ApiKeys",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyAssessments_CompetencyId",
                table: "CompetencyAssessments",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyAssessments_PersonId",
                table: "CompetencyAssessments",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSources_ProjectId",
                table: "FundingSources",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_InnateCodeTasks_InnateCodeId",
                table: "InnateCodeTasks",
                column: "InnateCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ProjectId",
                table: "Invoices",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_AuthorUserId",
                table: "Notes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_EditorUserId",
                table: "Notes",
                column: "EditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_ProjectId",
                table: "Notes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnedSkills_OwnerPersonId",
                table: "OwnedSkills",
                column: "OwnerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnedSkills_SkillTagId",
                table: "OwnedSkills",
                column: "SkillTagId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProjectId",
                table: "Payments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SourceFundingSourceId",
                table: "Payments",
                column: "SourceFundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_People_LineManagerPersonId",
                table: "People",
                column: "LineManagerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonProject_FollowersPersonId",
                table: "PersonProject",
                column: "FollowersPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_InnateActivityInnateCodeId",
                table: "Projects",
                column: "InnateActivityInnateCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectManagerPersonId",
                table: "Projects",
                column: "ProjectManagerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SchoolId",
                table: "Projects",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_FundedFromFundingSourceId",
                table: "Resources",
                column: "FundedFromFundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_PersonId",
                table: "Resources",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_SubTaskId",
                table: "Resources",
                column: "SubTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_FacultyId",
                table: "Schools",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillTagSubTask_TasksNeedingThisSkillSubTaskId",
                table: "SkillTagSubTask",
                column: "TasksNeedingThisSkillSubTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTasks_OwningProjectProjectId",
                table: "SubTasks",
                column: "OwningProjectProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTasks_PredecessorSubTaskId",
                table: "SubTasks",
                column: "PredecessorSubTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_InnateCodeTaskId",
                table: "TimesheetEntries",
                column: "InnateCodeTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetId",
                table: "TimesheetEntries",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_OwnerId",
                table: "Timesheets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_StatusChangedByPersonId",
                table: "Timesheets",
                column: "StatusChangedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonId",
                table: "Users",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkloadModelChanges_PersonId",
                table: "WorkloadModelChanges",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Absence");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "CompetencyAssessments");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "FinancialReferences");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "OwnedSkills");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PersonProject");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "SkillTagSubTask");

            migrationBuilder.DropTable(
                name: "TimesheetEntries");

            migrationBuilder.DropTable(
                name: "WorkloadModelChanges");

            migrationBuilder.DropTable(
                name: "Competencies");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "FundingSources");

            migrationBuilder.DropTable(
                name: "SkillTags");

            migrationBuilder.DropTable(
                name: "SubTasks");

            migrationBuilder.DropTable(
                name: "InnateCodeTasks");

            migrationBuilder.DropTable(
                name: "Timesheets");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "InnateCodes");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropTable(
                name: "Faculties");
        }
    }
}
