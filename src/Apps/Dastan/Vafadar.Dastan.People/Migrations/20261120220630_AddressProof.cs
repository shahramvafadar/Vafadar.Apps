using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <summary>How the address of an account was confirmed (step 20.3, ADR 0016).</summary>
    public partial class AddressProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressProof",
                schema: "identity",
                table: "VisitorAccounts",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            // What is certain is written down, and nothing else (step 20.3): an account that never had a
            // sign-in of a provider can only have confirmed its address by the link the site mailed. For an
            // account with such a sign-in nobody can say any more which way it was; it stays without a record,
            // and "people addresses" lists it.
            migrationBuilder.Sql("""
                UPDATE identity."VisitorAccounts" AS account SET "AddressProof" = 'mail'
                WHERE account."EmailConfirmed" AND NOT EXISTS (SELECT 1 FROM identity."VisitorLogins" AS login WHERE login."UserId" = account."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressProof",
                schema: "identity",
                table: "VisitorAccounts");
        }
    }
}
