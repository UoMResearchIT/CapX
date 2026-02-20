// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum CompetencyCategory
    {
        [Icon("article")]
        General,
        [Description("Department Culture and Cohesion")]
        [Icon("diversity_1")]
        Culture,
        [Icon("forum")]
        Communication,
        [Description("Version Control")]
        [Icon("conversion_path")]
        VersionControl,
        [Description("Technical Architecture")]
        [Icon("architecture")]
        Architecture,
        [Description("Engineering Process")]
        [Icon("engineering")]
        EngineeringProcess,
        [Description("Coding and Development Practices")]
        [Icon("code")]
        DevelopmentPractice,
        [Description("Departmental Processes and Practices")]
        [Icon("rebase")]
        DepartmentalProcess,
        [Description("Data Management")]
        [Icon("storage")]
        DataManagement,
        [Icon("leaderboard")]
        Leadership,
        [Description("Training, Coaching and Community Work")]
        [Icon("school")]
        Training,
        [Description("Staff and Operational Management")]
        [Icon("supervisor_account")]
        StaffManagement,
        [Description("Service and Project Management")]
        [Icon("support_agent")]
        ServiceProjectManagement
    }
}
