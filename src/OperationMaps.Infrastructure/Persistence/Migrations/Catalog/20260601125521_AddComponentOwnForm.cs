using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.Persistence.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddComponentOwnForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsAdminReview",
                table: "Components",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OwnFormId",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Components_OwnFormId",
                table: "Components",
                column: "OwnFormId");

            migrationBuilder.AddForeignKey(
                name: "FK_Components_Forms_OwnFormId",
                table: "Components",
                column: "OwnFormId",
                principalTable: "Forms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_Forms_OwnFormId",
                table: "Components");

            migrationBuilder.DropIndex(
                name: "IX_Components_OwnFormId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "NeedsAdminReview",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "OwnFormId",
                table: "Components");
        }
    }
}
