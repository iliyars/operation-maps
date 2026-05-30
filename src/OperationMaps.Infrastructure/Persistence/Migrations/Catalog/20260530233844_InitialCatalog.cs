using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.src.OperationMaps.infrastructure.Persistence.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComponentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Login = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ComponentTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Families_ComponentTypes_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyParsingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Pattern = table.Column<string>(type: "TEXT", nullable: false),
                    Example = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyParsingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyParsingRules_ComponentTypes_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceFileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Designation = table.Column<string>(type: "TEXT", nullable: true),
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Components_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectComponent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Designation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RawName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    DetectedCategory = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectComponent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectComponent_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectComponent_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentNotes",
                columns: table => new
                {
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentNotes", x => new { x.ComponentId, x.FormParameterId, x.NoteId });
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
                name: "ComponentNtdValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentNtdValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentNtdValues_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentPinValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Pins = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentPinValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentPinValues_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "FamilyNotes",
                columns: table => new
                {
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyNotes", x => new { x.FamilyId, x.FormParameterId, x.NoteId });
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

            migrationBuilder.CreateTable(
                name: "FamilyNtdValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyNtdValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyNtdValues_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CanBeLoadFactorBase = table.Column<bool>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Froms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsUniversal = table.Column<bool>(type: "INTEGER", nullable: false),
                    ColumnGrouping = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultLoadFactorParameterId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Froms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Froms_FormParameters_DefaultLoadFactorParameterId",
                        column: x => x.DefaultLoadFactorParameterId,
                        principalTable: "FormParameters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSections_Froms_FormId",
                        column: x => x.FormId,
                        principalTable: "Froms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormValueColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormValueColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormValueColumns_Froms_FormId",
                        column: x => x.FormId,
                        principalTable: "Froms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegimeGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    LoadFactorParameterId = table.Column<int>(type: "INTEGER", nullable: true),
                    LoadFactorMin = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegimeGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegimeGroup_FormParameters_LoadFactorParameterId",
                        column: x => x.LoadFactorParameterId,
                        principalTable: "FormParameters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegimeGroup_Froms_FormId",
                        column: x => x.FormId,
                        principalTable: "Froms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegimeGroup_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterCellValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegimeGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormValueColumnId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterCellValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterCellValue_FormParameters_FormParameterId",
                        column: x => x.FormParameterId,
                        principalTable: "FormParameters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParameterCellValue_FormValueColumns_FormValueColumnId",
                        column: x => x.FormValueColumnId,
                        principalTable: "FormValueColumns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParameterCellValue_RegimeGroup_RegimeGroupId",
                        column: x => x.RegimeGroupId,
                        principalTable: "RegimeGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegimeGroupMember",
                columns: table => new
                {
                    RegimeGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectComponentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegimeGroupMember", x => new { x.RegimeGroupId, x.ProjectComponentId });
                    table.ForeignKey(
                        name: "FK_RegimeGroupMember_ProjectComponent_ProjectComponentId",
                        column: x => x.ProjectComponentId,
                        principalTable: "ProjectComponent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegimeGroupMember_RegimeGroup_RegimeGroupId",
                        column: x => x.RegimeGroupId,
                        principalTable: "RegimeGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterCellNote",
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
                    table.PrimaryKey("PK_ParameterCellNote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterCellNote_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParameterCellNote_ParameterCellValue_ParameterCellValueId",
                        column: x => x.ParameterCellValueId,
                        principalTable: "ParameterCellValue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNotes_FormParameterId",
                table: "ComponentNotes",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNotes_NoteId",
                table: "ComponentNotes",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNtdValues_ComponentId",
                table: "ComponentNtdValues",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNtdValues_FormParameterId",
                table: "ComponentNtdValues",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentPinValues_ComponentId",
                table: "ComponentPinValues",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentPinValues_FormParameterId",
                table: "ComponentPinValues",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_FamilyId",
                table: "Components",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_FullName",
                table: "Components",
                column: "FullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTypes_Name",
                table: "ComponentTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Families_ComponentTypeId_Name",
                table: "Families",
                columns: new[] { "ComponentTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyForms_FormId",
                table: "FamilyForms",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNotes_FormParameterId",
                table: "FamilyNotes",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNotes_NoteId",
                table: "FamilyNotes",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNtdValues_FamilyId",
                table: "FamilyNtdValues",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNtdValues_FormParameterId",
                table: "FamilyNtdValues",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyParsingRules_ComponentTypeId",
                table: "FamilyParsingRules",
                column: "ComponentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FormParameters_FormId_RowNumber",
                table: "FormParameters",
                columns: new[] { "FormId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormParameters_SectionId",
                table: "FormParameters",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSections_FormId",
                table: "FormSections",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormValueColumns_FormId_Key",
                table: "FormValueColumns",
                columns: new[] { "FormId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Froms_DefaultLoadFactorParameterId",
                table: "Froms",
                column: "DefaultLoadFactorParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_Froms_Number",
                table: "Froms",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellNote_NoteId",
                table: "ParameterCellNote",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellNote_ParameterCellValueId_Order",
                table: "ParameterCellNote",
                columns: new[] { "ParameterCellValueId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellValue_FormParameterId",
                table: "ParameterCellValue",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellValue_FormValueColumnId",
                table: "ParameterCellValue",
                column: "FormValueColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellValue_RegimeGroupId_FormParameterId_FormValueColumnId",
                table: "ParameterCellValue",
                columns: new[] { "RegimeGroupId", "FormParameterId", "FormValueColumnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_CreatedById",
                table: "Project",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectComponent_ComponentId",
                table: "ProjectComponent",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectComponent_ProjectId",
                table: "ProjectComponent",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RegimeGroup_FormId",
                table: "RegimeGroup",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_RegimeGroup_LoadFactorParameterId",
                table: "RegimeGroup",
                column: "LoadFactorParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_RegimeGroup_ProjectId",
                table: "RegimeGroup",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RegimeGroupMember_ProjectComponentId",
                table: "RegimeGroupMember",
                column: "ProjectComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentNotes_FormParameters_FormParameterId",
                table: "ComponentNotes",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_FamilyForms_Froms_FormId",
                table: "FamilyForms",
                column: "FormId",
                principalTable: "Froms",
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
                name: "FK_FamilyNtdValues_FormParameters_FormParameterId",
                table: "FamilyNtdValues",
                column: "FormParameterId",
                principalTable: "FormParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FormParameters_FormSections_SectionId",
                table: "FormParameters",
                column: "SectionId",
                principalTable: "FormSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FormParameters_Froms_FormId",
                table: "FormParameters",
                column: "FormId",
                principalTable: "Froms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Froms_FormParameters_DefaultLoadFactorParameterId",
                table: "Froms");

            migrationBuilder.DropTable(
                name: "ComponentNotes");

            migrationBuilder.DropTable(
                name: "ComponentNtdValues");

            migrationBuilder.DropTable(
                name: "ComponentPinValues");

            migrationBuilder.DropTable(
                name: "FamilyForms");

            migrationBuilder.DropTable(
                name: "FamilyNotes");

            migrationBuilder.DropTable(
                name: "FamilyNtdValues");

            migrationBuilder.DropTable(
                name: "FamilyParsingRules");

            migrationBuilder.DropTable(
                name: "ParameterCellNote");

            migrationBuilder.DropTable(
                name: "RegimeGroupMember");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "ParameterCellValue");

            migrationBuilder.DropTable(
                name: "ProjectComponent");

            migrationBuilder.DropTable(
                name: "FormValueColumns");

            migrationBuilder.DropTable(
                name: "RegimeGroup");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "Families");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ComponentTypes");

            migrationBuilder.DropTable(
                name: "FormParameters");

            migrationBuilder.DropTable(
                name: "FormSections");

            migrationBuilder.DropTable(
                name: "Froms");
        }
    }
}
