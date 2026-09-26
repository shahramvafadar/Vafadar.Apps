using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Finance.Data.Migrations
{
    /// <inheritdoc />
    public partial class PartialPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entries_ScheduleId_OccurrenceDate",
                table: "Entries");

            migrationBuilder.AddColumn<long>(
                name: "PaidAmount",
                table: "OccurrenceStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsPartialPayment",
                table: "Entries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Entries_ScheduleId_OccurrenceDate",
                table: "Entries",
                columns: new[] { "ScheduleId", "OccurrenceDate" },
                unique: true,
                filter: "\"ScheduleId\" IS NOT NULL AND \"IsPartialPayment\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entries_ScheduleId_OccurrenceDate",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "OccurrenceStates");

            migrationBuilder.DropColumn(
                name: "IsPartialPayment",
                table: "Entries");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_ScheduleId_OccurrenceDate",
                table: "Entries",
                columns: new[] { "ScheduleId", "OccurrenceDate" },
                unique: true,
                filter: "\"ScheduleId\" IS NOT NULL");
        }
    }
}
