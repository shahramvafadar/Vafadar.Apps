using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdLifecycleRetirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AuditBytes",
                table: "HouseholdSpaces",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AuditCursor",
                table: "HouseholdSpaces",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "HouseholdSpaces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionActorHash",
                table: "HouseholdSpaces",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletionOperationId",
                table: "HouseholdSpaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletionReason",
                table: "HouseholdSpaces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionRequestHash",
                table: "HouseholdSpaces",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HouseholdAudit",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    MembershipHead = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PublicationCursor = table.Column<long>(type: "bigint", nullable: false),
                    PublicationHead = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EvidenceJson = table.Column<string>(type: "character varying(524288)", maxLength: 524288, nullable: false),
                    Bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdAudit", x => new { x.SpaceId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_HouseholdAudit_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdSpaces_Deleted_StorageRetired",
                table: "HouseholdSpaces",
                columns: new[] { "Deleted", "StorageRetired" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdSpaces_HostExpiresAt",
                table: "HouseholdSpaces",
                column: "HostExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdAudit");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdSpaces_Deleted_StorageRetired",
                table: "HouseholdSpaces");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdSpaces_HostExpiresAt",
                table: "HouseholdSpaces");

            migrationBuilder.DropColumn(
                name: "AuditBytes",
                table: "HouseholdSpaces");

            migrationBuilder.DropColumn(
                name: "AuditCursor",
                table: "HouseholdSpaces");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "HouseholdSpaces");

            migrationBuilder.DropColumn(
                name: "DeletionActorHash",
                table: "HouseholdSpaces");

            migrationBuilder.DropColumn(
                name: "DeletionOperationId",
                table: "HouseholdSpaces");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "HouseholdSpaces");

            migrationBuilder.DropColumn(
                name: "DeletionRequestHash",
                table: "HouseholdSpaces");
        }
    }
}
