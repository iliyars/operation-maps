using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComponentNtdValues_FormParameters_FormParameterId",
                table: "ComponentNtdValues");

            migrationBuilder.DropForeignKey(
                name: "FK_ComponentPinValues_FormParameters_FormParameterId",
                table: "ComponentPinValues");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyNtdValues_FormParameters_FormParameterId",
                table: "FamilyNtdValues");

            migrationBuilder.DropIndex(
                name: "IX_Components_FullName",
                table: "Components");

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComponentNotes",
                columns: table => new
                {
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentNotes", x => new { x.ComponentId, x.NoteId });
                    table.ForeignKey(
                        name: "FK_ComponentNotes_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentNotes_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyNotes",
                columns: table => new
                {
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyNotes", x => new { x.FamilyId, x.NoteId });
                    table.ForeignKey(
                        name: "FK_FamilyNotes_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyNotes_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNotes_NoteId",
                table: "ComponentNotes",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNotes_NoteId",
                table: "FamilyNotes",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_Text",
                table: "Notes",
                column: "Text",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentNtdValues_FormParameters_FormParameterId",
                table: "ComponentNtdValues",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentPinValues_FormParameters_FormParameterId",
                table: "ComponentPinValues",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyNtdValues_FormParameters_FormParameterId",
                table: "FamilyNtdValues",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComponentNtdValues_FormParameters_FormParameterId",
                table: "ComponentNtdValues");

            migrationBuilder.DropForeignKey(
                name: "FK_ComponentPinValues_FormParameters_FormParameterId",
                table: "ComponentPinValues");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyNtdValues_FormParameters_FormParameterId",
                table: "FamilyNtdValues");

            migrationBuilder.DropTable(
                name: "ComponentNotes");

            migrationBuilder.DropTable(
                name: "FamilyNotes");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.CreateIndex(
                name: "IX_Components_FullName",
                table: "Components",
                column: "FullName");

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentNtdValues_FormParameters_FormParameterId",
                table: "ComponentNtdValues",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentPinValues_FormParameters_FormParameterId",
                table: "ComponentPinValues",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyNtdValues_FormParameters_FormParameterId",
                table: "FamilyNtdValues",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id");
        }
    }
}
