using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class BillingPurchaseAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingCustomerRow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Distribution = table.Column<int>(type: "integer", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    PublicIdentity = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AnonymousAccountBinding = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AccountToken = table.Column<Guid>(type: "uuid", nullable: false),
                    ProtectedRegistrationResponse = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    RegistrationIdentityHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingCustomerRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingCustomerRow_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingIdentityRow",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    AccountToken = table.Column<Guid>(type: "uuid", nullable: false),
                    GoogleAccountBinding = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingIdentityRow", x => new { x.UserId, x.Environment });
                    table.ForeignKey(
                        name: "FK_BillingIdentityRow_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingNotificationRow",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Issuer = table.Column<int>(type: "integer", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    ProtectedReference = table.Column<string>(type: "character varying(32768)", maxLength: 32768, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    LeaseOwner = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingNotificationRow", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingCommandRow",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedRequest = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    ProtectedWork = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    CompletedWork = table.Column<int>(type: "integer", nullable: false),
                    LastState = table.Column<int>(type: "integer", nullable: true),
                    ProtectedResponse = table.Column<string>(type: "character varying(262144)", maxLength: 262144, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingCommandRow", x => new { x.CustomerId, x.Id });
                    table.ForeignKey(
                        name: "FK_BillingCommandRow_BillingCustomerRow_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "BillingCustomerRow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingPurchaseRow",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Issuer = table.Column<int>(type: "integer", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    ProtectedReference = table.Column<string>(type: "character varying(32768)", maxLength: 32768, nullable: false),
                    ProviderAccountBinding = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Verified = table.Column<bool>(type: "boolean", nullable: false),
                    Product = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    AccessUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FactsJson = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    AcknowledgePending = table.Column<bool>(type: "boolean", nullable: false),
                    RecheckAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedRecheckRevision = table.Column<long>(type: "bigint", nullable: false),
                    RecheckFailures = table.Column<int>(type: "integer", nullable: false),
                    LeaseOwner = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPurchaseRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingPurchaseRow_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingPurchaseRow_BillingCustomerRow_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "BillingCustomerRow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingDevicePurchaseRow",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingDevicePurchaseRow", x => new { x.CustomerId, x.PurchaseId });
                    table.ForeignKey(
                        name: "FK_BillingDevicePurchaseRow_BillingCustomerRow_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "BillingCustomerRow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingDevicePurchaseRow_BillingPurchaseRow_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "BillingPurchaseRow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingPurchaseAliasRow",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PurchaseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPurchaseAliasRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingPurchaseAliasRow_BillingPurchaseRow_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "BillingPurchaseRow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingCommandRow_ExpiresAt",
                table: "BillingCommandRow",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BillingCustomerRow_AccountToken",
                table: "BillingCustomerRow",
                column: "AccountToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingCustomerRow_Distribution_Environment_AnonymousAccoun~",
                table: "BillingCustomerRow",
                columns: new[] { "Distribution", "Environment", "AnonymousAccountBinding" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingCustomerRow_UserId_Environment_Revoked",
                table: "BillingCustomerRow",
                columns: new[] { "UserId", "Environment", "Revoked" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingDevicePurchaseRow_PurchaseId",
                table: "BillingDevicePurchaseRow",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingIdentityRow_AccountToken",
                table: "BillingIdentityRow",
                column: "AccountToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingIdentityRow_GoogleAccountBinding",
                table: "BillingIdentityRow",
                column: "GoogleAccountBinding",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingNotificationRow_Completed_RetryAt_LeaseUntil",
                table: "BillingNotificationRow",
                columns: new[] { "Completed", "RetryAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPurchaseAliasRow_PurchaseId",
                table: "BillingPurchaseAliasRow",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPurchaseRow_AcknowledgePending_LeaseUntil",
                table: "BillingPurchaseRow",
                columns: new[] { "AcknowledgePending", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPurchaseRow_CustomerId",
                table: "BillingPurchaseRow",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPurchaseRow_State_AccessUntil",
                table: "BillingPurchaseRow",
                columns: new[] { "State", "AccessUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPurchaseRow_UserId_Environment",
                table: "BillingPurchaseRow",
                columns: new[] { "UserId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPurchaseRow_Verified_RecheckAt_LeaseUntil",
                table: "BillingPurchaseRow",
                columns: new[] { "Verified", "RecheckAt", "LeaseUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingCommandRow");

            migrationBuilder.DropTable(
                name: "BillingDevicePurchaseRow");

            migrationBuilder.DropTable(
                name: "BillingIdentityRow");

            migrationBuilder.DropTable(
                name: "BillingNotificationRow");

            migrationBuilder.DropTable(
                name: "BillingPurchaseAliasRow");

            migrationBuilder.DropTable(
                name: "BillingPurchaseRow");

            migrationBuilder.DropTable(
                name: "BillingCustomerRow");
        }
    }
}
