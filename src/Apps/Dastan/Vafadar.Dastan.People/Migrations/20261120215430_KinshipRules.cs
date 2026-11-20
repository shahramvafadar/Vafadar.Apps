using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <summary>
    /// What the store checks under the lock of a tree, said to the database too (step 20.2, ADR 0022): nobody
    /// is linked to themselves, and two records of a tree are linked once, whichever is named first. The index
    /// is written here by hand – the model cannot say "the smaller and the larger of two columns".
    /// </summary>
    public partial class KinshipRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Links that two requests at the same moment could write before the store took the lock of the tree
            // would stand in the way of the rules: a link of a record to itself says nothing, and of two links
            // between the same two records the first one is the one the store would have kept.
            migrationBuilder.Sql("""
                DELETE FROM personal."Kinships" WHERE "From" = "To";
                DELETE FROM personal."Kinships" AS later USING personal."Kinships" AS first
                WHERE later."Tree" = first."Tree" AND later."Number" > first."Number"
                  AND LEAST(later."From", later."To") = LEAST(first."From", first."To")
                  AND GREATEST(later."From", later."To") = GREATEST(first."From", first."To");
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Kinships_TwoRecords",
                schema: "personal",
                table: "Kinships",
                sql: "\"From\" <> \"To\"");

            migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_Kinships_TwoRecordsOnce" ON personal."Kinships" ("Tree", LEAST("From", "To"), GREATEST("From", "To"));""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX personal."IX_Kinships_TwoRecordsOnce";""");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Kinships_TwoRecords",
                schema: "personal",
                table: "Kinships");
        }
    }
}
