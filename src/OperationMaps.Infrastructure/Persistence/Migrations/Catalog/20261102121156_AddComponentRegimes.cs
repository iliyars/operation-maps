using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.Persistence.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddComponentRegimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComponentRegimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Condition = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentRegimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentRegimes_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentRegimeValues",
                columns: table => new
                {
                    ComponentRegimeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentRegimeValues", x => new { x.ComponentRegimeId, x.FormParameterId });
                    table.ForeignKey(
                        name: "FK_ComponentRegimeValues_ComponentRegimes_ComponentRegimeId",
                        column: x => x.ComponentRegimeId,
                        principalTable: "ComponentRegimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentRegimeValues_FormParameters_FormParameterId",
                        column: x => x.FormParameterId,
                        principalTable: "FormParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentRegimes_ComponentId_Name",
                table: "ComponentRegimes",
                columns: new[] { "ComponentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentRegimeValues_FormParameterId",
                table: "ComponentRegimeValues",
                column: "FormParameterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentRegimeValues");

            migrationBuilder.DropTable(
                name: "ComponentRegimes");
        }
    }
}
