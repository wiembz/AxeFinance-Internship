using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDashboardAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePriorityFromProblems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Problems_Priority",
                table: "Problems");

            migrationBuilder.DropIndex(
                name: "IX_Problems_Status_Priority",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Problems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Problems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_Priority",
                table: "Problems",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_Status_Priority",
                table: "Problems",
                columns: new[] { "Status", "Priority" });
        }
    }
}
