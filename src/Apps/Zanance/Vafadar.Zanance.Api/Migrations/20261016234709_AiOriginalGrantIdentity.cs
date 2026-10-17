using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiOriginalGrantIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve the original SQL invariant while making the same relationship visible to EF insert ordering.
            migrationBuilder.Sql("ALTER TABLE \"AiCredits\" DROP CONSTRAINT IF EXISTS \"FK_AiCredits_Request\";");

            migrationBuilder.DropIndex(
                name: "IX_AiCredits_UserId_GrantKey",
                table: "AiCredits");

            migrationBuilder.CreateIndex(
                name: "IX_AiCredits_GrantKey",
                table: "AiCredits",
                column: "GrantKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AiCredits_Request",
                table: "AiCredits",
                column: "RequestId",
                principalTable: "AiRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiCredits_Request",
                table: "AiCredits");

            migrationBuilder.DropIndex(
                name: "IX_AiCredits_GrantKey",
                table: "AiCredits");

            migrationBuilder.CreateIndex(
                name: "IX_AiCredits_UserId_GrantKey",
                table: "AiCredits",
                columns: new[] { "UserId", "GrantKey" },
                unique: true);

            // The previous migration already owns this native foreign key.
            migrationBuilder.Sql("ALTER TABLE \"AiCredits\" ADD CONSTRAINT \"FK_AiCredits_Request\" FOREIGN KEY (\"RequestId\") REFERENCES \"AiRequests\"(\"Id\") ON DELETE RESTRICT;");
        }
    }
}
