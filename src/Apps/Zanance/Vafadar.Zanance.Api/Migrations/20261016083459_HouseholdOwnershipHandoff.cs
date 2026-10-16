using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdOwnershipHandoff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdOwnershipTransfers",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OwnerDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TargetDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignedOffer = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    PreparationJson = table.Column<string>(type: "character varying(2097152)", maxLength: 2097152, nullable: true),
                    SignedStatement = table.Column<string>(type: "character varying(131072)", maxLength: 131072, nullable: true),
                    AcceptanceJson = table.Column<string>(type: "character varying(2097152)", maxLength: 2097152, nullable: true),
                    PublishedProofJson = table.Column<string>(type: "character varying(524288)", maxLength: 524288, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdOwnershipTransfers", x => new { x.SpaceId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_HouseholdOwnershipTransfers_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdOwnershipTransfers_SpaceId",
                table: "HouseholdOwnershipTransfers",
                column: "SpaceId",
                unique: true,
                filter: "\"Stage\" >= 1 AND \"Stage\" <= 4");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdOwnershipTransfers_TargetId_Stage",
                table: "HouseholdOwnershipTransfers",
                columns: new[] { "TargetId", "Stage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdOwnershipTransfers");
        }
    }
}
