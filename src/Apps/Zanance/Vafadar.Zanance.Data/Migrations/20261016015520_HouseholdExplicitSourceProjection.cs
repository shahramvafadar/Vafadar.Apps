using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdExplicitSourceProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectionContentHash",
                table: "HouseholdLocalBinding",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectionReceiptJson",
                table: "HouseholdLocalBinding",
                type: "TEXT",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectionReviewId",
                table: "HouseholdLocalBinding",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectionSourceDigest",
                table: "HouseholdLocalBinding",
                type: "TEXT",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectionContentHash",
                table: "HouseholdLocalBinding");

            migrationBuilder.DropColumn(
                name: "ProjectionReceiptJson",
                table: "HouseholdLocalBinding");

            migrationBuilder.DropColumn(
                name: "ProjectionReviewId",
                table: "HouseholdLocalBinding");

            migrationBuilder.DropColumn(
                name: "ProjectionSourceDigest",
                table: "HouseholdLocalBinding");
        }
    }
}
