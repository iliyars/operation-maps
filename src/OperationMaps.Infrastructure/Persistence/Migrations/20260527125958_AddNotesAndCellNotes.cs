using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesAndCellNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectComponents_Components_ComponentId",
                table: "ProjectComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_CreatedById",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_RegimeGroupMembers_ProjectComponents_ProjectComponentId",
                table: "RegimeGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_RegimeGroups_Froms_FormId",
                table: "RegimeGroups");

            migrationBuilder.AddColumn<int>(
                name: "FormParameterId",
                table: "FamilyNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FormParameterId",
                table: "ComponentNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ParameterCellNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParameterCellValueId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterCellNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterCellNotes_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParameterCellNotes_ParameterCellValues_ParameterCellValueId",
                        column: x => x.ParameterCellValueId,
                        principalTable: "ParameterCellValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNotes_FormParameterId",
                table: "FamilyNotes",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNotes_FormParameterId",
                table: "ComponentNotes",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellNotes_NoteId",
                table: "ParameterCellNotes",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellNotes_ParameterCellValueId_Order",
                table: "ParameterCellNotes",
                columns: new[] { "ParameterCellValueId", "Order" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentNotes_FormParameters_FormParameterId",
                table: "ComponentNotes",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyNotes_FormParameters_FormParameterId",
                table: "FamilyNotes",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectComponents_Components_ComponentId",
                table: "ProjectComponents",
                column: "ComponentId",
                principalTable: "Components",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_CreatedById",
                table: "Projects",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegimeGroupMembers_ProjectComponents_ProjectComponentId",
                table: "RegimeGroupMembers",
                column: "ProjectComponentId",
                principalTable: "ProjectComponents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegimeGroups_Froms_FormId",
                table: "RegimeGroups",
                column: "FormId",
                principalTable: "Froms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComponentNotes_FormParameters_FormParameterId",
                table: "ComponentNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_FamilyNotes_FormParameters_FormParameterId",
                table: "FamilyNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectComponents_Components_ComponentId",
                table: "ProjectComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_CreatedById",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_RegimeGroupMembers_ProjectComponents_ProjectComponentId",
                table: "RegimeGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_RegimeGroups_Froms_FormId",
                table: "RegimeGroups");

            migrationBuilder.DropTable(
                name: "ParameterCellNotes");

            migrationBuilder.DropIndex(
                name: "IX_FamilyNotes_FormParameterId",
                table: "FamilyNotes");

            migrationBuilder.DropIndex(
                name: "IX_ComponentNotes_FormParameterId",
                table: "ComponentNotes");

            migrationBuilder.DropColumn(
                name: "FormParameterId",
                table: "FamilyNotes");

            migrationBuilder.DropColumn(
                name: "FormParameterId",
                table: "ComponentNotes");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectComponents_Components_ComponentId",
                table: "ProjectComponents",
                column: "ComponentId",
                principalTable: "Components",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_CreatedById",
                table: "Projects",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RegimeGroupMembers_ProjectComponents_ProjectComponentId",
                table: "RegimeGroupMembers",
                column: "ProjectComponentId",
                principalTable: "ProjectComponents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RegimeGroups_Froms_FormId",
                table: "RegimeGroups",
                column: "FormId",
                principalTable: "Froms",
                principalColumn: "Id");
        }
    }
}
