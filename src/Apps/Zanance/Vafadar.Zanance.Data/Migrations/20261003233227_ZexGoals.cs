using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <summary>
    /// Enhancement ZEX, phase 2: goal types (money set aside or the balance of one account), the paused state, Home pins,
    /// protected money and the completion time, and one contribution plan per goal. Existing goals become money set aside
    /// (type 0) and keep every number.
    /// </summary>
    public partial class ZexGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "Goals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompletedAt",
                table: "Goals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomePin",
                table: "Goals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PausedAt",
                table: "Goals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Protect",
                table: "Goals",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Goals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ContributionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GoalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: true),
                    Percent = table.Column<decimal>(type: "TEXT", precision: 9, scale: 4, nullable: true),
                    WeekendShift = table.Column<int>(type: "INTEGER", nullable: false),
                    HolidayRegion = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    WeekendDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    Interval = table.Column<int>(type: "INTEGER", nullable: false),
                    Start = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Calendar = table.Column<int>(type: "INTEGER", nullable: false),
                    DayRule = table.Column<int>(type: "INTEGER", nullable: false),
                    MissingDay = table.Column<int>(type: "INTEGER", nullable: false),
                    SecondDay = table.Column<int>(type: "INTEGER", nullable: true),
                    EndKind = table.Column<int>(type: "INTEGER", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: true),
                    CategoryIds = table.Column<string>(type: "TEXT", nullable: false),
                    ReminderEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionPlans_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Goals_AccountId",
                table: "Goals",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPlans_GoalId",
                table: "ContributionPlans",
                column: "GoalId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Accounts_AccountId",
                table: "Goals",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Accounts_AccountId",
                table: "Goals");

            migrationBuilder.DropTable(
                name: "ContributionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Goals_AccountId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "HomePin",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "Protect",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Goals");
        }
    }
}
