using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtrocidadesRSS.Generator.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTypes", x => x.Id);
                    table.CheckConstraint("CK_CaseTypes_Confidence", "Confidence >= 0 AND Confidence <= 100");
                });

            migrationBuilder.CreateTable(
                name: "CrimeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrimeTypes", x => x.Id);
                    table.CheckConstraint("CK_CrimeTypes_Confidence", "Confidence >= 0 AND Confidence <= 100");
                });

            migrationBuilder.CreateTable(
                name: "JudicialStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JudicialStatuses", x => x.Id);
                    table.CheckConstraint("CK_JudicialStatuses_Confidence", "Confidence >= 0 AND Confidence <= 100");
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CrimeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VictimName = table.Column<string>(type: "text", nullable: true),
                    VictimGender = table.Column<string>(type: "text", nullable: true),
                    VictimAge = table.Column<int>(type: "integer", nullable: true),
                    VictimNationality = table.Column<string>(type: "text", nullable: true),
                    VictimProfession = table.Column<string>(type: "text", nullable: true),
                    VictimRelationshipToAccused = table.Column<string>(type: "text", nullable: true),
                    VictimConfidence = table.Column<int>(type: "integer", nullable: false),
                    AccusedName = table.Column<string>(type: "text", nullable: true),
                    AccusedSocialName = table.Column<string>(type: "text", nullable: true),
                    AccusedGender = table.Column<string>(type: "text", nullable: true),
                    AccusedAge = table.Column<int>(type: "integer", nullable: true),
                    AccusedNationality = table.Column<string>(type: "text", nullable: true),
                    AccusedProfession = table.Column<string>(type: "text", nullable: true),
                    AccusedDocument = table.Column<string>(type: "text", nullable: true),
                    AccusedAddress = table.Column<string>(type: "text", nullable: true),
                    AccusedRelationshipToVictim = table.Column<string>(type: "text", nullable: true),
                    AccusedConfidence = table.Column<int>(type: "integer", nullable: false),
                    CrimeTypeId = table.Column<int>(type: "integer", nullable: false),
                    CrimeSubtype = table.Column<string>(type: "text", nullable: true),
                    EstimatedCrimeDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CrimeLocationAddress = table.Column<string>(type: "text", nullable: true),
                    CrimeLocationCity = table.Column<string>(type: "text", nullable: true),
                    CrimeLocationState = table.Column<string>(type: "text", nullable: true),
                    CrimeCoordinates = table.Column<string>(type: "text", nullable: true),
                    CrimeDescription = table.Column<string>(type: "text", nullable: true),
                    CaseTypeId = table.Column<int>(type: "integer", nullable: false),
                    NumberOfVictims = table.Column<int>(type: "integer", nullable: false),
                    NumberOfAccused = table.Column<int>(type: "integer", nullable: false),
                    WeaponUsed = table.Column<string>(type: "text", nullable: true),
                    Motivation = table.Column<string>(type: "text", nullable: true),
                    Premeditation = table.Column<string>(type: "text", nullable: true),
                    CrimeConfidence = table.Column<int>(type: "integer", nullable: false),
                    JudicialStatusId = table.Column<int>(type: "integer", nullable: false),
                    ProcessNumber = table.Column<string>(type: "text", nullable: true),
                    Court = table.Column<string>(type: "text", nullable: true),
                    County = table.Column<string>(type: "text", nullable: true),
                    CurrentPhase = table.Column<string>(type: "text", nullable: true),
                    JudicialReportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentencingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Sentence = table.Column<string>(type: "text", nullable: true),
                    PendingAppeals = table.Column<string>(type: "text", nullable: true),
                    JudicialConfidence = table.Column<int>(type: "integer", nullable: false),
                    MainCategory = table.Column<string>(type: "text", nullable: true),
                    IsSensitiveContent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AnonymizationStatus = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CuratorId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.CheckConstraint("CK_Cases_AccusedConfidence", "AccusedConfidence >= 0 AND AccusedConfidence <= 100");
                    table.CheckConstraint("CK_Cases_CrimeConfidence", "CrimeConfidence >= 0 AND CrimeConfidence <= 100");
                    table.CheckConstraint("CK_Cases_JudicialConfidence", "JudicialConfidence >= 0 AND JudicialConfidence <= 100");
                    table.CheckConstraint("CK_Cases_VictimConfidence", "VictimConfidence >= 0 AND VictimConfidence <= 100");
                    table.ForeignKey(
                        name: "FK_Cases_CaseTypes_CaseTypeId",
                        column: x => x.CaseTypeId,
                        principalTable: "CaseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_CrimeTypes_CrimeTypeId",
                        column: x => x.CrimeTypeId,
                        principalTable: "CrimeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_JudicialStatuses_JudicialStatusId",
                        column: x => x.JudicialStatusId,
                        principalTable: "JudicialStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseFieldHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaseId = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CuratorId = table.Column<string>(type: "text", nullable: true),
                    ChangeReason = table.Column<string>(type: "text", nullable: true),
                    ChangeConfidence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFieldHistory", x => x.Id);
                    table.CheckConstraint("CK_CaseFieldHistory_Confidence", "ChangeConfidence >= 0 AND ChangeConfidence <= 100");
                    table.ForeignKey(
                        name: "FK_CaseFieldHistory_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseTags",
                columns: table => new
                {
                    CaseId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTags", x => new { x.CaseId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CaseTags_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Evidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaseId = table.Column<int>(type: "integer", nullable: false),
                    EvidenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    Witnesses = table.Column<string>(type: "text", nullable: true),
                    Forensics = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidence", x => x.Id);
                    table.CheckConstraint("CK_Evidence_Confidence", "Confidence >= 0 AND Confidence <= 100");
                    table.ForeignKey(
                        name: "FK_Evidence_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaseId = table.Column<int>(type: "integer", nullable: false),
                    SourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PostDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OriginalLink = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Upvotes = table.Column<int>(type: "integer", nullable: true),
                    CommentsCount = table.Column<int>(type: "integer", nullable: true),
                    CurationNotes = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                    table.CheckConstraint("CK_Sources_Confidence", "Confidence >= 0 AND Confidence <= 100");
                    table.ForeignKey(
                        name: "FK_Sources_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldHistory_CaseId",
                table: "CaseFieldHistory",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseFieldHistory_ChangedAt",
                table: "CaseFieldHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_AccusedName",
                table: "Cases",
                column: "AccusedName");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CaseTypeId",
                table: "Cases",
                column: "CaseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CrimeDate",
                table: "Cases",
                column: "CrimeDate");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CrimeLocationCity",
                table: "Cases",
                column: "CrimeLocationCity");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CrimeLocationState",
                table: "Cases",
                column: "CrimeLocationState");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CrimeTypeId",
                table: "Cases",
                column: "CrimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_JudicialStatusId",
                table: "Cases",
                column: "JudicialStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ReferenceCode",
                table: "Cases",
                column: "ReferenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_VictimName",
                table: "Cases",
                column: "VictimName");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTags_TagId",
                table: "CaseTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTypes_Name",
                table: "CaseTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrimeTypes_Name",
                table: "CrimeTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evidence_CaseId",
                table: "Evidence",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_JudicialStatuses_Name",
                table: "JudicialStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sources_CaseId",
                table: "Sources",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Category",
                table: "Tags",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseFieldHistory");

            migrationBuilder.DropTable(
                name: "CaseTags");

            migrationBuilder.DropTable(
                name: "Evidence");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "CaseTypes");

            migrationBuilder.DropTable(
                name: "CrimeTypes");

            migrationBuilder.DropTable(
                name: "JudicialStatuses");
        }
    }
}
