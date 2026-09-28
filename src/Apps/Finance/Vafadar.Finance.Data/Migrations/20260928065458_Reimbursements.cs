using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Finance.Data.Migrations
{
    /// <inheritdoc />
    public partial class Reimbursements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReimbursableAmount",
                table: "Entries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReimbursedBy",
                table: "Entries",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReimbursableAmount",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "ReimbursedBy",
                table: "Entries");
        }
    }
}
