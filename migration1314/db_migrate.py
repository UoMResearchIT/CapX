import sqlite3

OLD_DB = "PPMTool-v13.9.2.db"
NEW_DB = "PPMTool-v14.0.db"

def connect(path):
    conn = sqlite3.connect(path)
    conn.row_factory = sqlite3.Row
    return conn


def assert_count(old, new, table, where_old=None, where_new=None):
    wo = f" WHERE {where_old}" if where_old else ""
    wn = f" WHERE {where_new}" if where_new else ""
    c_old = old.execute(f"SELECT COUNT(*) FROM {table}{wo}").fetchone()[0]
    c_new = new.execute(f"SELECT COUNT(*) FROM {table}{wn}").fetchone()[0]
    if c_old != c_new:
        raise RuntimeError(
            f"Row count mismatch for {table}: old={c_old}, new={c_new}"
        )


# ─────────────────────────
# SIMPLE 1:1 TABLES
# ─────────────────────────

def migrate_simple(old, new, table, columns, where=None):
    cols = ", ".join(columns)
    w = f" WHERE {where}" if where else ""
    
    sql = f"""
        INSERT INTO {table} ({cols})
        SELECT {cols}
        FROM olddb.{table}{w}
    """
    print(sql.strip())
    
    new.execute(sql)
    assert_count(old, new, table, where_old=where)


# ─────────────────────────
# MIGRATIONS
# ─────────────────────────

def migrate_faculties(old, new):
    migrate_simple(old, new, "Faculties",
        ["FacultyId", "Name", "Code", "IsActive"])

def migrate_schools(old, new):
    migrate_simple(old, new, "Schools",
        ["SchoolId", "FacultyId", "Name", "Code", "IsActive"])

def migrate_features(old, new):
    migrate_simple(old, new, "Features",
        ["FeatureId", "FeatureType", "Name", "Description",
         "Enabled", "MustAlwaysBeEnabled"])

def migrate_financial_references(old, new):
    migrate_simple(old, new, "FinancialReferences",
        ["FinancialReferenceId", "FinancialYear",
         "Grade41Costs", "Grade51Costs", "Grade55Costs",
         "Grade65Costs", "Grade71Costs", "Grade75Costs",
         "RecoveryTarget"])

def migrate_innate_codes(old, new):
    migrate_simple(old, new, "InnateCodes",
        ["InnateCodeId", "ActivityCode",
         "ActivityName", "IsActive", "IsSensitive"])

def migrate_competencies(old, new):
    migrate_simple(old, new, "Competencies",
        ["CompetencyId", "Description", "Objective", "Grade",
         "Category", "Revision", "CreatedDate",
         "RevisionDate", "IsActive", "LegacyId", "Number"])

def migrate_skill_tags(old, new):
    migrate_simple(old, new, "SkillTags",
        ["SkillTagId", "Name", "ControlledName",
         "HasValidWikiLink", "Rareness", "RarenessCount"])

def migrate_people(old, new):
    rows = []
    for r in old.execute("SELECT * FROM People"):
        short = r["ShortName"] or r["Name"].split()[0]
        rows.append((
            r["PersonId"], r["Name"], short,
            r["StartDate"], r["EndDate"],
            r["FTE"], r["LineManagerPersonId"],
            r["TimesheetTemplateData"]
        ))
    new.executemany("""
        INSERT INTO People (
            PersonId, Name, ShortName,
            StartDate, EndDate,
            FTE, LineManagerPersonId, TimesheetTemplateData
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    """, rows)
    assert_count(old, new, "People")

def migrate_users(old, new):
    rows = []
    for r in old.execute("SELECT * FROM Users"):
        email = r["EmailAddress"] or f"{r['CASUserName']}@invalid.local"
        rows.append((
            r["UserId"], r["RoleType"], r["CASUserName"],
            r["Name"], r["PersonId"],
            r["LastLoggedIn"], email
        ))
    new.executemany("""
        INSERT INTO Users (
            UserId, RoleType, CASUserName,
            Name, PersonId, LastLoggedIn, EmailAddress
        ) VALUES (?, ?, ?, ?, ?, ?, ?)
    """, rows)
    assert_count(old, new, "Users")

