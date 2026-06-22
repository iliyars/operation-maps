using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.Persistence.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddLoadFactorParameterCalc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLoadFactorResult",
                table: "FormParameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LoadFactorParameterId",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Components_LoadFactorParameterId",
                table: "Components",
                column: "LoadFactorParameterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Components_FormParameters_LoadFactorParameterId",
                table: "Components",
                column: "LoadFactorParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_FormParameters_LoadFactorParameterId",
                table: "Components");

            migrationBuilder.DropIndex(
                name: "IX_Components_LoadFactorParameterId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "IsLoadFactorResult",
                table: "FormParameters");

            migrationBuilder.DropColumn(
                name: "LoadFactorParameterId",
                table: "Components");
        }
    }
}
