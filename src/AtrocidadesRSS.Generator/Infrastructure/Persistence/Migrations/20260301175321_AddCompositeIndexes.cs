using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtrocidadesRSS.Generator.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cases_CrimeTypeId",
                table: "Cases");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CrimeLocationState_CrimeDate",
                table: "Cases",
                columns: new[] { "CrimeLocationState", "CrimeDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CrimeTypeId_JudicialStatusId",
                table: "Cases",
                columns: new[] { "CrimeTypeId", "JudicialStatusId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cases_CrimeLocationState_CrimeDate",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_CrimeTypeId_JudicialStatusId",
                table: "Cases");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CrimeTypeId",
                table: "Cases",
                column: "CrimeTypeId");
        }
    }
}
