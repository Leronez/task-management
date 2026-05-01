using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StatusAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE tasks
                ALTER COLUMN "Status" TYPE text
                USING CASE "Status"
                  WHEN 0 THEN 'New'
                  WHEN 1 THEN 'InProgress'
                  WHEN 2 THEN 'Completed'
                  ELSE 'New'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE tasks
                ALTER COLUMN "Status" TYPE integer
                USING CASE "Status"
                  WHEN 'New' THEN 0
                  WHEN 'InProgress' THEN 1
                  WHEN 'Completed' THEN 2
                  ELSE 0
                END;
                """);
        }
    }
}
