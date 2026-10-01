using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class FlexBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SpendingType",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // The default bills get the spending type a new install gives them (DefaultCategories.SpendingTypeOf).
            migrationBuilder.Sql("UPDATE Categories SET SpendingType = 1 WHERE Kind = 0 AND SystemKey IN ('Housing', 'Energy', 'Communication', 'Subscriptions');");
            migrationBuilder.Sql("UPDATE Categories SET SpendingType = 2 WHERE Kind = 0 AND SystemKey = 'Insurance';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpendingType",
                table: "Categories");
        }
    }
}
