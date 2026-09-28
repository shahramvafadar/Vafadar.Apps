using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Finance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccountCounterparty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Counterparty",
                table: "Accounts",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Counterparty",
                table: "Accounts");
        }
    }
}
