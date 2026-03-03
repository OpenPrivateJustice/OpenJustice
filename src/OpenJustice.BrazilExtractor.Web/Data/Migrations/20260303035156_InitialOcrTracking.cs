using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJustice.BrazilExtractor.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialOcrTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OcrPageRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExecutionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PdfPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    PageNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CharactersExtracted = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrPageRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OcrPageRecords_CompositeKey",
                table: "OcrPageRecords",
                columns: new[] { "ExecutionDate", "PdfPath", "PageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OcrPageRecords_ExecutionDate",
                table: "OcrPageRecords",
                column: "ExecutionDate");

            migrationBuilder.CreateIndex(
                name: "IX_OcrPageRecords_PdfPath",
                table: "OcrPageRecords",
                column: "PdfPath");

            migrationBuilder.CreateIndex(
                name: "IX_OcrPageRecords_Pending",
                table: "OcrPageRecords",
                columns: new[] { "ExecutionDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OcrPageRecords_Status",
                table: "OcrPageRecords",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OcrPageRecords");
        }
    }
}