def migrate_projects(old, new):
    migrate_simple(old, new, "Projects",
        ["ProjectId", "RTP", "PI", "SchoolId",
         "ProjectManagerPersonId", "Budget", "DayRate",
         "CostModel", "ProjectStatus", "InnateActivityInnateCodeId",
         "Description", "ScrumProjectLink", "RequestDocLink",
         "PlannedLeadershipCosts", "ActualLeadershipCosts",
         "BudgetedIndirects", "ActualsLastUpdated",
         "PlannedWorkHours", "ActualWorkHours",
         "PlannedCost", "ActualCost",
         "PlannedIndirectCost", "ActualIndirectCost",
         "Name", "StartDate", "EndDate"])

def migrate_subtasks(old, new):
    migrate_simple(old, new, "SubTasks",
        ["SubTaskId", "TaskType", "PredecessorSubTaskId",
         "HasFixedStart", "HasFixedEndDate",
         "Demand", "OriginalDemand", "UnmetDemand",
         "Lag", "OwningProjectProjectId", "IsLeadershipTask",
         "PlannedWorkHours", "ActualWorkHours",
         "PlannedCost", "ActualCost",
         "PlannedIndirectCost", "ActualIndirectCost",
         "Name", "StartDate", "EndDate",
         "DurationDays", "DurationBillableDays"],
        where="OwningProjectProjectId IS NOT NULL")

def migrate_innate_code_tasks(old, new):
    migrate_simple(old, new, "InnateCodeTasks",
        ["InnateCodeTaskId", "TaskName", "Duty", "InnateCodeId"])

def migrate_timesheets(old, new):
    migrate_simple(old, new, "Timesheets",
        ["TimesheetId", "OwnerId", "CreatedDate",
         "StartDate", "Info", "Status",
         "DateStatusChanged", "StatusChangedByPersonId"])

def migrate_timesheet_entries(old, new):
    migrate_simple(old, new, "TimesheetEntries",
        ["TimesheetEntryId", "TimesheetId", "InnateCodeTaskId",
         "MondayHours", "TuesdayHours", "WednesdayHours",
         "ThursdayHours", "FridayHours",
         "SaturdayHours", "SundayHours"])

def migrate_absence(old, new):
    migrate_simple(old, new, "Absence",
        ["AbsenceId", "StartDate", "EndDate", "PersonId"],
        where="PersonId IS NOT NULL")

def migrate_workload(old, new):
    migrate_simple(old, new, "WorkloadModelChanges",
        ["WorkloadModelChangeId", "Grade", "ChangeDate",
         "ProjectWorkFTE", "BusinessAsUsualFTE",
         "PersonalDevelopmentFTE", "StaffManagementFTE",
         "ProjectAndServiceManagementFTE", "ArchitectureFTE",
         "ServiceManagementFTE", "ProjectManagementFTE",
         "Notes", "PersonId"],
        where="PersonId IS NOT NULL")

def migrate_owned_skills(old, new):
    rows = []
    for r in old.execute("SELECT * FROM OwnedSkills"):
        rows.append((
            r["OwnedSkillId"], r["OwnerPersonId"], r["SkillTagId"],
            r["LastUsed"] or "0001-01-01 00:00:00",
            r["Proficiency"] or 0,
            r["FavouriteSkill"] or 0
        ))
    new.executemany("""
        INSERT INTO OwnedSkills (
            OwnedSkillId, OwnerPersonId, SkillTagId,
            LastUsed, Proficiency, FavouriteSkill
        ) VALUES (?, ?, ?, ?, ?, ?)
    """, rows)
    assert_count(old, new, "OwnedSkills")

def migrate_funding_sources(old, new):
    migrate_simple(old, new, "FundingSources",
        ["FundingSourceId", "FundingSourceType", "HasAccountCode",
         "AccountCode", "Description", "AmountAvailable",
         "ProjectId"])

def migrate_invoices(old, new):
    migrate_simple(old, new, "Invoices",
        ["InvoiceId", "InvoiceReference", "Status",
         "InvoiceUrl", "ProjectId", "KeyDate",
         "Value", "Description"])

def migrate_payments(old, new):
    migrate_simple(old, new, "Payments",
        ["PaymentId", "InvoiceId", "SourceFundingSourceId",
         "ProjectId", "KeyDate", "Value", "Description"])

def migrate_notes(old, new):
    migrate_simple(old, new, "Notes",
        ["NoteId", "HtmlContent", "AuthorUserId",
         "ProjectId", "CreatedDate", "EditedDate",
         "EditorUserId", "IsFinanceInfo",
         "DueDate", "CompletedDate"])

