using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class InstitutionReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextInstitution",
                schema: "personal",
                table: "Relationships",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Institution",
                schema: "personal",
                table: "Affiliations",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Affiliations_Institution",
                schema: "personal",
                table: "Affiliations",
                column: "Institution");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Affiliations_Institution",
                schema: "personal",
                table: "Affiliations");

            migrationBuilder.DropColumn(
                name: "ContextInstitution",
                schema: "personal",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "Institution",
                schema: "personal",
                table: "Affiliations");
        }
    }
}
