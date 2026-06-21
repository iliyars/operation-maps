using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.Persistence.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddOptionalFormParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOptional",
                table: "FormParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OptionalForRowNumber",
                table: "FormParameters",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOptional",
                table: "FormParameters");

            migrationBuilder.DropColumn(
                name: "OptionalForRowNumber",
                table: "FormParameters");
        }
    }
}
