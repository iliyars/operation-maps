using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDetectedCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetectedCategory",
                table: "ProjectComponents",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetectedCategory",
                table: "ProjectComponents");
        }
    }
}