def migrate_person_project(old, new):
    migrate_simple(old, new, "PersonProject",
        ["FollowedProjectsProjectId", "FollowersPersonId"])

def migrate_api_keys(old, new):
    migrate_simple(old, new, "ApiKeys",
        ["ApiKeyId", "OwnerUserId", "Key",
         "Description", "Active", "ExpiresAt"])

def migrate_competency_assessments(old, new):
    migrate_simple(old, new, "CompetencyAssessments",
        ["CompetencyAssessmentId", "Evidence", "DateCreated",
         "Status", "CompetencyRevision",
         "CompetencyDescription", "CompetencyObjective",
         "CompetencyId", "PersonId"])

def migrate_resources(old, new):
    rows = []
    for r in old.execute("SELECT * FROM Resources"):
        rows.append((
            r["ResourceId"], r["PersonId"], r["DayRate"],
            r["AssignmentFTE"], r["BilledFTE"] or 0,
            r["IsProvisional"], r["UseProjectDayRate"],
            r["FundedFromFundingSourceId"],
            r["SubTaskId"],
            r["PlannedWorkHours"], r["ActualWorkHours"],
            r["PlannedCost"], r["ActualCost"],
            r["PlannedIndirectCost"], r["ActualIndirectCost"]
        ))
    new.executemany("""
        INSERT INTO Resources (
            ResourceId, PersonId, DayRate,
            AssignmentFTE, BilledFTE,
            IsProvisional, UseProjectDayRate,
            FundedFromFundingSourceId,
            SubTaskId,
            PlannedWorkHours, ActualWorkHours,
            PlannedCost, ActualCost,
            PlannedIndirectCost, ActualIndirectCost
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, rows)
    assert_count(old, new, "Resources")

def migrate_skill_tag_subtask(old, new):
    migrate_simple(old, new, "SkillTagSubTask",
        ["SkillsRequiredSkillTagId", "TasksNeedingThisSkillSubTaskId"])
        

def old_table_exists(old, table_name):
    """Return True when the source database contains the specified table."""
    row = old.execute("""
        SELECT 1
        FROM sqlite_master
        WHERE type = 'table'
          AND name = ?
    """, (table_name,)).fetchone()

    return row is not None


# ─────────────────────────
# MAIN
# ─────────────────────────

def main():
    old = connect(OLD_DB)
    new = connect(NEW_DB)

    try:
        new.execute("PRAGMA foreign_keys = OFF")
        new.execute("ATTACH DATABASE ? AS olddb", (OLD_DB,))
        new.execute("BEGIN")

        migrate_faculties(old, new)
        migrate_schools(old, new)
        if old_table_exists(old, "Features"):
            migrate_features(old, new)
        else:
            print("⚠️  Features table missing in old DB – skipping features migration")
        migrate_financial_references(old, new)
        migrate_innate_codes(old, new)
        migrate_competencies(old, new)
        migrate_skill_tags(old, new)
        migrate_people(old, new)
        migrate_users(old, new)
        migrate_projects(old, new)
        migrate_subtasks(old, new)
        migrate_innate_code_tasks(old, new)
        migrate_timesheets(old, new)
        migrate_timesheet_entries(old, new)
        migrate_absence(old, new)
        migrate_workload(old, new)
        migrate_owned_skills(old, new)
        migrate_funding_sources(old, new)
        migrate_invoices(old, new)
        migrate_payments(old, new)
        migrate_notes(old, new)
        migrate_person_project(old, new)
        migrate_api_keys(old, new)
        migrate_competency_assessments(old, new)
        migrate_resources(old, new)
        migrate_skill_tag_subtask(old, new)

        fk_violations = new.execute("PRAGMA foreign_key_check").fetchall()
        if fk_violations:
            violation = fk_violations[0]
            raise RuntimeError(
                "Foreign key violations detected after migration, "
                f"first violation: table={violation[0]}, rowid={violation[1]}, "
                f"parent={violation[2]}, fk_index={violation[3]}"
            )

        new.commit()
        print("✅ FULL migration completed successfully")

    except Exception:
        new.rollback()
        raise
    finally:
        new.execute("PRAGMA foreign_keys = ON")
        old.close()
        new.close()


if __name__ == "__main__":
    main()
