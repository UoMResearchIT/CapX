// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPMTool.Migrations
{
    public partial class AddedInnateCodeTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InnateActivity",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "InnateActivityInnateCodeId",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InnateCodes",
                columns: table => new
                {
                    InnateCodeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivityCode = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnateCodes", x => x.InnateCodeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_InnateActivityInnateCodeId",
                table: "Projects",
                column: "InnateActivityInnateCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_InnateCodes_InnateActivityInnateCodeId",
                table: "Projects",
                column: "InnateActivityInnateCodeId",
                principalTable: "InnateCodes",
                principalColumn: "InnateCodeId");

            migrationBuilder.Sql(
                @"
                    INSERT INTO InnateCodes (ActivityCode, ActivityName) VALUES
                    ('S-RES001', 'Research Consultancy (short term)'),
                    ('S-RES002', 'Research Assignments'),
                    ('S-RES003', 'Research infrastructure'),
                    ('S-RES004', 'Research IT External Engagement'),
                    ('S-RES005', 'Research Sysadmin Service'),
                    ('S-RES006', 'Research IT Training'),
                    ('S-RES007', 'Mobile Development Service'),
                    ('S-RES008', 'Visualization and Data Analysis Lab'),
                    ('S-RES009', 'Research Data Management'),
                    ('S-RES010', 'Edge Compute and Satellite Storage Service'),
                    ('S-RES011', 'Web Application Development Service'),
                    ('S-RES012', 'Application Support Service'),
                    ('S-RES013', 'Research Communities and Peer Support'),
                    ('S-RES014', 'Research IT Development and Wellbeing'),
                    ('S-RES015', 'Research Software Architecture'),
                    ('S-RES-P001', 'Creative Data Science (CS, Jay)'),
                    ('S-RES-P002', 'IDInteraction (CS, Jay)'),
                    ('S-RES-P004', 'SYNBIOCHEM (MIB, Le Feuvre; CS, Goble)'),
                    ('S-RES-P005', 'SE Teaching Materials (CS, Jay; ITS, Haines)'),
                    ('S-RES-P006', 'DEMAA (CS, Jay)'),
                    ('S-RES-P007', 'eTekkatho (FLS, Tun, Walton)'),
                    ('S-RES-P008', 'Food Pathogens (Hum, Rigby)'),
                    ('S-RES-P009', 'Hippocratic Aphorisms (JRRI, Pormann)'),
                    ('S-RES-P010', 'ESRC/NIHR Neighbourhoods and Dementia Study (Nursing, Hall)'),
                    ('S-RES-P011', 'Research Lifecycle Predefine'),
                    ('S-RES-P012', 'Health Data Science'),
                    ('S-RES-P013', 'CityVerve'),
                    ('S-RES-P015', 'Digital Humanities'),
                    ('S-RES-P016', 'REVISIT'),
                    ('S-RES-P017', 'PROCAS-2'),
                    ('S-RES-P020', 'BBC Data Science Research Partnership'),
                    ('S-RES-P022', 'Institute of Coding'),
                    ('S-RES-P023', 'GCRF Brazil Breathing'),
                    ('S-RES-P024', 'QPlus (Parisio; EEE)'),
                    ('S-RES-P025', 'AuditCloud'),
                    ('S-RES-P026', 'Data viz for smarter mid-trial decisions (AZ/UoM)'),
                    ('S-RES-P027', 'PINGR - CHI (Jung, Brown)'),
                    ('S-RES-P028', 'Connected Health - CHI (Machin)'),
                    ('S-RES-P029', 'MOVING (Vigo)'),
                    ('S-RES-P030', 'Urban Observatory (Evans, Topping)'),
                    ('S-RES-P032', 'Radiography Probabilistic Planning (Christie, Osorio)'),
                    ('S-RES-P033', 'PICo (Haroon, Azadbakht)'),
                    ('S-RES-P034', 'Home Offshore (Nenadic)'),
                    ('S-RES-P035', 'MACE handover (Adrian)'),
                    ('S-RES-P036', 'CALM pipe analysis'),
                    ('S-RES-P037', 'BioExcel (Goble)'),
                    ('S-RES-P038', 'Stoller Backfill'),
                    ('S-RES-P039', 'CHERIL (Hall)'),
                    ('S-RES-P040', 'Caroline Jay Turing Fellowship'),
                    ('S-RES-P041', 'HiLeMMS (Revell)'),
                    ('S-RES-P042', 'MuSTEM (Eric Prestat)'),
                    ('S-RES-P043', 'TeSS (Goble)'),
                    ('S-RES-P045', 'SALVE (Dinsdale)'),
                    ('S-RES-P046', 'NDEC'),
                    ('S-RES-P048', 'RIT COVID-19 Response'),
                    ('S-RES-P050', 'ExposureBee (Launder-Polya)'),
                    ('S-RES-P051', 'Memories of the Gay Village (Balmer, Barron)'),
                    ('S-RES-P053', 'Recovery, Renewal, Resilient (Shaw)'),
                    ('S-RES-P054', 'COVID TTI Simulation (Jay)'),
                    ('S-RES-P056', 'N8-CIR'),
                    ('S-RES-P057', 'BRIAN Imaris Plugin (Milosavljevic)'),
                    ('S-RES-P059', 'NVIDIA Omniverse Demonstrator (Margetts)'),
                    ('S-RES-P060', 'HEET Future Dams (Kuriakose)'),
                    ('S-RES-P061', 'Spanish Plume (Schultz)'),
                    ('S-RES-P064', 'Vaccination Data Analytics (Chen)'),
                    ('S-RES-P066', 'ARCHER2 eCSE (Revell)'),
                    ('S-RES-P068', 'Air Quality Prediction (Topping)'),
                    ('S-RES-P071', 'UoM Library'),
                    ('S-RES-P072', 'EOSC Life (Goble)'),
                    ('S-RES-P073', 'NERC Workshop SDEES (Schultz)'),
                    ('S-RES-P076', 'Python GUI for CPF (Hunt)'),
                    ('S-RES-RTP-100', 'KWN Deformation Tool (Joao Quinta da Fonseca)'),
                    ('S-RES-RTP-102', 'CDI (Rattray)'),
                    ('S-RES-RTP-105', 'GPU / ML Data Compression (Camps Santasmasas)'),
                    ('S-RES-RTP-106', 'LightForm (Materials, Quinta da Fonseca)'),
                    ('S-RES-RTP-109', 'Determinants of COVID-19 vaccine hesitancy and acceptance'),
                    ('S-RES-RTP-115', 'Health Data Science (Lai)'),
                    ('S-RES-RTP-119', 'DBO-IM (Margetts)'),
                    ('S-RES-RTP-121', 'TOGETHER Mobile App'),
                    ('S-RES-RTP-122', 'Text Mining Tool (Kasmire)'),
                    ('S-RES-RTP-123', 'BioDT (eScience Lab)'),
                    ('S-RES-RTP-126', 'WRF and AI'),
                    ('S-RES-RTP-128', 'RADNET (Dickie)'),
                    ('S-RES-RTP-129', 'VR Trust Game (Clinch)'),
                    ('S-RES-RTP-135', 'TICM (Taylor)'),
                    ('S-RES-RTP-137', 'DARE-FX / HDR-UK (eScience Lab)'),
                    ('S-RES-RTP-14', 'Researcher Connection Tool (Faroni)'),
                    ('S-RES-RTP-140', 'Chemistry Lab System (Wan)'),
                    ('S-RES-RTP-145', 'GiFT (Weightman)'),
                    ('S-RES-RTP-149', 'iHelp Phase 2 (Ken Muir)'),
                    ('S-RES-RTP-154', 'NERC Workshop 2023/24'),
                    ('S-RES-RTP-155', 'FASE Hydraulic Press Control (Hunt)'),
                    ('S-RES-RTP-16', 'GRASS (Meah)'),
                    ('S-RES-RTP-167', 'OACP v2 Part 4'),
                    ('S-RES-RTP-168', 'ECG-X LQTS (Jay)'),
                    ('S-RES-RTP-17', 'CRM Data Mapping (Archer)'),
                    ('S-RES-RTP-173', 'Mathworks Toolbox Work'),
                    ('S-RES-RTP-179', 'PURE Hosting Project (Milin-Chalabi)'),
                    ('S-RES-RTP-18', 'Productivity Lab Portal (Ortega-Argiles)'),
                    ('S-RES-RTP-180', 'Polypharmacy KSS (Tjeerd van staa)'),
                    ('S-RES-RTP-182', 'EBRAINS 2 (Bertozzi)'),
                    ('S-RES-RTP-183', 'QinetiQ (John Goodacre)'),
                    ('S-RES-RTP-190', 'Brain Cancer Detection Tool (Hamerlik)'),
                    ('S-RES-RTP-191', 'Small Data (Phase 2) (Parkinson)'),
                    ('S-RES-RTP-197', 'Personal Exposure Model Rebuild (Topping)'),
                    ('S-RES-RTP-203', 'HEET (Kuriakose)'),
                    ('S-RES-RTP-206', 'Tool Tinder (Walsh)'),
                    ('S-RES-RTP-212', 'Hydra (Bull)'),
                    ('S-RES-RTP-222', 'ECG-X Web Site (Jay)'),
                    ('S-RES-RTP-224', 'Future Data Services (Mark Elliot)'),
                    ('S-RES-RTP-227', 'Continental European Books in Early Modern England (Nilani Ganeshwaran)'),
                    ('S-RES-RTP-23', 'Vaccination Acceptance Analysis (Yu-wang Chen)'),
                    ('S-RES-RTP-232', 'VR Mindfulness'),
                    ('S-RES-RTP-24', 'Dialectics of Modernity'),
                    ('S-RES-RTP-25', 'Conflict, Memory and Migration (Harte)'),
                    ('S-RES-RTP-31', 'Royce Catalogue (Race)'),
                    ('S-RES-RTP-32', 'DfT Project (Topping)'),
                    ('S-RES-RTP-33', 'Spanish Plume (Schultz)'),
                    ('S-RES-RTP-34', 'SEEK (Goble)'),
                    ('S-RES-RTP-38', 'Research CFD software in T&L (Revell)'),
                    ('S-RES-RTP-40', 'Transcriptomics (Morias)'),
                    ('S-RES-RTP-41', 'Biologic Studies Group (Kath Watson)'),
                    ('S-RES-RTP-42', 'Systemic Sclerosis Imaging (Berks)'),
                    ('S-RES-RTP-43', 'Manchester Proteome'),
                    ('S-RES-RTP-45', 'Cell-Matrix Platform Support (Lennon)'),
                    ('S-RES-RTP-46', 'KOKU (Stanmore)'),
                    ('S-RES-RTP-47', 'e-Lab - CHI (Couch)'),
                    ('S-RES-RTP-48', 'Data Acquisition Project (Parkinson)'),
                    ('S-RES-RTP-50', 'LEAP (Chernyavsky/Heazell)'),
                    ('S-RES-RTP-52', 'Manc Risk Screen (Rogers)'),
                    ('S-RES-RTP-58', 'Gene Prediction Tool (Morris)'),
                    ('S-RES-RTP-59', 'AInostics (Haroon, Azadbakht)'),
                    ('S-RES-RTP-61', 'Digital Emerging Cancer Medicine Team (AZ/UoM)'),
                    ('S-RES-RTP-62', 'NERC Digital Solutions Hub'),
                    ('S-RES-RTP-64', 'NCISH'),
                    ('S-RES-RTP-86', 'Turner Website Improvement'),
                    ('S-RES-RTP-87', 'Author Matching (Hum, Nini)'),
                    ('S-RES-RTP-88', 'James Baldwin and Britain (Douglas Field)'),
                    ('S-RES-RTP-89', 'Human Brain Project (CS, Furber)'),
                    ('S-RES-RTP-93', 'CIDAR (Garcia-Carreras)'),
                    ('S-RES-RTP-96', 'ECG-X (Jay)'),
                    ('S-RES-RTP-97', 'Sami Kaski World Leading AI Fellowship'),
                    ('S-RES-RTP-98', 'HearX (Kluk-de Kort)'),
                    ('S-RES-RTP-99', 'AI Foundry (IDSAI)');
                "
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_InnateCodes_InnateActivityInnateCodeId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "InnateCodes");

            migrationBuilder.DropIndex(
                name: "IX_Projects_InnateActivityInnateCodeId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "InnateActivityInnateCodeId",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "InnateActivity",
                table: "Projects",
                type: "TEXT",
                nullable: true);
        }
    }
}
