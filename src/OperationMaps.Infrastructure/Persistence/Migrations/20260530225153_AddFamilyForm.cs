using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TypeForms");

            migrationBuilder.DropIndex(
                name: "IX_Notes_Text",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_FamilyNtdValues_FamilyId_FormParameterId",
                table: "FamilyNtdValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FamilyNotes",
                table: "FamilyNotes");

            migrationBuilder.DropIndex(
                name: "IX_ComponentPinValues_ComponentId_FormParameterId",
                table: "ComponentPinValues");

            migrationBuilder.DropIndex(
                name: "IX_ComponentNtdValues_ComponentId_FormParameterId",
                table: "ComponentNtdValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComponentNotes",
                table: "ComponentNotes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FamilyNotes",
                table: "FamilyNotes",
                columns: new[] { "FamilyId", "FormParameterId", "NoteId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComponentNotes",
                table: "ComponentNotes",
                columns: new[] { "ComponentId", "FormParameterId", "NoteId" });

            migrationBuilder.CreateTable(
                name: "FamilyForms",
                columns: table => new
                {
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyForms", x => new { x.FamilyId, x.FormId });
                    table.ForeignKey(
                        name: "FK_FamilyForms_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyForms_Froms_FormId",
                        column: x => x.FormId,
                        principalTable: "Froms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNtdValues_FamilyId",
                table: "FamilyNtdValues",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_FullName",
                table: "Components",
                column: "FullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentPinValues_ComponentId",
                table: "ComponentPinValues",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNtdValues_ComponentId",
                table: "ComponentNtdValues",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyForms_FormId",
                table: "FamilyForms",
                column: "FormId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyForms");

            migrationBuilder.DropIndex(
                name: "IX_FamilyNtdValues_FamilyId",
                table: "FamilyNtdValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FamilyNotes",
                table: "FamilyNotes");

            migrationBuilder.DropIndex(
                name: "IX_Components_FullName",
                table: "Components");

            migrationBuilder.DropIndex(
                name: "IX_ComponentPinValues_ComponentId",
                table: "ComponentPinValues");

            migrationBuilder.DropIndex(
                name: "IX_ComponentNtdValues_ComponentId",
                table: "ComponentNtdValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComponentNotes",
                table: "ComponentNotes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FamilyNotes",
                table: "FamilyNotes",
                columns: new[] { "FamilyId", "NoteId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComponentNotes",
                table: "ComponentNotes",
                columns: new[] { "ComponentId", "NoteId" });

            migrationBuilder.CreateTable(
                name: "TypeForms",
                columns: table => new
                {
                    ComponentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeForms", x => new { x.ComponentTypeId, x.FormId });
                    table.ForeignKey(
                        name: "FK_TypeForms_ComponentTypes_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TypeForms_Froms_FormId",
                        column: x => x.FormId,
                        principalTable: "Froms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_Text",
                table: "Notes",
                column: "Text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNtdValues_FamilyId_FormParameterId",
                table: "FamilyNtdValues",
                columns: new[] { "FamilyId", "FormParameterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentPinValues_ComponentId_FormParameterId",
                table: "ComponentPinValues",
                columns: new[] { "ComponentId", "FormParameterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNtdValues_ComponentId_FormParameterId",
                table: "ComponentNtdValues",
                columns: new[] { "ComponentId", "FormParameterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypeForms_FormId",
                table: "TypeForms",
                column: "FormId");
        }
    }
}
