using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HomeFinancialRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // D-509: this file-local concurrency marker is not an EF financial entity, sync record or
            // portable preference. SQLite updates it atomically even for raw-SQL and bulk writers.
            migrationBuilder.Sql("""
                CREATE TABLE HomeFinancialRevision (
                    Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                    Revision TEXT NOT NULL CHECK (length(Revision) = 32)
                );
                INSERT INTO HomeFinancialRevision (Id, Revision) VALUES (1, lower(hex(randomblob(16))));
                """);
            foreach (var table in FinancialTables)
            foreach (var operation in Operations)
                migrationBuilder.Sql($"""
                    CREATE TRIGGER HomeFinancialRevision_{table}_{operation} AFTER {operation} ON "{table}"
                    BEGIN
                        UPDATE HomeFinancialRevision SET Revision = lower(hex(randomblob(16))) WHERE Id = 1;
                    END;
                    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in FinancialTables)
            foreach (var operation in Operations)
                migrationBuilder.Sql($"DROP TRIGGER HomeFinancialRevision_{table}_{operation};");
            migrationBuilder.DropTable("HomeFinancialRevision");
        }

        /// <summary>Complete financial tables captured by Home, beyond its separately reviewed definitions.</summary>
        private static readonly string[] FinancialTables =
            ["Entries", "Budgets", "BudgetCategoryLimits", "ExchangeRates", "AssetTypes", "AssetEvents"];

        /// <summary>All persisted mutations, including deletes and external original-table writers.</summary>
        private static readonly string[] Operations = ["INSERT", "UPDATE", "DELETE"];
    }
}
